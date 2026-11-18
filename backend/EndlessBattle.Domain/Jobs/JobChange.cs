using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Domain.Jobs;

// 代碼：SAME_JOB、LEVEL_TOO_LOW、PARENT_PROFICIENCY_TOO_LOW、STAT_TOO_LOW:<stat>
public sealed record JobChangeCheck(bool Eligible, IReadOnlyList<string> Reasons);

public static class JobChange
{
    public static JobChangeCheck Evaluate(Character character, JobDefinition target, IReadOnlyList<JobProficiency> proficiency)
    {
        var reasons = new List<string>();
        if (character.JobId == target.Id)
        {
            reasons.Add(ErrorCodes.SameJob);
        }

        if (character.Level < target.UnlockLevel)
        {
            reasons.Add(ErrorCodes.LevelTooLow);
        }

        if (target.ParentId is not null)
        {
            var parent = proficiency.FirstOrDefault(row => row.JobId == target.ParentId)?.Proficiency ?? 0;
            if (parent < target.ProficiencyRequired)
            {
                reasons.Add(ErrorCodes.ParentProficiencyTooLow);
            }
        }

        // 與 WOG 一致：比的是未含加成的 Base 能力值
        reasons.AddRange(target.StatRequirements
            .Where(requirement => character.Base.Of(requirement.Stat) < requirement.Minimum)
            .Select(requirement => BaseStatCode.StatTooLow(requirement.Stat)));

        return new JobChangeCheck(reasons.Count == 0, reasons);
    }

    public static Character Apply(Character character, JobDefinition target) => character with { JobId = target.Id };
}
