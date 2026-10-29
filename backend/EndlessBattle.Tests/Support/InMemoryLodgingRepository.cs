using EndlessBattle.Application.Rest;
using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 LodgingRepository 一致，每個角色一筆、整筆取代。寫入記為 lodging
public sealed class InMemoryLodgingRepository(List<string>? log = null) : ILodgingRepository, IRestorableRepository
{
    private readonly Dictionary<long, Lodging> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<Lodging?> FindAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult(rows.TryGetValue(characterId, out var lodging) ? lodging : null);

    public Task SaveAsync(long characterId, Lodging lodging, CancellationToken cancellationToken)
    {
        rows[characterId] = lodging;
        writes.Add("lodging");
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, Lodging>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (characterId, lodging) in saved)
            {
                rows[characterId] = lodging;
            }
        };
    }
}
