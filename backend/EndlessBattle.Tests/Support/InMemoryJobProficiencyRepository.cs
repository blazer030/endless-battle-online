using EndlessBattle.Application.Characters;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 JobProficiencyRepository 一樣累加差值；呼叫順序記在 log 裡
public sealed class InMemoryJobProficiencyRepository(List<string>? log = null) : IJobProficiencyRepository
{
    private readonly Dictionary<(long CharacterId, string JobId), JobProficiency> rows = [];

    public List<string> Log { get; } = log ?? [];

    public Task<IReadOnlyList<JobProficiency>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<JobProficiency>>(rows
            .Where(row => row.Key.CharacterId == characterId)
            .Select(row => row.Value)
            .OrderBy(row => row.JobId, StringComparer.Ordinal)
            .ToList());

    public Task AddAsync(long characterId, string jobId, long proficiencyDelta, long skillPointsDelta, CancellationToken cancellationToken)
    {
        Log.Add($"proficiency:{jobId}");
        var existing = rows.GetValueOrDefault((characterId, jobId)) ?? new JobProficiency(jobId, 0, 0);
        rows[(characterId, jobId)] = existing with
        {
            Proficiency = existing.Proficiency + proficiencyDelta,
            SkillPoints = existing.SkillPoints + skillPointsDelta,
        };
        return Task.CompletedTask;
    }
}
