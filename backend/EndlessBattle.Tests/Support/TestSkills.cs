using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// Domain 測試用：數值與 TestContent/skills.json 相同（sp 3·lv+2、被動 chance 20+10·lv、money 500·lv²、proficiency 300·lv）
public static class TestSkills
{
    public static SkillDefinition Create(string id, string jobId, SkillType type) => new(
        id,
        id,
        jobId,
        type,
        SkillTiming.BeforeAttack,
        type == SkillType.Active ? SkillEffectKind.AtMultiplier : SkillEffectKind.DfMultiplier,
        "說明",
        Enumerable.Range(1, 5)
            .Select(level => new SkillLevel(level, 3 * level + 2, 3, type == SkillType.Passive ? 20 + 10 * level : 0, 500L * level * level, 300L * level))
            .ToList());
}
