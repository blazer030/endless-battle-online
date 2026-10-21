using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Application.Characters;

public interface IJobProficiencyRepository
{
    // 依職業 id 排序；沒有任何紀錄時回傳空清單
    Task<IReadOnlyList<JobProficiency>> ListAsync(long characterId, CancellationToken cancellationToken);

    // 沒有紀錄就新增，有就把差值加上去
    Task AddAsync(long characterId, string jobId, long proficiencyDelta, long skillPointsDelta, CancellationToken cancellationToken);
}
