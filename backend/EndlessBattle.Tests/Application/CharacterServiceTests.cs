using EndlessBattle.Application.Characters;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class CharacterServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static InMemoryEquipmentRepository NoEquipment() => new(new InMemoryInventoryRepository());

    // 10 分鐘 = 3.33 個 180 秒週期 → 單次最多 +12：20 → 32；回復只反映在回傳值，不寫回
    [Fact]
    public async Task GetAsync_RegeneratesActNumWithoutWriting()
    {
        var repository = new InMemoryCharacterRepository();
        var service = new CharacterService(repository, new InMemoryJobProficiencyRepository(), NoEquipment(), Content, new FakeClock(Now));
        await repository.AddAsync(
            TestCharacters.Default(AccountId, "測試者", 1, 1, Now) with { ActNum = 20, ActNumTime = Now.AddMinutes(-10) },
            CancellationToken);

        var profile = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(32, profile.Character.ActNum);
        Assert.Equal(Now, profile.Character.ActNumTime);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(20, stored!.ActNum);
        Assert.Equal(Now.AddMinutes(-10), stored.ActNumTime);
        Assert.Equal(0, stored.Version);
    }

    // 起始地取自內容檔（TestContent：地區 1、子地區 1）
    [Fact]
    public async Task Create_StartsAtContentStartingPlace()
    {
        var service = new CharacterService(
            new InMemoryCharacterRepository(), new InMemoryJobProficiencyRepository(), NoEquipment(), Content, new FakeClock(Now));

        var character = await service.CreateAsync(
            AccountId, new CreateCharacterCommand("墨客", 1, 1, 1, "warrior", new StatAllocation(4, 2, 3, 1), "烈焰斬"), CancellationToken);

        Assert.Equal((Content.StartingPlaceId, Content.StartingSubPlace), (character.PlaceId, character.SubPlace));
    }

    // 裝上測試鐵劍 at 8：個人資料帶出裝備加成，供有效能力值顯示
    [Fact]
    public async Task GetAsync_IncludesEquipmentBonus()
    {
        var repository = new InMemoryCharacterRepository();
        var inventory = new InMemoryInventoryRepository();
        var equipment = new InMemoryEquipmentRepository(inventory);
        var service = new CharacterService(repository, new InMemoryJobProficiencyRepository(), equipment, Content, new FakeClock(Now));
        var character = await repository.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, Now), CancellationToken);
        var sword = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);

        var profile = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(StatBonus.Zero with { At = 8 }, profile.Equipment.Total);
    }
}
