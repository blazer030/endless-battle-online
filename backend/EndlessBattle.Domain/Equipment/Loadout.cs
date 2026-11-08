using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

// SupplyQuantity 是補給欄裝備的數量；沒裝備補給時為 0
public sealed record Loadout(IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipped, int SupplyQuantity);
