using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Domain.Jobs;

// 每個條件各自報告有沒有達成（職業清單用）；不能轉時的錯誤代碼依序 SAME_JOB、LEVEL_TOO_LOW、PARENT_PROFICIENCY_TOO_LOW、STAT_TOO_LOW:<stat>
public sealed record StatCheck(BaseStat Stat, int Minimum, bool Met);

public sealed record JobChangeCheck(bool Current, bool LevelMet, bool ParentProficiencyMet, IReadOnlyList<StatCheck> Stats)
{
    public bool Eligible => !Current && LevelMet && ParentProficiencyMet && Stats.All(stat => stat.Met);

    public IReadOnlyList<string> Reasons =>
    [
        .. Current ? [ErrorCodes.SameJob] : Array.Empty<string>(),
        .. LevelMet ? [] : new[] { ErrorCodes.LevelTooLow },
        .. ParentProficiencyMet ? [] : new[] { ErrorCodes.ParentProficiencyTooLow },
        .. Stats.Where(stat => !stat.Met).Select(stat => BaseStatCode.StatTooLow(stat.Stat)),
    ];
}

public static class JobChange
{
    public static JobChangeCheck Evaluate(Character character, JobDefinition target, IReadOnlyList<JobProficiency> proficiency)
    {
        var parentProficiencyMet = target.ParentId is null
            || (proficiency.FirstOrDefault(row => row.JobId == target.ParentId)?.Proficiency ?? 0) >= target.ProficiencyRequired;

        // 與 WOG 一致：比的是未含加成的 Base 能力值
        var stats = target.StatRequirements
            .Select(requirement => new StatCheck(requirement.Stat, requirement.Minimum, character.Base.Of(requirement.Stat) >= requirement.Minimum))
            .ToList();

        return new JobChangeCheck(character.JobId == target.Id, character.Level >= target.UnlockLevel, parentProficiencyMet, stats);
    }

    public static Character Apply(Character character, JobDefinition target) => character with { JobId = target.Id };
}
