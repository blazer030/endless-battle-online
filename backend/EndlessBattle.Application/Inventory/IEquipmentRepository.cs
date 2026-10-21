using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Application.Inventory;

public interface IEquipmentRepository
{
    Task<Loadout> GetAsync(long characterId, CancellationToken cancellationToken);

    // inventoryId 為 null 表示清空該欄；背包道具不屬於該角色時丟 InvalidOperationException
    Task SetSlotAsync(long characterId, EquipmentSlot slot, long? inventoryId, int? supplyQuantity, CancellationToken cancellationToken);
}
