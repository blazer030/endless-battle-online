using EndlessBattle.Application.Common;
using EndlessBattle.Application.Forge;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-sword（at 8、1 孔、精鍊上限 4）、test-refine-stone（Lv1 種類 1，at／df [1, 2]）、test-refine-stone-2（Lv2 種類 1）、
// test-stone（固定 at +5）、stone-remover、hole-driller、safe-refine-stone（材料）。
// ScriptedRandom 的值 v 對 rand(1, 100) 取 ⌊100v⌋ + 1
public class ForgeServiceTests
{
    private const long AccountId = 7;
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly ScriptedRandomFactory random = new();
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly ForgeService service;

    public ForgeServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        unitOfWork = new InMemoryUnitOfWork(characters, inventory);
        var inventoryService = new InventoryService(
            characters, inventory, equipment, new InMemoryPetRepository(), Content, new FakeClock(DateTimeOffset.UnixEpoch), random, unitOfWork);
        service = new ForgeService(characters, inventory, equipment, Content, random, inventoryService, unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> AddCharacterAsync() =>
        await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch), CancellationToken);

    private Task<long> GiveAsync(Character character, string itemId, int quantity = 1) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity) with { Holes = Content.GetItem(itemId).Holes }, CancellationToken);

    private InventoryEntry Find(Character character, long id) => inventory.Find(character.Id, id);

    private async Task<IReadOnlyList<(string ItemId, int Quantity)>> ItemsAsync(Character character) =>
        (await inventory.ListAsync(character.Id, CancellationToken)).Select(entry => (entry.ItemId, entry.Quantity)).ToList();

    // 先扣石頭／道具再寫強化：中途失敗時寧可損失消耗品，也不白給強化
    private void AssertConsumedBeforeEnhanced(long equipmentId)
    {
        var enhanced = inventory.Writes.IndexOf($"enhance:{equipmentId}");
        var consumed = inventory.Writes.FindIndex(write => write.StartsWith("remove:") || write.StartsWith("quantity:"));
        Assert.InRange(consumed, 0, enhanced - 1);
    }

    // 裝備中的道具也能精鍊：擲 1.0 → r = 100 成功，at、df 各擲 [1, 2] 的上界
    [Fact]
    public async Task Refine_EquippedItemCanBeRefined()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-refine-stone");
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.99);

        var result = await service.RefineAsync(AccountId, sword, stone, useSafeStone: false, CancellationToken);

        Assert.True(result.Success);
        AssertConsumedBeforeEnhanced(sword);
        var refined = Find(character, sword);
        Assert.Equal(1, refined.RefineLevel);
        Assert.Equal(StatBonus.Zero with { At = 2, Df = 2 }, refined.RefineBonus);
        Assert.Equal([("test-sword", 1)], await ItemsAsync(character));
        var weapon = Assert.Single(result.Inventory.Equipped);
        Assert.Equal(1, weapon.Item.Entry.RefineLevel);
        Assert.Equal(8 + 2, result.Inventory.Bonus.Total.At);
        Assert.Equal(["update"], log);
    }

    // 擲 0.0 → r = 1 失敗：扣 1 顆安全精鍊石、精鍊石與裝備都保留
    [Fact]
    public async Task Refine_SafeStoneFailureConsumesSafeStone()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-refine-stone");
        await GiveAsync(character, "safe-refine-stone", 2);
        var before = Find(character, sword);
        random.Next = new ScriptedRandom(0.0);

        var result = await service.RefineAsync(AccountId, sword, stone, useSafeStone: true, CancellationToken);

        Assert.False(result.Success);
        Assert.Equal([("test-sword", 1), ("test-refine-stone", 1), ("safe-refine-stone", 1)], await ItemsAsync(character));
        Assert.Equal(before, Find(character, sword));
        Assert.DoesNotContain($"enhance:{sword}", inventory.Writes);
    }

    [Fact]
    public async Task Refine_SafeStoneMissingRejectsWithoutWrites()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-refine-stone");

        var exception = await Assert.ThrowsAsync<InputValidationException>(
            () => service.RefineAsync(AccountId, sword, stone, useSafeStone: true, CancellationToken));

        Assert.Equal(["missingTool:safe-refine-stone"], exception.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Refine_ConflictWritesNothing()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-refine-stone");
        random.Next = new ScriptedRandom(0.99);
        characters.FailNextUpdate = true;
        inventory.Writes.Clear();

        await Assert.ThrowsAsync<ConflictException>(() => service.RefineAsync(AccountId, sword, stone, useSafeStone: false, CancellationToken));

        Assert.Empty(inventory.Writes);
        Assert.Equal([("test-sword", 1), ("test-refine-stone", 1)], await ItemsAsync(character));
        Assert.Equal(0, Find(character, sword).RefineLevel);
    }

    // 精鍊石已扣、寫入強化時失敗：整筆回滾，角色版本與精鍊石都還原，例外照常拋出
    [Fact]
    public async Task Refine_RollsBackWhenEnhancementWriteFails()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-refine-stone");
        random.Next = new ScriptedRandom(0.99);
        inventory.FailWrite = "enhance";

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefineAsync(AccountId, sword, stone, useSafeStone: false, CancellationToken));

        Assert.Equal(["begin", "rollback"], unitOfWork.Log);
        Assert.Equal(character, await characters.FindByAccountAsync(AccountId, CancellationToken));
        Assert.Equal([("test-sword", 1), ("test-refine-stone", 1)], await ItemsAsync(character));
        Assert.Equal(0, Find(character, sword).RefineLevel);
    }

    // 別的角色的道具與不存在的 id 一樣找不到
    [Fact]
    public async Task Refine_OtherCharactersItemIsNotFound()
    {
        var character = await AddCharacterAsync();
        var stone = await GiveAsync(character, "test-refine-stone");
        var otherSword = await inventory.AddAsync(character.Id + 100, TestItems.Entry(0, "test-sword"), CancellationToken);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RefineAsync(AccountId, otherSword, stone, useSafeStone: false, CancellationToken));
        await Assert.ThrowsAsync<NotFoundException>(() => service.RefineAsync(AccountId, 999, stone, useSafeStone: false, CancellationToken));
        Assert.Empty(log);
    }

    [Fact]
    public async Task Socket_InsertsStoneAndConsumesIt()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        var stone = await GiveAsync(character, "test-stone", 3);

        inventory.Writes.Clear();
        var result = await service.SocketAsync(AccountId, sword, 1, stone, CancellationToken);

        Assert.True(result.Success);
        AssertConsumedBeforeEnhanced(sword);
        Assert.Equal([new SocketedStone(1, "test-stone", StatBonus.Zero with { At = 5 })], Find(character, sword).Sockets);
        Assert.Equal([("test-sword", 1), ("test-stone", 2)], await ItemsAsync(character));
        Assert.Equal(["update"], log);
    }

    private async Task<long> SocketedSwordAsync(Character character)
    {
        var sword = await GiveAsync(character, "test-sword");
        await inventory.UpdateEnhancementAsync(
            character.Id, sword, 0, StatBonus.Zero, [new SocketedStone(1, "test-stone", StatBonus.Zero with { At = 5 })], 1, CancellationToken);
        return sword;
    }

    [Fact]
    public async Task Unsocket_RequiresRemover()
    {
        var character = await AddCharacterAsync();
        var sword = await SocketedSwordAsync(character);

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.UnsocketAsync(AccountId, sword, 1, CancellationToken));

        Assert.Equal(["missingTool:stone-remover"], exception.Reasons);
        Assert.Single(Find(character, sword).Sockets);
        Assert.Empty(log);
    }

    // 擲 0.0 → r = 1 < 25 回收：魔石回背包、拆石器用掉、孔位清空
    [Fact]
    public async Task Unsocket_RecoveredStoneReturnsToBag()
    {
        var character = await AddCharacterAsync();
        var sword = await SocketedSwordAsync(character);
        await GiveAsync(character, "stone-remover");
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.0);

        var result = await service.UnsocketAsync(AccountId, sword, 1, CancellationToken);

        Assert.True(result.Recovered);
        AssertConsumedBeforeEnhanced(sword);
        Assert.Empty(Find(character, sword).Sockets);
        Assert.Equal([("test-sword", 1), ("test-stone", 1)], await ItemsAsync(character));
        Assert.Equal(["update"], log);
    }

    // 魔石分類已滿：回收的魔石放不下就消失，不擋拆除
    [Fact]
    public async Task Unsocket_BagFullLosesRecoveredStone()
    {
        var character = await AddCharacterAsync();
        var sword = await SocketedSwordAsync(character);
        await GiveAsync(character, "stone-remover");
        for (var index = 0; index < 15; index++)
        {
            await GiveAsync(character, "test-stone", 99);
        }

        random.Next = new ScriptedRandom(0.0);

        var result = await service.UnsocketAsync(AccountId, sword, 1, CancellationToken);

        Assert.False(result.Recovered);
        Assert.Empty(Find(character, sword).Sockets);
        Assert.Equal(15 * 99, (await ItemsAsync(character)).Where(item => item.ItemId == "test-stone").Sum(item => item.Quantity));
        Assert.DoesNotContain(await ItemsAsync(character), item => item.ItemId == "stone-remover");
    }

    // 擲 0.99 → r = 100 > 8 失敗：孔數不變，鑽孔器照樣用掉
    [Fact]
    public async Task Drill_FailureStillConsumesDriller()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await GiveAsync(character, "hole-driller", 2);
        random.Next = new ScriptedRandom(0.99);

        var result = await service.DrillAsync(AccountId, sword, CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(1, Find(character, sword).Holes);
        Assert.Equal([("test-sword", 1), ("hole-driller", 1)], await ItemsAsync(character));
    }

    [Fact]
    public async Task Drill_SuccessAddsHoleToSameEntry()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await GiveAsync(character, "hole-driller");
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.0);

        var result = await service.DrillAsync(AccountId, sword, CancellationToken);

        Assert.True(result.Success);
        AssertConsumedBeforeEnhanced(sword);
        Assert.Equal(2, Find(character, sword).Holes);
        Assert.Equal([("test-sword", 1)], await ItemsAsync(character));
        Assert.Equal(
            ["missingTool:hole-driller"],
            (await Assert.ThrowsAsync<InputValidationException>(() => service.DrillAsync(AccountId, sword, CancellationToken))).Reasons);
    }

    // 5 顆 Lv1 合 2 組 → 剩 1 顆 Lv1、得 2 顆 Lv2
    [Fact]
    public async Task Merge_ConsumesPairsAndAddsNextLevel()
    {
        var character = await AddCharacterAsync();
        var stone = await GiveAsync(character, "test-refine-stone", 5);

        var result = await service.MergeStonesAsync(AccountId, stone, 2, CancellationToken);

        Assert.True(result.Success);
        Assert.Equal([("test-refine-stone", 1), ("test-refine-stone-2", 2)], await ItemsAsync(character));
        Assert.Equal(["update"], log);
    }

    // 精鍊石分類 15 格全滿、合成後原格還有剩 → 新等級的石頭沒有格子放
    [Fact]
    public async Task Merge_BagFullConsumesNothing()
    {
        var character = await AddCharacterAsync();
        var stone = await GiveAsync(character, "test-refine-stone", 4);
        for (var index = 0; index < 14; index++)
        {
            await GiveAsync(character, "test-refine-stone", 99);
        }

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MergeStonesAsync(AccountId, stone, 1, CancellationToken));

        Assert.Equal(["bagFull"], exception.Reasons);
        Assert.Equal(4, Find(character, stone).Quantity);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Merge_RejectsBadQuantityAndUnmergeableStones()
    {
        var character = await AddCharacterAsync();
        var stone = await GiveAsync(character, "test-refine-stone", 4);
        var top = await GiveAsync(character, "test-refine-stone-2", 2);
        var magic = await GiveAsync(character, "test-stone", 2);

        async Task<IReadOnlyList<string>> ReasonsAsync(long id, int pairs) =>
            (await Assert.ThrowsAsync<InputValidationException>(() => service.MergeStonesAsync(AccountId, id, pairs, CancellationToken))).Reasons;

        Assert.Equal(["invalidQuantity"], await ReasonsAsync(stone, 3));
        Assert.Equal(["invalidQuantity"], await ReasonsAsync(stone, 0));
        Assert.Equal(["invalidQuantity"], await ReasonsAsync(stone, int.MaxValue));
        Assert.Equal(["cannotMerge"], await ReasonsAsync(top, 1));
        Assert.Equal(["notRefineStone"], await ReasonsAsync(magic, 1));
        Assert.Empty(log);
    }

    // 已 +1、有精鍊加成與魔石、2 孔的劍用 Lv2 石頭精鍊，擲 0.0 → r = 1 < 20 失敗：整筆裝備不變、沒有寫入強化，只少一顆精鍊石
    [Fact]
    public async Task ForgeService_RefineFailureConsumesStoneOnly()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await inventory.UpdateEnhancementAsync(
            character.Id, sword, 1, StatBonus.Zero with { At = 2, Df = 1 },
            [new SocketedStone(1, "test-stone", StatBonus.Zero with { At = 5 })], 2, CancellationToken);
        var stone = await GiveAsync(character, "test-refine-stone-2", 2);
        var before = Find(character, sword);
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.0);

        var result = await service.RefineAsync(AccountId, sword, stone, useSafeStone: false, CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(before, Find(character, sword));
        Assert.Equal([$"quantity:{stone}"], inventory.Writes);
        Assert.Equal([("test-sword", 1), ("test-refine-stone-2", 1)], await ItemsAsync(character));
        Assert.Equal(["update"], log);
    }

    // 擲 0.99 → r = 100 ≥ 25 沒回收：孔位照樣清空並寫入、拆石器用掉、魔石不回背包
    [Fact]
    public async Task ForgeService_UnsocketLostStoneStillClearsHole()
    {
        var character = await AddCharacterAsync();
        var sword = await SocketedSwordAsync(character);
        await GiveAsync(character, "stone-remover");
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.99);

        var result = await service.UnsocketAsync(AccountId, sword, 1, CancellationToken);

        Assert.False(result.Recovered);
        Assert.Empty(Find(character, sword).Sockets);
        Assert.Contains($"enhance:{sword}", inventory.Writes);
        Assert.Equal([("test-sword", 1)], await ItemsAsync(character));
    }

    // test-forge-sword：test-ore ×3 → test-sword ×1，成功率 70。擲 0.0 → r = 1 成功、0.99 → r = 100 失敗。
    // 礦石分兩格（4 + 3）：先扣材料再放成品，只扣 1 次成功的 1000
    [Fact]
    public async Task Synthesis_ConsumesMaterialsAndChargesSuccesses()
    {
        var character = await AddCharacterAsync();
        var first = await GiveAsync(character, "test-ore", 4);
        var second = await GiveAsync(character, "test-ore", 3);
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.99, 0.0);

        var result = await service.SynthesizeAsync(AccountId, "test-forge-sword", 2, CancellationToken);

        Assert.Equal((1, 2, 1000L), (result.Successes, result.Attempts, result.Cost));
        Assert.Equal([$"remove:{first}", $"quantity:{second}", "add:test-sword"], inventory.Writes);
        Assert.Equal([("test-ore", 1), ("test-sword", 1)], await ItemsAsync(character));
        Assert.Equal(1000, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Equal(["update"], log);
        Assert.Single(result.Inventory.Bag.Single(bag => bag.Category == ItemCategory.Weapon).Items);
    }

    // 武器分類 15 格全滿：就算可能全部失敗，也要放得下全部成功的成品才合成
    [Fact]
    public async Task Synthesis_RespectsBagCapacity()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 3);
        for (var index = 0; index < InventoryRules.SlotsPerCategory; index++)
        {
            await GiveAsync(character, "test-sword");
        }

        var exception = await Assert.ThrowsAsync<InputValidationException>(
            () => service.SynthesizeAsync(AccountId, "test-forge-sword", 1, CancellationToken));

        Assert.Equal(["bagFull"], exception.Reasons);
        Assert.Equal(3, Find(character, ore).Quantity);
        Assert.Equal(2000, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Empty(log);
    }

    // 預設金錢 2000：合成 3 次可能要付 3000，擲骰前就拒絕
    [Fact]
    public async Task Synthesis_RequiresMoneyForAllAttempts()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 9);

        var exception = await Assert.ThrowsAsync<InputValidationException>(
            () => service.SynthesizeAsync(AccountId, "test-forge-sword", 3, CancellationToken));

        Assert.Equal(["notEnoughMoney"], exception.Reasons);
        Assert.Equal(9, Find(character, ore).Quantity);
        Assert.Empty(log);
    }

    // 扣錢的角色寫入兼版本鎖，必須排在扣材料、放成品之前：衝突時背包與金錢都不動
    [Fact]
    public async Task Synthesis_ConflictWritesNothing()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 3);
        inventory.Writes.Clear();
        random.Next = new ScriptedRandom(0.0);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.SynthesizeAsync(AccountId, "test-forge-sword", 1, CancellationToken));

        Assert.Empty(inventory.Writes);
        Assert.Equal([("test-ore", 3)], await ItemsAsync(character));
        Assert.Equal(3, Find(character, ore).Quantity);
        Assert.Equal(2000, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
    }

    [Fact]
    public async Task Synthesis_UnknownRecipeIsNotFound()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-ore", 3);

        await Assert.ThrowsAsync<NotFoundException>(() => service.SynthesizeAsync(AccountId, "nope", 1, CancellationToken));

        Assert.Empty(log);
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Next { get; set; } = new ScriptedRandom(0.0);

        public IRandomSource Create() => Next;
    }
}
