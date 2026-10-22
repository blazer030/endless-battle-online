using EndlessBattle.Application.Champion;
using EndlessBattle.Domain.Champion;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：新增或覆寫，寫入記為 champion:save
public sealed class InMemoryChampionRepository(List<string>? log = null) : IChampionRepository, IRestorableRepository
{
    private readonly Dictionary<long, ChampionSeat> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<ChampionSeat?> FindAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult(rows.GetValueOrDefault(characterId));

    public Task SaveAsync(long characterId, ChampionSeat seat, CancellationToken cancellationToken)
    {
        rows[characterId] = seat;
        writes.Add("champion:save");
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, ChampionSeat>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (characterId, seat) in saved)
            {
                rows[characterId] = seat;
            }
        };
    }
}
