using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Missions;

// Updated 是扣除後還有剩的列（新數量），Removed 是整列交出的 id；Missing 非空時其餘欄位不可用
public sealed record DeliveryPlan(IReadOnlyList<InventoryEntry> Updated, IReadOnlyList<long> Removed, IReadOnlyList<string> Missing);

public static class Delivery
{
    // bag 只含背包，不含已裝備與補給欄
    public static IReadOnlyDictionary<string, int> Owned(IReadOnlyList<InventoryEntry> bag) =>
        bag.GroupBy(entry => entry.ItemId).ToDictionary(group => group.Key, group => group.Sum(entry => entry.Quantity));

    // 同一道具先拿未強化的列，再依數量少→多、id 小→大；交出的裝備連同精鍊與鑲嵌一起消失
    public static DeliveryPlan Plan(IReadOnlyList<InventoryEntry> bag, IReadOnlyList<ItemQuantity> deliver)
    {
        var owned = Owned(bag);
        var updated = new List<InventoryEntry>();
        var removed = new List<long>();
        var missing = new List<string>();
        var required = deliver.GroupBy(item => item.ItemId).Select(group => new ItemQuantity(group.Key, group.Sum(item => item.Quantity)));
        foreach (var item in required)
        {
            if (owned.GetValueOrDefault(item.ItemId) < item.Quantity)
            {
                missing.Add(item.ItemId);
                continue;
            }

            var remaining = item.Quantity;
            var rows = bag.Where(entry => entry.ItemId == item.ItemId)
                .OrderBy(InventoryEnhancement.IsEnhanced)
                .ThenBy(entry => entry.Quantity)
                .ThenBy(entry => entry.Id);
            foreach (var row in rows)
            {
                if (remaining == 0)
                {
                    break;
                }

                var taken = Math.Min(remaining, row.Quantity);
                if (taken == row.Quantity)
                {
                    removed.Add(row.Id);
                }
                else
                {
                    updated.Add(row with { Quantity = row.Quantity - taken });
                }

                remaining -= taken;
            }
        }

        return new DeliveryPlan(updated, removed, missing);
    }

    public static IReadOnlyList<InventoryEntry> After(IReadOnlyList<InventoryEntry> bag, DeliveryPlan plan)
    {
        var updated = plan.Updated.ToDictionary(entry => entry.Id);
        return bag.Where(entry => !plan.Removed.Contains(entry.Id))
            .Select(entry => updated.GetValueOrDefault(entry.Id, entry))
            .ToList();
    }
}
