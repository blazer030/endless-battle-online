using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// Domain 測試用：數值與 TestContent/skills.json 相同（sp 3·lv+2、被動 chance 20+10·lv、money 500·lv²、proficiency 300·lv）
public static class TestSkills
{
    public static SkillDefinition Create(string id, string jobId, SkillType type) =>
        Create(id, id, jobId, type, SkillTiming.BeforeAttack, type == SkillType.Active ? SkillEffectKind.AtMultiplier : SkillEffectKind.DfMultiplier, rounds: 3);

    // 戰鬥測試用：rounds 依 plan 的技能表（Heal／Bloodthirst／BonusDamage 為 1，其餘 3）
    public static SkillDefinition Battle(string id, string name, SkillEffectKind effect, SkillType type = SkillType.Active, SkillTiming timing = SkillTiming.BeforeAttack, int rounds = 3) =>
        Create(id, name, "test-job", type, timing, effect, rounds);

    private static SkillDefinition Create(string id, string name, string jobId, SkillType type, SkillTiming timing, SkillEffectKind effect, int rounds) => new(
        id,
        name,
        jobId,
        type,
        timing,
        effect,
        "說明",
        Enumerable.Range(1, 5)
            .Select(level => new SkillLevel(level, 3 * level + 2, rounds, type == SkillType.Passive ? 20 + 10 * level : 0, 500L * level * level, 300L * level))
            .ToList());
}
