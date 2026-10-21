using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 SkillBookRepository 一樣以技能 id 為鍵設定等級；呼叫順序記在 log 裡
public sealed class InMemorySkillBookRepository(List<string>? log = null) : ISkillBookRepository
{
    private readonly Dictionary<(long CharacterId, string SkillId), LearnedSkill> rows = [];

    public List<string> Log { get; } = log ?? [];

    public Task<IReadOnlyList<LearnedSkill>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LearnedSkill>>(rows
            .Where(row => row.Key.CharacterId == characterId)
            .Select(row => row.Value)
            .OrderBy(row => row.SkillId, StringComparer.Ordinal)
            .ToList());

    public Task UpsertAsync(long characterId, LearnedSkill skill, CancellationToken cancellationToken)
    {
        Log.Add($"book:{skill.SkillId}:{skill.Level}");
        rows[(characterId, skill.SkillId)] = skill;
        return Task.CompletedTask;
    }
}
