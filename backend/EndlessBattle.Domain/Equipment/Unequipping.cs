using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

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
}
