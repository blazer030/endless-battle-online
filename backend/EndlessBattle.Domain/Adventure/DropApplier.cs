using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Domain.Adventure;

// NewEntries 的 Id 為 0（尚未寫入）；Discarded 與輸入一一對應，該筆有任何一個放不下即為 true
public sealed record DropApplication(
    IReadOnlyList<InventoryEntry> NewEntries,
    IReadOnlyList<InventoryEntry> UpdatedEntries,
    IReadOnlyList<bool> Discarded);

public static class DropApplier
{
    // 依序一件一件放進背包：先堆疊、再開新格，該分類放不下就丟棄。bag 是背包中未裝備的道具（所有分類）
    public static DropApplication Apply(
        IReadOnlyList<InventoryEntry> bag, IReadOnlyList<string> drops, Func<string, ItemDefinition> items, int bagBonus) =>
        Apply(bag, drops.Select(itemId => new ItemQuantity(itemId, 1)).ToList(), items, bagBonus);

    // 依序一筆一筆放進背包（後面的以前面放完的背包計算），規則同上；任務獎勵也走這裡
    public static DropApplication Apply(
        IReadOnlyList<InventoryEntry> bag, IReadOnlyList<ItemQuantity> drops, Func<string, ItemDefinition> items, int bagBonus)
    {
        // 本批新開的格子先給暫時的負數 Id，後面的掉落才能合併進去；輸出時再還原成 0
        var working = bag.ToList();
        var updatedIds = new HashSet<long>();
        var nextTemporaryId = -1L;
        var discarded = new List<bool>();

        foreach (var (itemId, quantity) in drops)
        {
            var item = items(itemId);
            var sameCategory = working.Where(entry => items(entry.ItemId).Category == item.Category).ToList();
            var result = InventoryRules.TryAdd(sameCategory, item, quantity, bagBonus);
            discarded.Add(result.Discarded > 0);

            foreach (var entry in result.UpdatedEntries)
            {
                working[working.FindIndex(existing => existing.Id == entry.Id)] = entry;
                updatedIds.Add(entry.Id);
            }

            working.AddRange(result.NewEntries.Select(entry => entry with { Id = nextTemporaryId-- }));
        }

        return new DropApplication(
            working.Where(entry => entry.Id < 0).Select(entry => entry with { Id = 0 }).ToList(),
            working.Where(entry => entry.Id > 0 && updatedIds.Contains(entry.Id)).ToList(),
            discarded);
    }
}
