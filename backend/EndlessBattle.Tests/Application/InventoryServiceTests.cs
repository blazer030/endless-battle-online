using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-sword（at 8、str ≥ 10）、test-axe（at 12、str ≥ 10）、test-heavy-sword（at 28、str ≥ 60）、
// test-power-ring（str +50）、test-supply（hp 50／sp 25）。預設戰士 Base str 12
public class InventoryServiceTests
{
    private const long AccountId = 7;
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemoryPetRepository pets = new();
    private readonly InMemoryJobProficiencyRepository proficiency = new();
    private readonly InMemoryBuffRepository buffs;
    private readonly ScriptedRandomFactory random = new();
    private readonly FakeClock clock = new(Now);
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly AdventureService adventure;
    private readonly InventoryService service;

    public InventoryServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        buffs = new InMemoryBuffRepository(log);
        unitOfWork = new InMemoryUnitOfWork(characters, inventory, equipment, pets, proficiency, buffs);
        adventure = new AdventureService(
            characters, proficiency, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(), inventory, equipment,
            new InMemoryMissionRepository(), pets, new InMemoryMercenaryRepository(), new InMemoryAreaPassRepository(), buffs,
            new InMemoryLodgingRepository(), Content, clock, random, unitOfWork);
        service = new InventoryService(
            characters, inventory, equipment, pets, proficiency, buffs, Content, clock, random, unitOfWork, adventure);
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    private Task<long> GiveAsync(Character character, string itemId, int quantity = 1) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity) with { Holes = Content.GetItem(itemId).Holes }, CancellationToken);

    private static IEnumerable<(string ItemId, int Quantity)> BagOf(InventoryView view, ItemCategory category) =>
        view.Bag.Single(bag => bag.Category == category).Items.Select(item => (item.Entry.ItemId, item.Entry.Quantity));

    private static IEnumerable<(EquipmentSlot Slot, string ItemId, int Quantity)> EquippedOf(InventoryView view) =>
        view.Equipped.Select(equipped => (equipped.Slot, equipped.Item.Entry.ItemId, equipped.Quantity));

    private async Task<List<(long Id, string ItemId, int Quantity)>> RowsAsync(Character character) =>
        (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => (entry.Id, entry.ItemId, entry.Quantity)).ToList();

    private static async Task<IReadOnlyList<string>> ReasonsAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<InputValidationException>(action)).Reasons;

    // 換下的武器回到背包；新武器離開背包，格數不變
    [Fact]
    public async Task Equip_SwapsBackToBag()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var axe = await GiveAsync(character, "test-axe");

        var first = await service.EquipAsync(AccountId, sword, null, CancellationToken);
        Assert.Equal([(EquipmentSlot.Weapon, "test-sword", 1)], EquippedOf(first));
        Assert.Equal([("test-axe", 1)], BagOf(first, ItemCategory.Weapon));

        var swapped = await service.EquipAsync(AccountId, axe, null, CancellationToken);

        Assert.Equal([(EquipmentSlot.Weapon, "test-axe", 1)], EquippedOf(swapped));
        Assert.Equal([("test-sword", 1)], BagOf(swapped, ItemCategory.Weapon));
        Assert.Equal(12, swapped.Bonus.Total.Attack);
    }

    [Fact]
    public async Task Equip_RejectsAlreadyEquipped()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await service.EquipAsync(AccountId, sword, null, CancellationToken);

        Assert.Equal(["alreadyEquipped"], await ReasonsAsync(() => service.EquipAsync(AccountId, sword, null, CancellationToken)));
    }

    // 別的角色的道具、不存在的 id：都當成背包裡沒有
    [Fact]
    public async Task Equip_RejectsItemNotInBag()
    {
        await AddCharacterAsync();
        var other = await characters.AddAsync(TestCharacters.Default(AccountId + 1, "路人", 1, 1, DateTimeOffset.UnixEpoch), CancellationToken);
        var foreign = await GiveAsync(other, "test-sword");

        Assert.Equal(["itemNotFound"], await ReasonsAsync(() => service.EquipAsync(AccountId, foreign, null, CancellationToken)));
        Assert.Equal(["itemNotFound"], await ReasonsAsync(() => service.EquipAsync(AccountId, 999, null, CancellationToken)));
    }

    // 戒指 +50 力量後有效力量 62，但巨劍需求比對裸點 12 → 仍然不能裝
    [Fact]
    public async Task Equip_RequirementsIgnoreEquippedBonus()
    {
        var character = await AddCharacterAsync();
        await service.EquipAsync(AccountId, await GiveAsync(character, "test-power-ring"), null, CancellationToken);
        var heavy = await GiveAsync(character, "test-heavy-sword");

        Assert.Equal(["statTooLow:strength"], await ReasonsAsync(() => service.EquipAsync(AccountId, heavy, null, CancellationToken)));
    }

    [Fact]
    public async Task Equip_RejectsNonEquipment()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 3);

        Assert.Equal(["notEquipment"], await ReasonsAsync(() => service.EquipAsync(AccountId, ore, null, CancellationToken)));
    }

    // ×30 裝 10 到補給欄：欄位記 10，背包剩 20；補給品不計能力加成
    [Fact]
    public async Task Equip_SupplySlotKeepsQuantity()
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);

        var view = await service.EquipAsync(AccountId, supply, 10, CancellationToken);

        Assert.Equal([(EquipmentSlot.Supply, "test-supply", 10)], EquippedOf(view));
        Assert.Equal([("test-supply", 20)], BagOf(view, ItemCategory.Consumable));
        Assert.Equal(EquipmentBonus.Zero, view.Bonus);
        Assert.Equal(10, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
    }

    // 不指定數量 → 整格裝上
    [Fact]
    public async Task Equip_SupplyDefaultsToWholeStack()
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);

        var view = await service.EquipAsync(AccountId, supply, null, CancellationToken);

        Assert.Equal([(EquipmentSlot.Supply, "test-supply", 30)], EquippedOf(view));
        Assert.Empty(BagOf(view, ItemCategory.Consumable));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(100)]
    public async Task Equip_SupplyRejectsInvalidQuantity(int quantity)
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);
        if (quantity == 100)
        {
            await inventory.UpdateQuantityAsync(character.Id, supply, 120, CancellationToken);
        }

        Assert.Equal(["invalidQuantity"], await ReasonsAsync(() => service.EquipAsync(AccountId, supply, quantity, CancellationToken)));
    }

    // 補給欄已有 10：從背包 ×20 再裝 5 → 背包 20 − 5 + 10 = 25，欄位 5，總數 30 不變
    [Fact]
    public async Task Equip_SupplySwapReturnsPreviousQuantity()
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);
        await service.EquipAsync(AccountId, supply, 10, CancellationToken);

        var view = await service.EquipAsync(AccountId, supply, 5, CancellationToken);

        Assert.Equal([(EquipmentSlot.Supply, "test-supply", 5)], EquippedOf(view));
        Assert.Equal([("test-supply", 25)], BagOf(view, ItemCategory.Consumable));
        Assert.Equal(2, (await inventory.ListAsync(character.Id, CancellationToken)).Count);
    }

    // 捕捉道具和補給品一樣放補給欄：×5 裝 3 → 欄位 3，背包剩 2
    [Fact]
    public async Task Equip_CaptureItemGoesToSupplySlot()
    {
        var character = await AddCharacterAsync();
        var net = await GiveAsync(character, "test-capture-net", 5);

        var view = await service.EquipAsync(AccountId, net, 3, CancellationToken);

        Assert.Equal([(EquipmentSlot.Supply, "test-capture-net", 3)], EquippedOf(view));
        Assert.Equal([("test-capture-net", 2)], BagOf(view, ItemCategory.Consumable));
        Assert.Equal(3, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
    }

    [Fact]
    public async Task Unequip_ReturnsEquipmentToBag()
    {
        var character = await AddCharacterAsync();
        await service.EquipAsync(AccountId, await GiveAsync(character, "test-sword"), null, CancellationToken);

        var view = await service.UnequipAsync(AccountId, EquipmentSlot.Weapon, CancellationToken);

        Assert.Empty(view.Equipped);
        Assert.Equal([("test-sword", 1)], BagOf(view, ItemCategory.Weapon));
        Assert.Equal(EquipmentBonus.Zero, view.Bonus);
    }

    // 背包武器 15 格已滿：卸下會多出一格 → 400 bagFull，武器仍在身上
    [Fact]
    public async Task Unequip_EquipmentBagFull400()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(character, "test-axe");
        }

        await service.EquipAsync(AccountId, sword, null, CancellationToken);

        Assert.Equal(["bagFull"], await ReasonsAsync(() => service.UnequipAsync(AccountId, EquipmentSlot.Weapon, CancellationToken)));
        Assert.Equal([(EquipmentSlot.Weapon, "test-sword", 1)], EquippedOf(await service.GetAsync(AccountId, CancellationToken)));
    }

    // 補給欄 10 卸下：疊回背包的 ×20 → ×30；背包 15 格都是 ×99 時放不下 → 400 bagFull，補給欄不變
    [Fact]
    public async Task Unequip_SupplyReturnsQuantity_BagFull400()
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);
        await service.EquipAsync(AccountId, supply, 10, CancellationToken);

        var view = await service.UnequipAsync(AccountId, EquipmentSlot.Supply, CancellationToken);

        Assert.Empty(view.Equipped);
        Assert.Equal([("test-supply", 30)], BagOf(view, ItemCategory.Consumable));

        var full = await AddFullSupplyBagAsync();
        Assert.Equal(["bagFull"], await ReasonsAsync(() => service.UnequipAsync(full.AccountId, EquipmentSlot.Supply, CancellationToken)));
        Assert.Equal(10, (await equipment.GetAsync(full.Id, CancellationToken)).SupplyQuantity);
    }

    // 另一個角色：補給欄 10，背包 15 格 test-supply 各 99
    private async Task<Character> AddFullSupplyBagAsync()
    {
        var full = await characters.AddAsync(TestCharacters.Default(AccountId + 1, "滿包", 1, 1, DateTimeOffset.UnixEpoch), CancellationToken);
        var equipped = await GiveAsync(full, "test-supply", 10);
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(full, "test-supply", InventoryRules.StackLimit);
        }

        await service.EquipAsync(full.AccountId, equipped, null, CancellationToken);
        return full;
    }

    [Fact]
    public async Task Unequip_RejectsEmptySlot()
    {
        await AddCharacterAsync();

        Assert.Equal(["slotEmpty"], await ReasonsAsync(() => service.UnequipAsync(AccountId, EquipmentSlot.Head, CancellationToken)));
    }

    // 力量藥水 str +1、vit +2：永久加在裸點；×2 用一次剩 ×1，再用一次整格消失
    [Fact]
    public async Task Use_StatPotionRaisesBase()
    {
        var character = await AddCharacterAsync();
        var potion = await GiveAsync(character, "test-str-potion", 2);

        var view = (await service.UseAsync(AccountId, potion, CancellationToken)).Inventory;

        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(character.Base with { Strength = 13, Vitality = 12 }, stored!.Base);
        Assert.Equal([("test-str-potion", 1)], BagOf(view, ItemCategory.Consumable));

        var emptied = (await service.UseAsync(AccountId, potion, CancellationToken)).Inventory;
        Assert.Empty(BagOf(emptied, ItemCategory.Consumable));
        Assert.Equal(14, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Base.Strength);
    }

    // 擴充是「設定」為 5，不是累加：3 → 5（累加會變 8）；道具與魔石分類變 20 格
    [Fact]
    public async Task Use_BagExpansionSetsNotAdds()
    {
        var character = await AddCharacterAsync(character => character with { BagBonus = 3, DepotBonus = 3 });

        var view = (await service.UseAsync(AccountId, await GiveAsync(character, "test-bag-expansion"), CancellationToken)).Inventory;
        await service.UseAsync(AccountId, await GiveAsync(character, "test-depot-expansion"), CancellationToken);

        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((5, 5), (stored!.BagBonus, stored.DepotBonus));
        Assert.Equal(20, view.Bag.Single(bag => bag.Category == ItemCategory.Consumable).Capacity);
        Assert.Equal(15, view.Bag.Single(bag => bag.Category == ItemCategory.Material).Capacity);
    }

    [Fact]
    public async Task Use_ExpansionFromZero()
    {
        var character = await AddCharacterAsync();

        await service.UseAsync(AccountId, await GiveAsync(character, "test-bag-expansion"), CancellationToken);

        Assert.Equal(5, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.BagBonus);
    }

    [Theory]
    [InlineData("test-sword")]
    [InlineData("test-supply")]
    [InlineData("test-ore")]
    public async Task Use_RejectsNonConsumable(string itemId)
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, itemId);

        Assert.Equal(["notUsable"], await ReasonsAsync(() => service.UseAsync(AccountId, id, CancellationToken)));
        Assert.Empty(log);
    }

    // 角色以版本鎖寫入失敗時，藥水不能被扣掉
    [Fact]
    public async Task Use_KeepsItem_WhenCharacterUpdateConflicts()
    {
        var character = await AddCharacterAsync();
        var potion = await GiveAsync(character, "test-str-potion", 2);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.UseAsync(AccountId, potion, CancellationToken));

        Assert.Equal(2, inventory.Find(character.Id, potion).Quantity);
    }

    [Fact]
    public async Task Get_Returns404WithoutCharacter()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(AccountId, CancellationToken));
    }

    // 重複送出的裝備請求要撞版本鎖：角色寫入衝突時，背包與欄位都不能動
    [Fact]
    public async Task Equip_ConflictLeavesBagUntouched()
    {
        var character = await AddCharacterAsync();
        var supply = await GiveAsync(character, "test-supply", 30);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.EquipAsync(AccountId, supply, 10, CancellationToken));

        Assert.Equal([(supply, "test-supply", 30)], (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => (entry.Id, entry.ItemId, entry.Quantity)));
        Assert.Empty((await equipment.GetAsync(character.Id, CancellationToken)).Equipped);
    }

    [Fact]
    public async Task Unequip_ConflictLeavesSlotUntouched()
    {
        var character = await AddCharacterAsync();
        await service.EquipAsync(AccountId, await GiveAsync(character, "test-supply", 30), 10, CancellationToken);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.UnequipAsync(AccountId, EquipmentSlot.Supply, CancellationToken));

        Assert.Equal(10, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
        Assert.Equal([20, 10], (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => entry.Quantity));
    }

    [Fact]
    public async Task Equip_BumpsCharacterVersion()
    {
        var character = await AddCharacterAsync();

        await service.EquipAsync(AccountId, await GiveAsync(character, "test-sword"), null, CancellationToken);
        await service.UnequipAsync(AccountId, EquipmentSlot.Weapon, CancellationToken);

        Assert.Equal(2, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version);
        Assert.Equal(["update", "update"], log);
    }

    // 目前加成已 ≥ 擴充格數：400 noEffect，不消耗、不寫角色
    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    public async Task Use_ExpansionRejectedWhenNoEffect(int bonus)
    {
        var character = await AddCharacterAsync(character => character with { BagBonus = bonus, DepotBonus = bonus });
        var bag = await GiveAsync(character, "test-bag-expansion", 2);
        var depot = await GiveAsync(character, "test-depot-expansion");

        Assert.Equal(["noEffect"], await ReasonsAsync(() => service.UseAsync(AccountId, bag, CancellationToken)));
        Assert.Equal(["noEffect"], await ReasonsAsync(() => service.UseAsync(AccountId, depot, CancellationToken)));

        Assert.Equal([2, 1], (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => entry.Quantity));
        Assert.Empty(log);
    }

    // 容量只看同分類：道具分類 15 格全滿，卸下武器仍放得回武器分類
    [Fact]
    public async Task Unequip_IgnoresOtherFullCategories()
    {
        var character = await AddCharacterAsync();
        await service.EquipAsync(AccountId, await GiveAsync(character, "test-sword"), null, CancellationToken);
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(character, "test-supply", InventoryRules.StackLimit);
        }

        var view = await service.UnequipAsync(AccountId, EquipmentSlot.Weapon, CancellationToken);

        Assert.Equal([("test-sword", 1)], BagOf(view, ItemCategory.Weapon));
    }

    // 魔石分類 15 格都是 99 顆的測試攻擊魔石（寶箱唯一的候選）→ 開出的魔石放不下：400 bagFull，寶箱、背包、角色版本都不變
    [Fact]
    public async Task Use_StoneBoxRejectedWhenStoneBagFull()
    {
        var character = await AddCharacterAsync();
        var box = await GiveAsync(character, "test-stone-box", 2);
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(character, "test-stone", InventoryRules.StackLimit);
        }

        var before = await RowsAsync(character);

        Assert.Equal(["bagFull"], await ReasonsAsync(() => service.UseAsync(AccountId, box, CancellationToken)));

        Assert.Equal(before, await RowsAsync(character));
        Assert.Equal(2, inventory.Find(character.Id, box).Quantity);
        Assert.Equal(0, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version);
        Assert.Empty(log);
    }

    // 已有 3 隻寵物：400 petsFull，蛋還在、沒有新寵物、角色版本不變
    [Fact]
    public async Task Use_PetEggRejectedWhenPetsFull()
    {
        var character = await AddCharacterAsync();
        var egg = await GiveAsync(character, "test-pet-egg");
        for (var index = 0; index < 3; index++)
        {
            await pets.AddAsync(character.Id, TestPets.Create(carried: false), CancellationToken);
        }

        var before = await RowsAsync(character);

        Assert.Equal(["petsFull"], await ReasonsAsync(() => service.UseAsync(AccountId, egg, CancellationToken)));

        Assert.Equal(before, await RowsAsync(character));
        Assert.Equal(1, inventory.Find(character.Id, egg).Quantity);
        Assert.Equal(3, (await pets.ListAsync(character.Id, CancellationToken)).Count);
        Assert.Equal(0, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version);
        Assert.Empty(log);
    }

    // 力量藥水已寫進角色、扣藥水失敗：整筆回滾，角色能力與版本、背包都回到原狀
    [Fact]
    public async Task Use_RollsBackCharacterWhenBagWriteFails()
    {
        var character = await AddCharacterAsync();
        var potion = await GiveAsync(character, "test-str-potion");
        var bag = await RowsAsync(character);
        inventory.FailWrite = "remove";

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UseAsync(AccountId, potion, CancellationToken));

        Assert.Equal("rollback", unitOfWork.Log[^1]);
        Assert.Equal(character, await characters.FindByAccountAsync(AccountId, CancellationToken));
        Assert.Equal(bag, await RowsAsync(character));
    }

    // 魔石分類 14 格滿疊、道具分類有寶箱與補給品：只算魔石分類的格數，開出的魔石放進第 15 格
    [Fact]
    public async Task Use_StoneBoxCountsOnlyTheStoneCategory()
    {
        var character = await AddCharacterAsync();
        var box = await GiveAsync(character, "test-stone-box", 2);
        await GiveAsync(character, "test-supply");
        for (var index = 0; index < InventoryRules.SlotsPerCategory - 1; index++)
        {
            await GiveAsync(character, "test-stone", InventoryRules.StackLimit);
        }

        var result = await service.UseAsync(AccountId, box, CancellationToken);

        Assert.Equal(["test-stone"], result.GrantedItemIds);
        Assert.Equal(
            [.. Enumerable.Repeat(("test-stone", InventoryRules.StackLimit), InventoryRules.SlotsPerCategory - 1), ("test-stone", 1)],
            BagOf(result.Inventory, ItemCategory.Stone));
        Assert.Equal([("test-stone-box", 1), ("test-supply", 1)], BagOf(result.Inventory, ItemCategory.Consumable));
    }

    // 擲 0.0 → r = 1 等級 1，候選只有測試攻擊魔石：疊到原本那格上，寶箱整格消失
    [Fact]
    public async Task Use_StoneBoxAddsStonesAndConsumesBox()
    {
        var character = await AddCharacterAsync();
        var box = await GiveAsync(character, "test-stone-box");
        await GiveAsync(character, "test-stone", 3);

        var result = await service.UseAsync(AccountId, box, CancellationToken);

        Assert.Equal(["test-stone"], result.GrantedItemIds);
        Assert.Equal([("test-stone", 4)], BagOf(result.Inventory, ItemCategory.Stone));
        Assert.Empty(BagOf(result.Inventory, ItemCategory.Consumable));
        Assert.Equal(["update"], log);
    }

    // 寵物蛋：放獸欄、能力 ×280，回傳寵物名
    [Fact]
    public async Task Use_PetEggStoresPetInPen()
    {
        var character = await AddCharacterAsync();
        var egg = await GiveAsync(character, "test-pet-egg");
        random.Next = new ScriptedRandom(0.0);

        var result = await service.UseAsync(AccountId, egg, CancellationToken);

        var pet = Assert.Single(await pets.ListAsync(character.Id, CancellationToken));
        Assert.False(pet.Carried);
        Assert.Equal(Content.AllMonsters.First(monster => monster.MissionId is null).Id, pet.MonsterId);
        Assert.Equal((280, 280, 280, Now), (pet.Attack, pet.Magic, pet.Defense, pet.BornAt));
        Assert.Equal(pet.Name, result.PetName);
        Assert.Empty(BagOf(result.Inventory, ItemCategory.Consumable));
    }

    // 心得書：目前職業（戰士）的技能點 +300，熟練度不變
    [Fact]
    public async Task Use_ProficiencyBookWritesSkillPoints()
    {
        var character = await AddCharacterAsync();
        var notes = await GiveAsync(character, "test-job-notes", 2);

        var result = await service.UseAsync(AccountId, notes, CancellationToken);

        Assert.Equal(300, result.SkillPoints);
        Assert.Equal([new("warrior", 0, 300)], await proficiency.ListAsync(character.Id, CancellationToken));
        Assert.Equal([("test-job-notes", 1)], BagOf(result.Inventory, ItemCategory.Consumable));
    }

    // 喝下時（上次結算就是現在，不必推進）：以版本鎖寫角色 → 扣密藥 → 存密藥，從現在起 1 小時
    [Fact]
    public async Task Use_ElixirSavesBuff()
    {
        var character = await AddCharacterAsync(character => character with { LastSettledAt = Now });
        var elixir = await GiveAsync(character, "test-exp-elixir", 2);

        var result = await service.UseAsync(AccountId, elixir, CancellationToken);

        var expected = new CharacterBuff(BuffKind.Exp, 1.5, Now, Now.AddHours(1));
        Assert.Equal(expected, result.Buff);
        Assert.Equal([expected], await buffs.ListAsync(character.Id, CancellationToken));
        Assert.Equal([("test-exp-elixir", 1)], BagOf(result.Inventory, ItemCategory.Consumable));
        Assert.Equal(["update", "buff:Exp"], log);
    }

    // 測試雪精每場 2.8 秒、擲骰 0.0 時勝場經驗 116，經驗密藥 → 174（見 AdventureServiceTests 上方）；NextExp 很大，不升級。
    // 上次結算後 60 秒才喝、90 秒推進：0～58.8 開打的 22 場在喝之前，經驗不變；61.6～89.6 的 11 場在喝之後才加成
    [Fact]
    public async Task Use_ElixirDoesNotApplyToUnsettledFightsBeforeIt()
    {
        var character = await AddCharacterAsync(character => character with { LastSettledAt = Now, NextLevelExperience = 1_000_000_000 });
        var elixir = await GiveAsync(character, "test-exp-elixir");
        clock.Advance(TimeSpan.FromSeconds(60));

        await service.UseAsync(AccountId, elixir, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(30));
        await adventure.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal((22 * 116) + (11 * 174), (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Experience);
    }

    // 再喝一瓶同種密藥會覆蓋舊的：喝之前先把經過的時間以舊密藥結算掉，0～58.8 的 22 場仍是 174；之後的 11 場由新密藥加成
    [Fact]
    public async Task Use_ElixirSettlesElapsedTimeUnderTheBuffItReplaces()
    {
        var character = await AddCharacterAsync(character => character with { LastSettledAt = Now, NextLevelExperience = 1_000_000_000 });
        await buffs.SaveAsync(character.Id, new CharacterBuff(BuffKind.Exp, 1.5, Now.AddMinutes(-30), Now.AddMinutes(30)), CancellationToken);
        var elixir = await GiveAsync(character, "test-exp-elixir");
        clock.Advance(TimeSpan.FromSeconds(60));

        await service.UseAsync(AccountId, elixir, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(30));
        await adventure.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(33 * 174, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Experience);
        Assert.Equal(
            [new CharacterBuff(BuffKind.Exp, 1.5, Now.AddSeconds(60), Now.AddSeconds(3660))], await buffs.ListAsync(character.Id, CancellationToken));
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Next { get; set; } = new ScriptedRandom(0.0);

        public IRandomSource Create() => Next;
    }
}
