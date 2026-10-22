using EndlessBattle.Application.Common;
using EndlessBattle.Application.Jobs;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class JobServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly InMemorySkillLoadoutRepository loadout;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly FakeClock clock = new(Now);
    private readonly JobService service;

    public JobServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        loadout = new InMemorySkillLoadoutRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        service = new JobService(characters, proficiencies, loadout, inventory, equipment, Content, clock, new InMemoryUnitOfWork());
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> ArrangeAsync(params SkillSlot[] slots)
    {
        var character = await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, Now), CancellationToken);
        await loadout.ReplaceAsync(character.Id, slots, CancellationToken);
        log.Clear();
        return character;
    }

    [Fact]
    public async Task JobChange_ClearsPassiveSlotsKeepsActive()
    {
        var character = await ArrangeAsync(new SkillSlot(1, "burst", 0), new SkillSlot(4, "iron-skin", 40));

        var profile = await service.ChangeAsync(AccountId, "thief", CancellationToken);

        Assert.Equal("thief", profile.Character.JobId);
        Assert.Equal(["update", "loadout"], log);
        Assert.Equal([new SkillSlot(1, "burst", 0)], await loadout.ListAsync(character.Id, CancellationToken));
    }

    // 行動力 10、上次回復在 Now；400 秒後換職業 → 回應 10 + 12 = 22（唯讀，存檔仍是 10）
    [Fact]
    public async Task Change_ReturnsRegeneratedActNum()
    {
        await characters.AddAsync(
            TestCharacters.Default(AccountId, "測試者", 1, 1, Now) with { ActNum = 10, ActNumTime = Now }, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(400));

        var profile = await service.ChangeAsync(AccountId, "thief", CancellationToken);

        Assert.Equal(22, profile.Character.ActNum);
        Assert.Equal(10, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.ActNum);
    }

    [Fact]
    public async Task Change_DoesNotTouchSlots_WhenIneligible()
    {
        await ArrangeAsync(new SkillSlot(4, "iron-skin", 40));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.ChangeAsync(AccountId, "berserker", CancellationToken));

        Assert.Contains("levelTooLow", exception.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Change_RejectsUnknownJob()
    {
        await ArrangeAsync();

        await Assert.ThrowsAsync<InputValidationException>(() => service.ChangeAsync(AccountId, "nope", CancellationToken));
    }

    [Fact]
    public async Task Change_ConflictLeavesSlots()
    {
        var character = await ArrangeAsync(new SkillSlot(4, "iron-skin", 40));
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeAsync(AccountId, "thief", CancellationToken));

        Assert.Empty(log);
        Assert.Single(await loadout.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task List_MarksCurrentAndPercent()
    {
        var character = await ArrangeAsync();
        await proficiencies.AddAsync(character.Id, "warrior", 1750, 0, CancellationToken);

        var options = await service.ListAsync(AccountId, CancellationToken);

        Assert.Equal(Content.AllJobs.Select(job => job.Id), options.Select(option => option.Id));
        var warrior = options.Single(option => option.Id == "warrior");
        Assert.True(warrior.Current);
        Assert.Equal(50, warrior.ProficiencyPercent);
        Assert.Equal(["sameJob"], warrior.Reasons);
        var thief = options.Single(option => option.Id == "thief");
        Assert.True(thief.Eligible);
        Assert.False(thief.Current);
        var berserker = options.Single(option => option.Id == "berserker");
        Assert.Equal("測試戰士", berserker.ParentName);
        Assert.False(berserker.Eligible);
        Assert.Equal(0, berserker.ProficiencyPercent);
    }

    private Task<long> GiveAsync(Character character, string itemId, int quantity = 1) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity), CancellationToken);

    private async Task<long> EquipAsync(Character character, EquipmentSlot slot, string itemId, int quantity = 1)
    {
        var id = await GiveAsync(character, itemId, quantity);
        await equipment.SetSlotAsync(character.Id, slot, id, slot == EquipmentSlot.Supply ? quantity : null, CancellationToken);
        return id;
    }

    private async Task<IReadOnlyList<(string ItemId, int Quantity)>> ItemsAsync(Character character) =>
        (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => (entry.ItemId, entry.Quantity)).ToList();

    // 轉職時六格裝備與補給欄全部回到背包，回應的裝備加成歸零
    [Fact]
    public async Task JobChange_UnequipsEverything()
    {
        var character = await ArrangeAsync();
        await EquipAllSlotsAsync(character);

        var profile = await service.ChangeAsync(AccountId, "thief", CancellationToken);

        Assert.Equal("thief", profile.Character.JobId);
        Assert.Equal(EquipmentBonus.Zero, profile.Equipment);
        Assert.Empty((await equipment.GetAsync(character.Id, CancellationToken)).Equipped);
        Assert.Equal(
            [
                ("test-sword", 1), ("test-helm", 1), ("test-armor", 1), ("test-gloves", 1), ("test-boots", 1),
                ("test-power-ring", 1), ("test-supply", 5),
            ],
            await ItemsAsync(character));
    }

    // 角色寫入（版本鎖）必須排在卸裝之前：衝突時裝備欄、背包列都不動
    [Fact]
    public async Task Change_ConflictLeavesEquippedItems()
    {
        var character = await ArrangeAsync();
        await EquipAllSlotsAsync(character);
        inventory.Writes.Clear();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeAsync(AccountId, "thief", CancellationToken));

        Assert.Empty(inventory.Writes);
        var loadoutAfter = await equipment.GetAsync(character.Id, CancellationToken);
        Assert.Equal(Enum.GetValues<EquipmentSlot>(), loadoutAfter.Equipped.Keys.Order());
        Assert.Equal(5, loadoutAfter.SupplyQuantity);
    }

    private async Task EquipAllSlotsAsync(Character character)
    {
        await EquipAsync(character, EquipmentSlot.Weapon, "test-sword");
        await EquipAsync(character, EquipmentSlot.Head, "test-helm");
        await EquipAsync(character, EquipmentSlot.Body, "test-armor");
        await EquipAsync(character, EquipmentSlot.Hand, "test-gloves");
        await EquipAsync(character, EquipmentSlot.Foot, "test-boots");
        await EquipAsync(character, EquipmentSlot.Accessory, "test-power-ring");
        await EquipAsync(character, EquipmentSlot.Supply, "test-supply", 5);
    }

    // 補給欄 5 個併回背包原有的 10 個，補給欄那一列刪除
    [Fact]
    public async Task JobChange_UnequipsSupplySlot()
    {
        var character = await ArrangeAsync();
        var stack = await GiveAsync(character, "test-supply", 10);
        await EquipAsync(character, EquipmentSlot.Supply, "test-supply", 5);

        await service.ChangeAsync(AccountId, "thief", CancellationToken);

        Assert.Empty((await equipment.GetAsync(character.Id, CancellationToken)).Equipped);
        Assert.Equal([("test-supply", 15)], await ItemsAsync(character));
        Assert.Equal(15, inventory.Find(character.Id, stack).Quantity);
    }

    // 武器分類 15 格全滿，裝備中的劍放不回去：不轉職、什麼都不寫
    [Fact]
    public async Task JobChange_BagFullWritesNothing()
    {
        var character = await ArrangeAsync(new SkillSlot(4, "iron-skin", 40));
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(character, "test-sword");
        }

        await EquipAsync(character, EquipmentSlot.Weapon, "test-sword");
        await EquipAsync(character, EquipmentSlot.Supply, "test-supply", 5);
        inventory.Writes.Clear();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.ChangeAsync(AccountId, "thief", CancellationToken));

        Assert.Equal(["bagFull"], exception.Reasons);
        Assert.Empty(log);
        Assert.Empty(inventory.Writes);
        Assert.Equal(2, (await equipment.GetAsync(character.Id, CancellationToken)).Equipped.Count);
        Assert.Equal("warrior", (await characters.FindByAccountAsync(AccountId, CancellationToken))!.JobId);
    }
}
