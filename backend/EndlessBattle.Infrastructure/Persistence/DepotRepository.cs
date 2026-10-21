using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class DepotRepository(EndlessBattleDbContext context) : IDepotRepository
{
    public async Task<IReadOnlyList<InventoryEntry>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterDepots.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new InventoryEntry(
            row.Id, row.ItemId, row.Quantity, row.RefineLevel,
            EnhancementJson.DeserializeBonus(row.RefineBonus), EnhancementJson.DeserializeSockets(row.Sockets), row.Holes)).ToList();
    }

    public async Task<long> AddAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken)
    {
        var row = new CharacterDepot
        {
            CharacterId = characterId,
            ItemId = entry.ItemId,
            Quantity = entry.Quantity,
            RefineLevel = entry.RefineLevel,
            RefineBonus = EnhancementJson.SerializeBonus(entry.RefineBonus),
            Sockets = EnhancementJson.SerializeSockets(entry.Sockets),
            Holes = entry.Holes,
        };
        context.CharacterDepots.Add(row);
        await context.SaveChangesAsync(cancellationToken);
        return row.Id;
    }

    public async Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken)
    {
        context.CharacterDepots.Remove(await FindAsync(characterId, id, cancellationToken));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, id, cancellationToken);
        row.Quantity = quantity;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CharacterDepot> FindAsync(long characterId, long id, CancellationToken cancellationToken) =>
        await context.CharacterDepots.SingleOrDefaultAsync(row => row.Id == id && row.CharacterId == characterId, cancellationToken)
        ?? throw new InvalidOperationException("道具不屬於此角色");
}
