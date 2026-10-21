using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Inventory;

public sealed record InventoryItem(InventoryEntry Entry, ItemDefinition Item);

// 背包只列出沒有裝備上的道具；每個分類都會列出，含空分類
public sealed record BagCategory(ItemCategory Category, int Capacity, IReadOnlyList<InventoryItem> Items);

// Quantity：補給欄為補給數量，其他欄為 1
public sealed record EquippedItem(EquipmentSlot Slot, InventoryItem Item, int Quantity);

public sealed record InventoryView(IReadOnlyList<BagCategory> Bag, IReadOnlyList<EquippedItem> Equipped, EquipmentBonus Bonus);
