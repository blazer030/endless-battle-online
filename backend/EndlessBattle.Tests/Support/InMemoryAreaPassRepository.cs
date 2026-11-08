using EndlessBattle.Application.World;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 AreaPassRepository 一致，同地區覆蓋。寫入記為 pass:<placeId>
public sealed class InMemoryAreaPassRepository(List<string>? log = null) : IAreaPassRepository, IRestorableRepository
{
    private readonly Dictionary<(long CharacterId, int PlaceId), AreaPass> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<AreaPass?> FindAsync(long characterId, int placeId, CancellationToken cancellationToken) =>
        Task.FromResult(rows.GetValueOrDefault((characterId, placeId)));

    public Task<IReadOnlyList<AreaPass>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AreaPass>>(rows
            .Where(row => row.Key.CharacterId == characterId)
            .Select(row => row.Value)
            .OrderBy(pass => pass.PlaceId)
            .ToList());

    public Task SaveAsync(long characterId, AreaPass pass, CancellationToken cancellationToken)
    {
        rows[(characterId, pass.PlaceId)] = pass;
        writes.Add($"pass:{pass.PlaceId}");
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<(long CharacterId, int PlaceId), AreaPass>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (key, pass) in saved)
            {
                rows[key] = pass;
            }
        };
    }
}
