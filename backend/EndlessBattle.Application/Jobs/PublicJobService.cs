using EndlessBattle.Domain.Characters;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Jobs;

public sealed record GrowthRangeView(int Minimum, int Maximum);

public sealed record JobGrowthView(
    GrowthRangeView Strength,
    GrowthRangeView Intelligence,
    GrowthRangeView Life,
    GrowthRangeView Vitality,
    GrowthRangeView Agility,
    GrowthRangeView Charisma,
    GrowthRangeView Faith);

// Stat 用完整名稱（strength、intelligence…）
public sealed record JobStatRequirementView(string Stat, int Minimum);

public sealed record SkillLevelSummaryView(int Level, string Summary);

// Prerequisite 在沒有前置技能時為 null；Levels 從 1 到最高級，Summary 與技能頁同一份文字
public sealed record JobSkillView(
    string Id,
    string Name,
    string Description,
    int MaxLevel,
    SkillPrerequisiteView? Prerequisite,
    IReadOnlyList<SkillLevelSummaryView> Levels);

public sealed record PublicJobView(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<JobStatRequirementView> StatRequirements,
    JobGrowthView Growth,
    IReadOnlyList<JobSkillView> Skills);

// 不用登入就能看的職業列表：職業定義加上各自教的技能，依內容檔中的順序
public sealed class PublicJobService(IGameContent content)
{
    public IReadOnlyList<PublicJobView> GetAll() => content.AllJobs.Select(ToView).ToList();

    private PublicJobView ToView(JobDefinition job) => new(
        job.Id,
        job.Name,
        job.Tier,
        job.ParentId,
        job.UnlockLevel,
        job.ProficiencyRequired,
        job.StatRequirements.Select(requirement => new JobStatRequirementView(FullStatName(requirement.Stat), requirement.Minimum)).ToList(),
        new JobGrowthView(
            Range(job.Growth.Strength),
            Range(job.Growth.Intelligence),
            Range(job.Growth.Life),
            Range(job.Growth.Vitality),
            Range(job.Growth.Agility),
            Range(job.Growth.Charisma),
            Range(job.Growth.Faith)),
        content.SkillsOf(job.Id).Select(ToView).ToList());

    private JobSkillView ToView(SkillDefinition skill) => new(
        skill.Id,
        skill.Name,
        skill.Description,
        skill.MaxLevel,
        skill.Prerequisite is { } prerequisite
            ? new SkillPrerequisiteView(prerequisite.SkillId, content.GetSkill(prerequisite.SkillId).Name, prerequisite.Level)
            : null,
        skill.Levels.Select(level => new SkillLevelSummaryView(level.Level, SkillSummary.Of(level))).ToList());

    private static GrowthRangeView Range(StatRange range) => new(range.Minimum, range.Maximum);

    private static string FullStatName(BaseStat stat) => stat switch
    {
        BaseStat.Strength => "strength",
        BaseStat.Intelligence => "intelligence",
        BaseStat.Life => "life",
        BaseStat.Vitality => "vitality",
        BaseStat.Agility => "agility",
        BaseStat.Charisma => "charisma",
        BaseStat.Faith => "faith",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "未知的能力"),
    };
}
