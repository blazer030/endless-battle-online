using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Pets;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Inventory;

// 裝備欄引用背包表的列：裝上 = 欄位指向該列，卸下 = 清空欄位。已裝備的列不算在背包裡，也不佔背包格數
public sealed class InventoryService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IPetRepository pets,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
    IUnitOfWork unitOfWork)
{
    public async Task<InventoryView> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return await ViewAsync(character, cancellationToken);
    }

    public Task<InventoryView> EquipAsync(long accountId, long inventoryId, int? supplyQuantity, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var state = await LoadAsync(character.Id, cancellationToken);
            var entry = FindInBag(state, inventoryId);
            var item = content.GetItem(entry.ItemId);
            if (item.Consumable is ConsumableKind.Supply or ConsumableKind.Capture)
            {
                await EquipSupplyAsync(character, state, entry, item, supplyQuantity ?? entry.Quantity, cancellationToken);
                return await ViewAsync(character, cancellationToken);
            }

            var reasons = EquipmentRules.CanEquip(character, item);
            if (reasons.Count > 0)
            {
                throw new InputValidationException("無法裝備", reasons);
            }

            // 原本的裝備隨欄位改指向新裝備而回到背包；新裝備同時離開背包，該分類格數不變
            await LockAsync(character, cancellationToken);
            await equipment.SetSlotAsync(character.Id, EquipmentRules.SlotOf(item.Category), entry.Id, null, cancellationToken);
            return await ViewAsync(character, cancellationToken);
        }, cancellationToken);

    public Task<InventoryView> UnequipAsync(long accountId, EquipmentSlot slot, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var state = await LoadAsync(character.Id, cancellationToken);
            var equipped = state.Loadout.Equipped.GetValueOrDefault(slot)
                ?? throw new InputValidationException("這一欄沒有裝備", ["slotEmpty"]);

            if (slot == EquipmentSlot.Supply)
            {
                var returned = TryReturn(state.Bag, equipped, state.Loadout.SupplyQuantity, character.BagBonus);
                await LockAsync(character, cancellationToken);
                await equipment.SetSlotAsync(character.Id, slot, null, null, cancellationToken);
                await ApplyReturnAsync(character.Id, equipped, returned, cancellationToken);
            }
            else
            {
                // 裝備的列本來就在背包表裡，只要確認放得下，清空欄位即可
                TryReturn(state.Bag, equipped, 1, character.BagBonus);
                await LockAsync(character, cancellationToken);
                await equipment.SetSlotAsync(character.Id, slot, null, null, cancellationToken);
            }

            return await ViewAsync(character, cancellationToken);
        }, cancellationToken);

    // 先以版本鎖寫入角色，成功後才扣道具；衝突時道具不動
    public Task<InventoryView> UseAsync(long accountId, long inventoryId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var state = await LoadAsync(character.Id, cancellationToken);
            var entry = FindInBag(state, inventoryId);
            var item = content.GetItem(entry.ItemId);
            var owned = await pets.ListAsync(character.Id, cancellationToken);
            var context = new UseContext(character, content.GetJob(character.JobId), owned.Count, content.AllItems, content.AllMonsters);
            var reasons = ConsumableUse.CanUse(context, item);
            if (reasons.Count > 0)
            {
                throw new InputValidationException("這個道具不能使用", reasons);
            }

            var outcome = ConsumableUse.Apply(context, item, clock.UtcNow, randomFactory.Create());
            var updated = await characters.UpdateAsync(outcome.Character, cancellationToken);
            if (entry.Quantity > 1)
            {
                await inventory.UpdateQuantityAsync(updated.Id, entry.Id, entry.Quantity - 1, cancellationToken);
            }
            else
            {
                await inventory.RemoveAsync(updated.Id, entry.Id, cancellationToken);
            }

            return await ViewAsync(updated, cancellationToken);
        }, cancellationToken);

    // 從背包那一格分出 quantity 個放到補給欄（整格裝上時直接引用該列）；原本的補給品先算好能放回背包才動手
    private async Task EquipSupplyAsync(
        Character character, InventoryState state, InventoryEntry entry, ItemDefinition item, int quantity, CancellationToken cancellationToken)
    {
        if (quantity < 1 || quantity > InventoryRules.StackLimit || quantity > entry.Quantity)
        {
            throw new InputValidationException($"補給數量必須是 1～{InventoryRules.StackLimit}，且不超過持有數量", ["invalidQuantity"]);
        }

        var left = entry with { Quantity = entry.Quantity - quantity };
        var bagAfterSplit = state.Bag
            .Select(other => other.Id == entry.Id ? left : other)
            .Where(other => other.Quantity > 0)
            .ToList();
        var previous = state.Loadout.Equipped.GetValueOrDefault(EquipmentSlot.Supply);
        var returned = previous is null ? null : TryReturn(bagAfterSplit, previous, state.Loadout.SupplyQuantity, character.BagBonus);
        await LockAsync(character, cancellationToken);
        if (previous is not null)
        {
            await equipment.SetSlotAsync(character.Id, EquipmentSlot.Supply, null, null, cancellationToken);
        }

        var supplyId = entry.Id;
        if (left.Quantity > 0)
        {
            await inventory.UpdateQuantityAsync(character.Id, entry.Id, left.Quantity, cancellationToken);
            supplyId = await inventory.AddAsync(character.Id, entry with { Id = 0, Quantity = quantity }, cancellationToken);
        }

        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Supply, supplyId, quantity, cancellationToken);
        if (previous is not null)
        {
            await ApplyReturnAsync(character.Id, previous, returned!, cancellationToken);
        }
    }

    // 卸下的道具要放回背包：同分類放不下就整筆拒絕（400 bagFull），不丟棄
    private AddResult TryReturn(IReadOnlyList<InventoryEntry> bag, InventoryEntry equipped, int quantity, int bagBonus) =>
        Unequipping.TryReturn(bag, content.GetItem(equipped.ItemId), quantity, bagBonus, content.GetItem) ?? throw BagFull();

    // 欄位必須先清空：裝備欄引用背包列，刪除仍被引用的列會撞外鍵
    private async Task ApplyReturnAsync(long characterId, InventoryEntry equipped, AddResult result, CancellationToken cancellationToken)
    {
        foreach (var updated in result.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(characterId, updated.Id, updated.Quantity, cancellationToken);
        }

        foreach (var created in result.NewEntries)
        {
            await inventory.AddAsync(characterId, created, cancellationToken);
        }

        await inventory.RemoveAsync(characterId, equipped.Id, cancellationToken);
    }

    // 交易中第一個寫入：以版本鎖寫入角色（欄位不變、只遞增版本）鎖住角色列，
    // 同一角色的並行或重複請求排在後面，因版本不符收到 409
    private Task<Character> LockAsync(Character character, CancellationToken cancellationToken) =>
        characters.UpdateAsync(character, cancellationToken);

    private static InputValidationException BagFull() => new("背包已滿", ["bagFull"]);

    private Task<InventoryState> LoadAsync(long characterId, CancellationToken cancellationToken) =>
        BagReader.LoadAsync(inventory, equipment, characterId, cancellationToken);

    private static InventoryEntry FindInBag(InventoryState state, long inventoryId)
    {
        if (state.Loadout.Equipped.Values.Any(entry => entry.Id == inventoryId))
        {
            throw new InputValidationException("這件道具已經裝備中", ["alreadyEquipped"]);
        }

        return state.Bag.FirstOrDefault(entry => entry.Id == inventoryId)
            ?? throw new InputValidationException("背包裡沒有這件道具", ["itemNotFound"]);
    }

    private async Task<InventoryView> ViewAsync(Character character, CancellationToken cancellationToken)
    {
        var state = await LoadAsync(character.Id, cancellationToken);
        var items = state.Bag.Select(entry => InventoryItem.From(entry, content.GetItem)).ToList();
        var bag = Enum.GetValues<ItemCategory>()
            .Select(category => new BagCategory(
                category,
                InventoryRules.Capacity(category, character.BagBonus),
                items.Where(item => item.Item.Category == category).ToList()))
            .ToList();
        var equipped = state.Loadout.Equipped
            .OrderBy(pair => pair.Key)
            .Select(pair => new EquippedItem(
                pair.Key,
                InventoryItem.From(pair.Value, content.GetItem),
                pair.Key == EquipmentSlot.Supply ? state.Loadout.SupplyQuantity : 1))
            .ToList();
        return new InventoryView(bag, equipped, EquipmentBonusCalculator.Sum(state.Loadout, content.GetItem));
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
