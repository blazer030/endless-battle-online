using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Inventory;

// 所有方法都以 characterId 為範圍；道具不屬於該角色時丟 InvalidOperationException
public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryEntry>> ListAsync(long characterId, CancellationToken cancellationToken);

    Task<long> AddAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken);

    Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken);

    Task UpdateEnhancementAsync(
        long characterId, long id, int refineLevel, StatBonus refineBonus, IReadOnlyList<SocketedStone> sockets, int holes,
        CancellationToken cancellationToken);

    Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken);
}
