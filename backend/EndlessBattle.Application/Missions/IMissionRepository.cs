using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Application.Missions;

// 全部以 characterId 為範圍，時間以 UTC 存取。
// UpdateProgressAsync、CompleteAsync、RemoveAsync 遇到不存在的列時不做事也不丟例外，狀態由服務層先驗證
public interface IMissionRepository
{
    Task<IReadOnlyList<MissionRecord>> ListAsync(long characterId, CancellationToken cancellationToken);

    // 同角色同任務已有一列 → ConflictException
    Task AddAsync(long characterId, MissionRecord record, CancellationToken cancellationToken);

    Task UpdateProgressAsync(long characterId, string missionId, int progress, CancellationToken cancellationToken);

    Task CompleteAsync(long characterId, string missionId, DateTimeOffset completedAt, CancellationToken cancellationToken);

    Task RemoveAsync(long characterId, string missionId, CancellationToken cancellationToken);
}
