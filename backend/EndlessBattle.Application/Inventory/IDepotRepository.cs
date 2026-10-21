using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Inventory;

public interface IDepotRepository
{
    Task<IReadOnlyList<InventoryEntry>> ListAsync(long characterId, CancellationToken cancellationToken);

    Task<long> AddAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken);

    Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken);

    Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken);
}
