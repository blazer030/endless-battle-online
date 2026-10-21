using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 EquipmentRepository 一致，讀取時帶出背包道具的最新內容；不檢查同一件道具是否佔了兩欄
public sealed class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly InMemoryInventoryRepository inventory;
    private readonly Dictionary<(long CharacterId, EquipmentSlot Slot), (long InventoryId, int? SupplyQuantity)> slots = [];

    public InMemoryEquipmentRepository(InMemoryInventoryRepository inventory)
    {
        this.inventory = inventory;
        inventory.IsEquipped = id => slots.Values.Any(slot => slot.InventoryId == id);
    }

    public Task<Loadout> GetAsync(long characterId, CancellationToken cancellationToken)
    {
        var own = slots.Where(slot => slot.Key.CharacterId == characterId).ToList();
        var equipped = own.ToDictionary(slot => slot.Key.Slot, slot => inventory.Find(characterId, slot.Value.InventoryId));
        var supply = own.SingleOrDefault(slot => slot.Key.Slot == EquipmentSlot.Supply).Value.SupplyQuantity ?? 0;
        return Task.FromResult(new Loadout(equipped, supply));
    }

    public Task SetSlotAsync(long characterId, EquipmentSlot slot, long? inventoryId, int? supplyQuantity, CancellationToken cancellationToken)
    {
        if (inventoryId is null)
        {
            slots.Remove((characterId, slot));
            return Task.CompletedTask;
        }

        inventory.Find(characterId, inventoryId.Value);
        slots[(characterId, slot)] = (inventoryId.Value, supplyQuantity);
        return Task.CompletedTask;
    }
}
