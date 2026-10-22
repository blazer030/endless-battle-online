using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class EquipmentRepository(EndlessBattleDbContext context, IGameContent content) : IEquipmentRepository
{
    public async Task<Loadout> GetAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterEquipments.AsNoTracking()
            .Include(row => row.Inventory)
            .Where(row => row.CharacterId == characterId)
            .ToListAsync(cancellationToken);
        // 引用的道具已不在內容裡時視為空欄
        var equipped = new Dictionary<EquipmentSlot, InventoryEntry>();
        foreach (var row in rows)
        {
            if (InventoryRepository.ToEntry(row.Inventory, content) is { } entry)
            {
                equipped[Enum.Parse<EquipmentSlot>(row.Slot)] = entry;
            }
        }

        var supply = equipped.ContainsKey(EquipmentSlot.Supply)
            ? rows.Single(row => row.Slot == nameof(EquipmentSlot.Supply)).SupplyQuantity ?? 0
            : 0;
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
