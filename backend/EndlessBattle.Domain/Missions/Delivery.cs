using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Missions;

public static class Delivery
{
    // bag 只含背包，不含已裝備與補給欄
    public static IReadOnlyDictionary<string, int> Owned(IReadOnlyList<InventoryEntry> bag) =>
        bag.GroupBy(entry => entry.ItemId).ToDictionary(group => group.Key, group => group.Sum(entry => entry.Quantity));
}
