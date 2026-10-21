using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Domain.Jobs;

// 代碼：sameJob、levelTooLow、parentProficiencyTooLow、statTooLow:<stat>
public sealed record JobChangeCheck(bool Eligible, IReadOnlyList<string> Reasons);

public static class JobChange
{
    public static JobChangeCheck Evaluate(Character character, JobDefinition target, IReadOnlyList<JobProficiency> proficiency)
    {
        var reasons = new List<string>();
        if (character.JobId == target.Id)
        {
            reasons.Add("sameJob");
        }

        if (character.Level < target.UnlockLevel)
        {
            reasons.Add("levelTooLow");
        }

        if (target.ParentId is not null)
        {
            var parent = proficiency.FirstOrDefault(row => row.JobId == target.ParentId)?.Proficiency ?? 0;
            if (parent < target.ProficiencyRequired)
            {
                reasons.Add("parentProficiencyTooLow");
            }
        }

        // 與 WOG 一致：比的是未含加成的 Base 能力值
        reasons.AddRange(target.StatRequirements
            .Where(requirement => BaseStatOf(character.Base, requirement.Stat) < requirement.Minimum)
            .Select(requirement => $"statTooLow:{requirement.Stat}"));

        return new JobChangeCheck(reasons.Count == 0, reasons);
    }

    public static Character Apply(Character character, JobDefinition target) => character with { JobId = target.Id };

    private static int BaseStatOf(BaseStats stats, string stat) => stat switch
    {
        "str" => stats.Str,
        "smart" => stats.Smart,
        "life" => stats.Life,
        "vit" => stats.Vit,
        "agi" => stats.Agi,
        "au" => stats.Au,
        "be" => stats.Be,
        _ => throw new ArgumentException($"未知的能力值：{stat}", nameof(stat)),
    };
}
