using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class EquipmentRepository(EndlessBattleDbContext context) : IEquipmentRepository
{
    public async Task<Loadout> GetAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterEquipments.AsNoTracking()
            .Include(row => row.Inventory)
            .Where(row => row.CharacterId == characterId)
            .ToListAsync(cancellationToken);
        var equipped = rows.ToDictionary(row => Enum.Parse<EquipmentSlot>(row.Slot), row => InventoryRepository.ToEntry(row.Inventory));
        var supply = rows.SingleOrDefault(row => row.Slot == nameof(EquipmentSlot.Supply))?.SupplyQuantity ?? 0;
        return new Loadout(equipped, supply);
    }

    public async Task SetSlotAsync(long characterId, EquipmentSlot slot, long? inventoryId, int? supplyQuantity, CancellationToken cancellationToken)
    {
        var slotName = slot.ToString();
        var existing = await context.CharacterEquipments
            .SingleOrDefaultAsync(row => row.CharacterId == characterId && row.Slot == slotName, cancellationToken);

        if (inventoryId is null)
        {
            if (existing is not null)
            {
                context.CharacterEquipments.Remove(existing);
                await context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var owned = await context.CharacterInventories.AnyAsync(
            row => row.Id == inventoryId && row.CharacterId == characterId, cancellationToken);
        if (!owned)
        {
            throw new InvalidOperationException("道具不屬於此角色");
        }

        if (existing is null)
        {
            context.CharacterEquipments.Add(new CharacterEquipment
            {
                CharacterId = characterId,
                Slot = slotName,
                InventoryId = inventoryId.Value,
                SupplyQuantity = supplyQuantity,
            });
        }
        else
        {
            existing.InventoryId = inventoryId.Value;
            existing.SupplyQuantity = supplyQuantity;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
