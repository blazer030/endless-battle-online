using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// Domain 測試用：數值與 TestContent/skills.json 相同（主動 SP 消耗 3·lv+2、發動率 30+10·lv；常駐被動沒有條件、消耗與持續回合；
// 觸發型被動不耗 SP、持續 rounds、chance 型發動率 30+10·lv；hpBelow 型只看 HP；money 500·lv²、proficiency 300·lv）。
// effect 是舊格式的效果名，轉成數值等價的條件、消耗與效果
public static class TestSkills
{
    public static SkillDefinition Create(string id, string jobId, SkillType type) =>
        Create(id, id, jobId, type, SkillTiming.BeforeAttack, type == SkillType.Active ? SkillEffectKind.AtMultiplier : SkillEffectKind.DfMultiplier, rounds: 3);

    // 戰鬥測試用：rounds 依 plan 的技能表（Heal／BonusDamage 為 1，其餘 3）；cooldown 預設 0（無冷卻）；
    // trigger 預設 null（主動擲發動率、被動常駐）
    public static SkillDefinition Battle(
        string id, string name, SkillEffectKind effect, SkillType type = SkillType.Active, SkillTiming timing = SkillTiming.BeforeAttack, int rounds = 3,
        int cooldown = 0, SkillTrigger? trigger = null) =>
        Create(id, name, "test-job", type, timing, effect, rounds, cooldown, trigger);

    private static SkillDefinition Create(
        string id, string name, string jobId, SkillType type, SkillTiming timing, SkillEffectKind effect, int rounds, int cooldown = 0,
        SkillTrigger? trigger = null)
    {
        var alwaysOn = type == SkillType.Passive && trigger is null;
        return new SkillDefinition(
            id,
            name,
            jobId,
            type,
            timing,
            Effect: null,
            "說明",
            [.. Enumerable.Range(1, 5).Select(level => new SkillLevel(
                level, Sp: 0, Rounds: 0, Chance: 0, 500L * level * level, 300L * level, cooldown,
                Conditions(type, trigger, level),
                type == SkillType.Active ? [new SkillCost(SkillCostKind.SkillPoints, 3 * level + 2)] : [],
                Effects(effect, level, alwaysOn ? 0 : rounds)))]);
    }

    private static IReadOnlyList<SkillCondition> Conditions(SkillType type, SkillTrigger? trigger, int level) => trigger switch
    {
        { Kind: SkillTriggerKind.HpBelow, Percent: var percent } => [new SkillCondition(SkillConditionKind.HitPointsBelow, percent)],
        null when type == SkillType.Passive => [],
        _ => [new SkillCondition(SkillConditionKind.Chance, 30 + 10 * level)],
    };

    private static StatChangeEffect Stat(SkillStat stat, double? percent, int rounds, SkillTarget target = SkillTarget.Caster, int? amount = null,
        IReadOnlyList<SkillScaling>? scaling = null) =>
        new(target, stat, percent, amount, scaling, rounds);

    private static IReadOnlyList<SkillEffect> Effects(SkillEffectKind effect, int n, int rounds) => effect switch
    {
        SkillEffectKind.AtMultiplier => [Stat(SkillStat.Attack, n, rounds)],
        SkillEffectKind.MatMultiplier => [Stat(SkillStat.MagicAttack, n, rounds)],
        SkillEffectKind.DfMultiplier => [Stat(SkillStat.Defense, n, rounds)],
        SkillEffectKind.MdfMultiplier => [Stat(SkillStat.MagicDefense, n, rounds)],
        SkillEffectKind.AgiMultiplier => [Stat(SkillStat.Agility, n, rounds)],
        SkillEffectKind.MultiHit => [new MultiHitEffect(SkillAttackKind.Physical, n + 1, rounds), Stat(SkillStat.Attack, -n, rounds)],
        SkillEffectKind.MagicMultiHit => [new MultiHitEffect(SkillAttackKind.Magic, n + 1, rounds)],
        SkillEffectKind.EnemyAgiDown => [Stat(SkillStat.Agility, -n, rounds, SkillTarget.Opponent)],
        SkillEffectKind.EnemyAttackDown =>
            [Stat(SkillStat.Attack, -2 * n, rounds, SkillTarget.Opponent), Stat(SkillStat.MagicAttack, -2 * n, rounds, SkillTarget.Opponent)],
        SkillEffectKind.Heal =>
            [new RestoreEffect(SkillResource.HitPoints, [new SkillScaling(SkillScalingBasis.Intelligence, 1 + 0.375 * (n - 1))], null, rounds)],
        SkillEffectKind.WarGodWrath =>
            [Stat(SkillStat.Attack, null, rounds, scaling: [new SkillScaling(SkillScalingBasis.Strength, 0.45)]), Stat(SkillStat.HitRate, null, rounds, amount: -75 + 5 * n)],
        SkillEffectKind.Counter => [new DamageReductionEffect(SkillAttackKind.Physical, 8 + 2 * n, rounds), Stat(SkillStat.Attack, n, rounds)],
        SkillEffectKind.CritChanceUp => [Stat(SkillStat.PhysicalCriticalChance, null, rounds, amount: 2 * n)],
        SkillEffectKind.Bloodthirst => [new RestoreEffect(SkillResource.HitPoints, null, PercentOfDamage: 2 * n, rounds)],
        SkillEffectKind.BonusDamage => [new BonusDamageEffect(SkillAttackKind.Any, [new SkillScaling(SkillScalingBasis.Charisma, 0.08 * n)], rounds)],
        SkillEffectKind.Stun => [new StunEffect(3 * n, rounds)],
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "未知的技能效果"),
    };
}
