using EndlessBattle.Application.Common;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 MissionRepository 一致，重複新增丟 ConflictException，更新不存在的列不做事。
// 寫入記為 mission:add|progress|complete|remove:{missionId}
public sealed class InMemoryMissionRepository(List<string>? log = null) : IMissionRepository, IRestorableRepository
{
    private readonly Dictionary<(long CharacterId, string MissionId), MissionRecord> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<IReadOnlyList<MissionRecord>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MissionRecord>>(rows
            .Where(row => row.Key.CharacterId == characterId)
            .Select(row => row.Value)
            .OrderBy(record => record.AcceptedAt)
            .ToList());

    public Task AddAsync(long characterId, MissionRecord record, CancellationToken cancellationToken)
    {
        if (!rows.TryAdd((characterId, record.MissionId), record))
        {
            throw new ConflictException("任務已存在");
        }

        writes.Add($"mission:add:{record.MissionId}");
        return Task.CompletedTask;
    }

    public Task UpdateProgressAsync(long characterId, string missionId, IReadOnlyList<int> progress, CancellationToken cancellationToken)
    {
        if (rows.TryGetValue((characterId, missionId), out var record))
        {
            rows[(characterId, missionId)] = record with { Progress = progress };
            writes.Add($"mission:progress:{missionId}");
        }

        return Task.CompletedTask;
    }

    public Task CompleteAsync(long characterId, string missionId, DateTimeOffset completedAt, CancellationToken cancellationToken)
    {
        if (rows.TryGetValue((characterId, missionId), out var record))
        {
            rows[(characterId, missionId)] = record with { State = MissionState.Completed, CompletedAt = completedAt };
            writes.Add($"mission:complete:{missionId}");
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(long characterId, string missionId, CancellationToken cancellationToken)
    {
        if (rows.Remove((characterId, missionId)))
        {
            writes.Add($"mission:remove:{missionId}");
        }

        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<(long CharacterId, string MissionId), MissionRecord>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (key, record) in saved)
            {
                rows[key] = record;
            }
        };
    }
}
