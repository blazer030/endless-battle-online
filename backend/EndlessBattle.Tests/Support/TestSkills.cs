using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// Domain 測試用：數值與 TestContent/skills.json 相同（主動 sp 3·lv+2、發動率 30+10·lv；被動常駐，sp／rounds／chance 都是 0；
// money 500·lv²、proficiency 300·lv）
public static class TestSkills
{
    public static SkillDefinition Create(string id, string jobId, SkillType type) =>
        Create(id, id, jobId, type, SkillTiming.BeforeAttack, type == SkillType.Active ? SkillEffectKind.AtMultiplier : SkillEffectKind.DfMultiplier, rounds: 3);

    // 戰鬥測試用：rounds 依 plan 的技能表（Heal／BonusDamage 為 1，其餘 3）；cooldown 預設 0（無冷卻）；
    // trigger 預設 null（主動擲發動率、被動常駐），hpBelow 的主動技 chance 照內容規則為 0
    public static SkillDefinition Battle(
        string id, string name, SkillEffectKind effect, SkillType type = SkillType.Active, SkillTiming timing = SkillTiming.BeforeAttack, int rounds = 3,
        int cooldown = 0, SkillTrigger? trigger = null) =>
        Create(id, name, "test-job", type, timing, effect, rounds, cooldown, trigger);

    private static SkillDefinition Create(
        string id, string name, string jobId, SkillType type, SkillTiming timing, SkillEffectKind effect, int rounds, int cooldown = 0,
        SkillTrigger? trigger = null) => new(
        id,
        name,
        jobId,
        type,
        timing,
        effect,
        "說明",
        Enumerable.Range(1, 5)
            .Select(level => type == SkillType.Active
                ? new SkillLevel(level, 3 * level + 2, rounds, trigger?.Kind == SkillTriggerKind.HpBelow ? 0 : 30 + 10 * level, 500L * level * level, 300L * level, cooldown)
                : new SkillLevel(level, Sp: 0, Rounds: 0, Chance: 0, 500L * level * level, 300L * level, cooldown))
            .ToList(),
        trigger);
}
