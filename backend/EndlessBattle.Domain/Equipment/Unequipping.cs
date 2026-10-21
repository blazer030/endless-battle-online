using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

// Supply：補給欄併回背包的寫入；沒裝補給時為 null。一般裝備的列原樣回到背包，只要清空欄位
public sealed record UnequipAllResult(AddResult? Supply);

public static class Unequipping
{
    // 卸下的道具放回背包：bag 是整個背包（不含已裝備的列），只看與 item 同分類的格子；放不下回 null（呼叫端回 bagFull，不丟棄）
    public static AddResult? TryReturn(
        IReadOnlyList<InventoryEntry> bag, ItemDefinition item, int quantity, int bagBonus, Func<string, ItemDefinition> getItem)
    {
        var sameCategory = bag.Where(entry => getItem(entry.ItemId).Category == item.Category).ToList();
        var result = InventoryRules.TryAdd(sameCategory, item, quantity, bagBonus);
        return result.Discarded > 0 ? null : result;
    }

    // 轉職時全部卸下（spec §5.2）：依欄位順序逐件放回模擬的背包，任何一件放不下就回 null，整筆都不卸
    public static UnequipAllResult? TryReturnAll(
        IReadOnlyList<InventoryEntry> bag, Loadout loadout, int bagBonus, Func<string, ItemDefinition> getItem)
    {
        var simulated = bag.ToList();
        AddResult? supply = null;
        foreach (var (slot, equipped) in loadout.Equipped.OrderBy(pair => pair.Key))
        {
            var isSupply = slot == EquipmentSlot.Supply;
            var result = TryReturn(simulated, getItem(equipped.ItemId), isSupply ? loadout.SupplyQuantity : 1, bagBonus, getItem);
            if (result is null)
            {
                return null;
            }

            if (isSupply)
            {
                supply = result;
                var updated = result.UpdatedEntries.ToDictionary(entry => entry.Id);
                simulated = [.. simulated.Select(entry => updated.GetValueOrDefault(entry.Id, entry)), .. result.NewEntries];
            }
            else
            {
                simulated.Add(equipped);
            }
        }

        return new UnequipAllResult(supply);
    }
}
