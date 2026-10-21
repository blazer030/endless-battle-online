namespace EndlessBattle.Domain.Items;

// NewEntries 的 Id 為 0（尚未寫入）；UpdatedEntries 是數量改變後的既有道具
public sealed record AddResult(int Accepted, int Discarded, IReadOnlyList<InventoryEntry> NewEntries, IReadOnlyList<InventoryEntry> UpdatedEntries);

public static class InventoryRules
{
    public const int SlotsPerCategory = 15, StackLimit = 99;

    // 道具（消耗品）與魔石可以擴充
    public static int Capacity(ItemCategory category, int bagBonus) =>
        category is ItemCategory.Consumable or ItemCategory.Stone ? SlotsPerCategory + bagBonus : SlotsPerCategory;

    // bag 是背包中與 item 同分類的道具（不含已裝備的）；先堆疊到同種道具上，再開新格，放不下的丟棄
    public static AddResult TryAdd(IReadOnlyList<InventoryEntry> bag, ItemDefinition item, int quantity, int bagBonus)
    {
        var remaining = quantity;
        var updated = new List<InventoryEntry>();
        if (item.Stackable)
        {
            foreach (var entry in bag.Where(entry => entry.ItemId == item.Id && !IsEnhanced(entry)))
            {
                var added = Math.Min(remaining, StackLimit - entry.Quantity);
                if (added <= 0)
                {
                    continue;
                }

                updated.Add(entry with { Quantity = entry.Quantity + added });
                remaining -= added;
            }
        }

        var created = new List<InventoryEntry>();
        var freeSlots = Capacity(item.Category, bagBonus) - bag.Count;
        var perSlot = item.Stackable ? StackLimit : 1;
        while (remaining > 0 && created.Count < freeSlots)
        {
            var added = Math.Min(remaining, perSlot);
            created.Add(new InventoryEntry(0, item.Id, added, 0, StatBonus.Zero, [], item.Holes));
            remaining -= added;
        }

        return new AddResult(quantity - remaining, remaining, created, updated);
    }

    private static bool IsEnhanced(InventoryEntry entry) =>
        entry.RefineLevel != 0 || entry.Sockets.Count != 0 || entry.RefineBonus != StatBonus.Zero;
}
