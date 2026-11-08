using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 InventoryRepository 一致，道具不屬於該角色時丟 InvalidOperationException；
// 刪除仍在裝備欄的道具時模擬外鍵失敗
public sealed class InMemoryInventoryRepository(List<string>? log = null) : IInventoryRepository, IRestorableRepository
{
    private readonly Dictionary<long, (long CharacterId, InventoryEntry Entry)> rows = [];
    private long nextId = 1;

    // 由 InMemoryEquipmentRepository 設定
    public Func<long, bool> IsEquipped { get; set; } = _ => false;

    // 依序記錄寫入：add:<itemId>、quantity:<id>、enhance:<id>、remove:<id>；傳入 log 時與其他 repository 共用
    public List<string> Writes { get; } = log ?? [];

    // 設為 add、quantity、enhance 或 remove 時，該種寫入一律丟 InvalidOperationException，模擬資料庫寫入失敗
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
        Writes.Add($"add:{entry.ItemId}");
        return Task.FromResult(id);
    }

    public Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken)
    {
        ThrowIfFailing("quantity");
        rows[id] = (characterId, Find(characterId, id) with { Quantity = quantity });
        Writes.Add($"quantity:{id}");
        return Task.CompletedTask;
    }

    public Task UpdateEnhancementAsync(
        long characterId, long id, int refineLevel, StatBonus refineBonus, IReadOnlyList<SocketedStone> sockets, int holes,
        CancellationToken cancellationToken)
    {
        ThrowIfFailing("enhance");
        rows[id] = (characterId, Find(characterId, id) with { RefineLevel = refineLevel, RefineBonus = refineBonus, Sockets = sockets, Holes = holes });
        Writes.Add($"enhance:{id}");
        return Task.CompletedTask;
    }

    public Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken)
    {
        ThrowIfFailing("remove");
        Find(characterId, id);
        if (IsEquipped(id))
        {
            throw new InvalidOperationException("外鍵：道具仍在裝備欄");
        }

        rows.Remove(id);
        Writes.Add($"remove:{id}");
        return Task.CompletedTask;
    }

    public InventoryEntry Find(long characterId, long id) =>
        rows.TryGetValue(id, out var row) && row.CharacterId == characterId
            ? row.Entry
            : throw new InvalidOperationException("道具不屬於此角色");

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
}
