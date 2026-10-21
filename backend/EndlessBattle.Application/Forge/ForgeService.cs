using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Forge;

public sealed record ForgeResult(bool Success, InventoryView Inventory);

// Recovered 為 false：沒擲中回收，或背包魔石分類已滿而消失
public sealed record UnsocketResult(bool Recovered, InventoryView Inventory);

// 精鍊與鑲嵌的對象可以在背包或裝備欄；石頭與道具必須在背包。
public sealed record SynthesisResult(int Successes, int Attempts, long Cost, InventoryView Inventory);

// 先以版本鎖寫入角色（內容不變、只遞增版本），成功後才擲骰與寫入背包；衝突時背包不動。
// 寫入沒有交易可包，一律先扣石頭／道具再寫強化：中途失敗時寧可損失消耗品，也不白給強化
public sealed class ForgeService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IGameContent content,
    IRandomSourceFactory randomFactory,
    InventoryService inventoryService)
{
    public async Task<ForgeResult> RefineAsync(
        long accountId, long inventoryId, long stoneInventoryId, bool useSafeStone, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await LoadAsync(character.Id, cancellationToken);
        var target = FindTarget(state, inventoryId);
        var stone = FindInBag(state, stoneInventoryId);
        var targetItem = content.GetItem(target.ItemId);
        var stoneItem = content.GetItem(stone.ItemId);
        Check(Refine.Check(target, targetItem, stoneItem), "無法精鍊");
        var safeStone = useSafeStone ? FindTool(state, ForgeTools.SafeRefineStone) : null;

        await LockAsync(character, cancellationToken);
        var outcome = Refine.Attempt(target, targetItem, stoneItem, useSafeStone, randomFactory.Create());
        if (outcome.ConsumedStone)
        {
            await ConsumeAsync(character.Id, stone, 1, cancellationToken);
        }

        if (outcome.ConsumedSafeStone)
        {
            await ConsumeAsync(character.Id, safeStone!, 1, cancellationToken);
        }

        if (outcome.Success)
        {
            await SaveEnhancementAsync(character.Id, outcome.Entry, cancellationToken);
        }

        return new ForgeResult(outcome.Success, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    public async Task<ForgeResult> SocketAsync(
        long accountId, long inventoryId, int hole, long stoneInventoryId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await LoadAsync(character.Id, cancellationToken);
        var target = FindTarget(state, inventoryId);
        var stone = FindInBag(state, stoneInventoryId);
        var stoneItem = content.GetItem(stone.ItemId);
        Check(Socket.CheckInsert(target, content.GetItem(target.ItemId), hole, stoneItem), "無法鑲嵌");

        await LockAsync(character, cancellationToken);
        var outcome = Socket.Insert(target, hole, stoneItem, randomFactory.Create());
        await ConsumeAsync(character.Id, stone, 1, cancellationToken);
        await SaveEnhancementAsync(character.Id, outcome.Entry, cancellationToken);
        return new ForgeResult(true, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    // 回收的魔石放不下時直接消失，不擋拆除
    public async Task<UnsocketResult> UnsocketAsync(long accountId, long inventoryId, int hole, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await LoadAsync(character.Id, cancellationToken);
        var target = FindTarget(state, inventoryId);
        Check(Socket.CheckRemove(target, content.GetItem(target.ItemId), hole), "無法拆除魔石");
        var remover = FindTool(state, ForgeTools.StoneRemover);

        await LockAsync(character, cancellationToken);
        var outcome = Socket.Remove(target, hole, randomFactory.Create());
        await ConsumeAsync(character.Id, remover, 1, cancellationToken);
        await SaveEnhancementAsync(character.Id, outcome.Entry, cancellationToken);
        var recovered = outcome.Recovered && await TryAddAsync(character, AfterConsuming(state.Bag, remover, 1), content.GetItem(outcome.StoneId), 1, cancellationToken);
        return new UnsocketResult(recovered, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    // 不論成敗都用掉 1 個鑽孔器
    public async Task<ForgeResult> DrillAsync(long accountId, long inventoryId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await LoadAsync(character.Id, cancellationToken);
        var target = FindTarget(state, inventoryId);
        var targetItem = content.GetItem(target.ItemId);
        Check(Socket.CheckDrill(target, targetItem), "無法打洞");
        var driller = FindTool(state, ForgeTools.HoleDriller);

        await LockAsync(character, cancellationToken);
        var outcome = Socket.Drill(target, targetItem, randomFactory.Create());
        await ConsumeAsync(character.Id, driller, 1, cancellationToken);
        if (outcome.Success)
        {
            await SaveEnhancementAsync(character.Id, outcome.Entry, cancellationToken);
        }
        return new ForgeResult(outcome.Success, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    // 同類合成（規則見 StoneMerge）：從同一格扣 2 × quantityPairs 顆，得 quantityPairs 顆同種類 +1 級；放不下就整筆拒絕（400 bagFull）
    public async Task<ForgeResult> MergeStonesAsync(long accountId, long stoneInventoryId, int quantityPairs, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await LoadAsync(character.Id, cancellationToken);
        var stone = FindInBag(state, stoneInventoryId);
        var stoneItem = content.GetItem(stone.ItemId);
        Check(StoneMerge.Check(stoneItem, stone.Quantity, quantityPairs, FindRefineStone), "無法合成");
        var next = StoneMerge.SameKindNext(stoneItem, stoneItem, FindRefineStone) ?? throw new InvalidOperationException("Check 已確認可合成");
        var consumed = quantityPairs * StoneMerge.StonesPerPair;
        var added = InventoryRules.TryAdd(SameCategory(AfterConsuming(state.Bag, stone, consumed), next), next, quantityPairs, character.BagBonus);
        if (added.Discarded > 0)
        {
            throw new InputValidationException("背包已滿", ["bagFull"]);
        }

        await LockAsync(character, cancellationToken);
        await ConsumeAsync(character.Id, stone, consumed, cancellationToken);
        await ApplyAsync(character.Id, added, cancellationToken);
        return new ForgeResult(true, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    // 合成（規則見 Synthesis）：金錢與背包先以全部成功檢查。花費取決於擲骰結果，所以先擲（不寫入），
    // 再以版本鎖扣錢（只算成功的次數），然後扣材料、放成品；衝突時什麼都不寫
    public async Task<SynthesisResult> SynthesizeAsync(long accountId, string recipeId, int attempts, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var recipe = content.AllRecipes.FirstOrDefault(candidate => candidate.Id == recipeId) ?? throw new NotFoundException("找不到配方");
        var state = await LoadAsync(character.Id, cancellationToken);
        Check(Synthesis.Check(recipe, attempts, state.Bag, character.Money, character.BagBonus, content.GetItem), "無法合成");

        var outcome = Synthesis.Attempt(recipe, attempts, state.Bag, randomFactory.Create());
        await characters.UpdateAsync(character with { Money = character.Money - outcome.Cost }, cancellationToken);
        foreach (var taken in Synthesis.TakeMaterials(state.Bag, outcome.MaterialsConsumed))
        {
            await (taken.Quantity > 0
                ? inventory.UpdateQuantityAsync(character.Id, taken.Id, taken.Quantity, cancellationToken)
                : inventory.RemoveAsync(character.Id, taken.Id, cancellationToken));
        }

        if (outcome.Successes > 0)
        {
            var result = content.GetItem(recipe.ResultItemId);
            var bag = Synthesis.AfterTaking(state.Bag, outcome.MaterialsConsumed);
            var added = InventoryRules.TryAdd(SameCategory(bag, result), result, recipe.ResultQuantity * outcome.Successes, character.BagBonus);
            await ApplyAsync(character.Id, added, cancellationToken);
        }

        return new SynthesisResult(outcome.Successes, attempts, outcome.Cost, await inventoryService.GetAsync(accountId, cancellationToken));
    }

    private ItemDefinition? FindRefineStone(int kind, int level) =>
        content.AllItems.FirstOrDefault(item => item.Refine is { } refine && refine.Kind == kind && refine.Level == level);

    // 放得下才寫入並回 true；放不下什麼都不寫
    private async Task<bool> TryAddAsync(
        Character character, IReadOnlyList<InventoryEntry> bag, ItemDefinition item, int quantity, CancellationToken cancellationToken)
    {
        var added = InventoryRules.TryAdd(SameCategory(bag, item), item, quantity, character.BagBonus);
        if (added.Discarded > 0)
        {
            return false;
        }

        await ApplyAsync(character.Id, added, cancellationToken);
        return true;
    }

    private async Task ApplyAsync(long characterId, AddResult added, CancellationToken cancellationToken)
    {
        foreach (var updated in added.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(characterId, updated.Id, updated.Quantity, cancellationToken);
        }

        foreach (var created in added.NewEntries)
        {
            await inventory.AddAsync(characterId, created, cancellationToken);
        }
    }

    private List<InventoryEntry> SameCategory(IReadOnlyList<InventoryEntry> bag, ItemDefinition item) =>
        bag.Where(entry => content.GetItem(entry.ItemId).Category == item.Category).ToList();

    // 扣掉數量後的背包（扣光的格子移除），用來計算放不放得下
    private static List<InventoryEntry> AfterConsuming(IReadOnlyList<InventoryEntry> bag, InventoryEntry consumed, int quantity) =>
        bag.Select(entry => entry.Id == consumed.Id ? entry with { Quantity = entry.Quantity - quantity } : entry)
            .Where(entry => entry.Quantity > 0)
            .ToList();

    private Task SaveEnhancementAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken) =>
        inventory.UpdateEnhancementAsync(characterId, entry.Id, entry.RefineLevel, entry.RefineBonus, entry.Sockets, entry.Holes, cancellationToken);

    private Task ConsumeAsync(long characterId, InventoryEntry entry, int quantity, CancellationToken cancellationToken) =>
        entry.Quantity > quantity
            ? inventory.UpdateQuantityAsync(characterId, entry.Id, entry.Quantity - quantity, cancellationToken)
            : inventory.RemoveAsync(characterId, entry.Id, cancellationToken);

    private static void Check(IReadOnlyList<string> reasons, string message)
    {
        if (reasons.Count > 0)
        {
            throw new InputValidationException(message, reasons);
        }
    }

    private static InventoryEntry FindTool(InventoryState state, string toolId) =>
        state.Bag.FirstOrDefault(entry => entry.ItemId == toolId && entry.Quantity >= 1)
            ?? throw new InputValidationException("缺少需要的道具", [ForgeTools.Missing(toolId)]);

    private static InventoryEntry FindTarget(InventoryState state, long inventoryId) =>
        state.Bag.Concat(state.Loadout.Equipped.Values).FirstOrDefault(entry => entry.Id == inventoryId)
            ?? throw new NotFoundException("找不到這件道具");

    private static InventoryEntry FindInBag(InventoryState state, long inventoryId) =>
        state.Bag.FirstOrDefault(entry => entry.Id == inventoryId) ?? throw new NotFoundException("背包裡沒有這件道具");

    // 裝備欄與背包沒有交易可包：先以版本鎖佔住這次變更，重複送出的請求會在動到背包之前收到 409
    private Task<Character> LockAsync(Character character, CancellationToken cancellationToken) =>
        characters.UpdateAsync(character, cancellationToken);

    private Task<InventoryState> LoadAsync(long characterId, CancellationToken cancellationToken) =>
        BagReader.LoadAsync(inventory, equipment, characterId, cancellationToken);

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
