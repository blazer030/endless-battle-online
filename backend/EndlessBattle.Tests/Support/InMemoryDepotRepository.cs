using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 DepotRepository 一致，道具不屬於該角色時丟 InvalidOperationException
public sealed class InMemoryDepotRepository : IDepotRepository, IRestorableRepository
{
    private readonly Dictionary<long, (long CharacterId, InventoryEntry Entry)> rows = [];
    private long nextId = 1;

    // 設為 add、quantity 或 remove 時，該種寫入一律丟 InvalidOperationException，模擬資料庫寫入失敗
    public string? FailWrite { get; set; }

    public Task<IReadOnlyList<InventoryEntry>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InventoryEntry>>(rows.Values
            .Where(row => row.CharacterId == characterId)
            .Select(row => row.Entry)
            .OrderBy(entry => entry.Id)
            .ToList());

    public Task<long> AddAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken)
    {
        ThrowIfFailing("add");
        var id = nextId++;
        rows[id] = (characterId, entry with { Id = id });
        return Task.FromResult(id);
    }

    public Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken)
    {
        ThrowIfFailing("remove");
        Find(characterId, id);
        rows.Remove(id);
        return Task.CompletedTask;
    }

    public Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken)
    {
        ThrowIfFailing("quantity");
        rows[id] = (characterId, Find(characterId, id) with { Quantity = quantity });
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, (long CharacterId, InventoryEntry Entry)>(rows);
        var savedNextId = nextId;
        return () =>
        {
            rows.Clear();
            foreach (var (id, row) in saved)
            {
                rows[id] = row;
            }

            nextId = savedNextId;
        };
    }

    private void ThrowIfFailing(string write)
    {
        if (FailWrite == write)
        {
            throw new InvalidOperationException($"模擬寫入失敗：{write}");
        }
    }

    private InventoryEntry Find(long characterId, long id) =>
        rows.TryGetValue(id, out var row) && row.CharacterId == characterId
            ? row.Entry
            : throw new InvalidOperationException("道具不屬於此角色");
}
