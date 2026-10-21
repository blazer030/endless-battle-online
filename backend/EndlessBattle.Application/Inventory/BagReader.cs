using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Inventory;

// Bag 是背包表中沒有被裝備欄引用的列
internal sealed record InventoryState(IReadOnlyList<InventoryEntry> Bag, Loadout Loadout);

internal static class BagReader
{
    public static async Task<InventoryState> LoadAsync(
        IInventoryRepository inventory, IEquipmentRepository equipment, long characterId, CancellationToken cancellationToken)
    {
        var entries = await inventory.ListAsync(characterId, cancellationToken);
        var loadout = await equipment.GetAsync(characterId, cancellationToken);
        var equippedIds = loadout.Equipped.Values.Select(entry => entry.Id).ToHashSet();
        return new InventoryState(entries.Where(entry => !equippedIds.Contains(entry.Id)).ToList(), loadout);
    }
}
