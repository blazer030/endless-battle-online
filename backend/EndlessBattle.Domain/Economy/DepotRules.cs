using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Economy;

// Created 的 Id 為 0（尚未寫入）；Updated 是數量改變後的既有道具
public sealed record DepotPlan(IReadOnlyList<string> Reasons, IReadOnlyList<InventoryEntry> Created, IReadOnlyList<InventoryEntry> Updated);

public static class DepotRules
{
    public const int Slots = 20, StackLimit = 254;

    public static int Capacity(int depotBonus) => Slots + depotBonus;

    // 可堆疊的道具與倉庫中同種的合併，合併後超過 254 就拒絕；裝備每件一格，強化原樣帶入。
    // 原因代碼：invalidQuantity、stackLimit、depotFull。綁定的道具也可以存（綁定只擋贈送與交易）
    public static DepotPlan TryStore(
        IReadOnlyList<InventoryEntry> depot, ItemDefinition item, InventoryEntry entry, int quantity, int depotBonus)
    {
        var valid = item.IsEquipment ? quantity == 1 : quantity >= 1 && quantity <= entry.Quantity;
        if (!valid)
        {
            return Rejected("invalidQuantity");
        }

        if (item.Stackable)
        {
            var existing = depot.FirstOrDefault(other => other.ItemId == item.Id);
            if (existing is not null)
            {
                return existing.Quantity + quantity > StackLimit
                    ? Rejected("stackLimit")
                    : new DepotPlan([], [], [existing with { Quantity = existing.Quantity + quantity }]);
            }
        }

        if (depot.Count >= Capacity(depotBonus))
        {
            return Rejected("depotFull");
        }

        return new DepotPlan([], [entry with { Id = 0, Quantity = quantity }], []);
    }

    // 取回背包：可堆疊的走 InventoryRules.TryAdd（背包堆疊上限 99），裝備佔一格並保留強化；
    // sameCategoryBag 是背包中同分類、沒有裝備上的道具。原因代碼：invalidQuantity、bagFull
    public static DepotPlan TryTake(
        IReadOnlyList<InventoryEntry> sameCategoryBag, ItemDefinition item, InventoryEntry entry, int quantity, int bagBonus)
    {
        var valid = item.IsEquipment ? quantity == 1 : quantity >= 1 && quantity <= entry.Quantity;
        if (!valid)
        {
            return Rejected("invalidQuantity");
        }

        if (item.IsEquipment)
        {
            return sameCategoryBag.Count >= InventoryRules.Capacity(item.Category, bagBonus)
                ? Rejected("bagFull")
                : new DepotPlan([], [entry with { Id = 0, Quantity = 1 }], []);
        }

        var added = InventoryRules.TryAdd(sameCategoryBag, item, quantity, bagBonus);
        return added.Discarded > 0 ? Rejected("bagFull") : new DepotPlan([], added.NewEntries, added.UpdatedEntries);
    }

    private static DepotPlan Rejected(string reason) => new([reason], [], []);
}
