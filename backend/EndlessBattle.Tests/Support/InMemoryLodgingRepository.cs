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

    private readonly Dictionary<long, List<Lodging>> stays = [];

    private List<Lodging> StaysOf(long characterId) =>
        stays.TryGetValue(characterId, out var list) ? list : stays[characterId] = [];

    public Task<Lodging?> FindActiveAsync(long characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(StaysOf(characterId).Where(stay => stay.StartsAt <= now && stay.EndsAt > now).MaxBy(stay => stay.StartsAt));

    public Task<IReadOnlyList<Lodging>> ListEndingAfterAsync(long characterId, DateTimeOffset after, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Lodging>>(StaysOf(characterId).Where(stay => stay.EndsAt > after).OrderBy(stay => stay.StartsAt).ToList());

    public Task SaveBookingAsync(long characterId, Lodging booked, Lodging? ended, DateTimeOffset pruneBefore, CancellationToken cancellationToken)
    {
        var list = StaysOf(characterId);
        if (ended is not null)
        {
            var index = list.FindIndex(stay => stay.StartsAt == ended.StartsAt);
            list[index] = ended;
        }

        list.Add(booked);
        list.RemoveAll(stay => stay.EndsAt < pruneBefore);
        writes.Add("lodging");
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, Lodging>(rows);
        var savedStays = stays.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
        return () =>
        {
            rows.Clear();
            foreach (var (characterId, lodging) in saved)
            {
                rows[characterId] = lodging;
            }

            stays.Clear();
            foreach (var (characterId, list) in savedStays)
            {
                stays[characterId] = list;
            }
        };
    }
}
