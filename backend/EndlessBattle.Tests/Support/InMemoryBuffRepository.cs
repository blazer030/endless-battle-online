using EndlessBattle.Application.Items;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 BuffRepository 一致，同種覆蓋、依種類列出。寫入記為 buff:<kind>
public sealed class InMemoryBuffRepository(List<string>? log = null) : IBuffRepository, IRestorableRepository
{
    private readonly Dictionary<(long CharacterId, BuffKind Kind), CharacterBuff> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<IReadOnlyList<CharacterBuff>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CharacterBuff>>(rows
            .Where(row => row.Key.CharacterId == characterId)
            .Select(row => row.Value)
            .OrderBy(buff => buff.Kind)
            .ToList());

    public Task SaveAsync(long characterId, CharacterBuff buff, CancellationToken cancellationToken)
    {
        rows[(characterId, buff.Kind)] = buff;
        writes.Add($"buff:{buff.Kind}");
        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<(long CharacterId, BuffKind Kind), CharacterBuff>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (key, buff) in saved)
            {
                rows[key] = buff;
            }
        };
    }
}
