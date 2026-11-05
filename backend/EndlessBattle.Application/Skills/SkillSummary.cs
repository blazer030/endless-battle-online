using System.Globalization;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

// 技能頁與戰鬥紀錄用的效果文字，由每級的條件、消耗、效果產生；description 是另外寫的技能說明，與效果無關
public static class SkillSummary
{
    public static string Of(SkillLevel level)
    {
        var parts = new List<string>();
        if (level.Conditions is [])
        {
            parts.Add("常駐");
        }

        parts.AddRange(level.Conditions.Select(Condition));
        parts.AddRange(level.Costs.Select(Cost));
        parts.Add(string.Join("、", level.Effects.Select(Effect)));
        if (level.Cooldown > 0)
        {
            parts.Add($"冷卻 {level.Cooldown} 回合");
        }

        return string.Join("｜", parts);
    }

    private static string Condition(SkillCondition condition) => condition.Kind switch
    {
        SkillConditionKind.Chance => $"機率 {condition.Percent}%",
        SkillConditionKind.HitPointsBelow => $"HP 低於 {condition.Percent}%",
        SkillConditionKind.HitPointsBelowOpponent => "HP 比對手低",
        SkillConditionKind.StatAbove => $"{BasisName(condition.Stat!.Value)}比對手高",
        _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Kind, "未知的發動條件"),
    };

    private static string Cost(SkillCost cost) => cost.Kind switch
    {
        SkillCostKind.SkillPoints when cost.All => "消耗全部 SP",
        SkillCostKind.SkillPoints => $"消耗 SP {CostAmount(cost)}",
        SkillCostKind.HitPoints => $"消耗 HP {CostAmount(cost)}",
        SkillCostKind.Money => $"消耗金錢 {cost.Amount}",
        _ => throw new ArgumentOutOfRangeException(nameof(cost), cost.Kind, "未知的消耗"),
    };

    private static string CostAmount(SkillCost cost) => cost.Percent is { } percent ? $"最大值×{Number(percent)}%" : $"{cost.Amount}";

    private static string Effect(SkillEffect effect)
    {
        var text = effect switch
        {
            StatChangeEffect change => $"{(change.Target == SkillTarget.Caster ? "自己" : "對手")}{StatName(change.Stat)} {StatValue(change)}{ChanceText(change.Chance)}",
            BonusDamageEffect bonus => $"追加{AttackKindName(bonus.AttackKind)}傷害 {Scaling(bonus.Scaling)}{ChanceText(bonus.Chance)}",
            PercentDamageEffect damage => $"削減對手目前 HP {Number(damage.Percent)}%",
            DamageOverTimeEffect overTime => $"每回合造成 {Scaling(overTime.Scaling)} 傷害",
            ReduceSkillPointsEffect loss => $"削減對手 SP {(loss.Amount is { } amount ? amount.ToString(CultureInfo.InvariantCulture) : $"最大值×{Number(loss.PercentOfMaximum!.Value)}%")}",
            SilenceEffect silence => $"封鎖對手技能 機率 {silence.Chance}%",
            AttackElementEffect element => $"攻擊屬性轉為{ElementName(element.Element)}",
            DefenseElementEffect element => $"防禦屬性轉為{ElementName(element.Element)}",
            ElementAdvantageEffect advantage => $"剋制時屬性傷害 +{Number(advantage.Percent)}%",
            MultiHitEffect multiHit => $"{AttackKindName(multiHit.AttackKind)}連擊 {multiHit.Hits} 次",
            DamageReductionEffect reduction => $"受到的{AttackKindName(reduction.AttackKind)}傷害 -{Number(reduction.Percent)}%",
            RestoreEffect restore => $"回復 {(restore.Resource == SkillResource.HitPoints ? "HP" : "SP")} {RestoreAmount(restore)}",
            StunEffect stun => $"對手無法行動 機率 {stun.Chance}%",
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "未知的效果"),
        };
        return effect.Rounds > 0 ? $"{text}，{effect.Rounds} 回合" : text;
    }

    private static string RestoreAmount(RestoreEffect restore) =>
        restore.Scaling is { } scaling ? Scaling(scaling)
        : restore.PercentOfMaximum is { } maximum ? $"最大值×{Number(maximum)}%"
        : $"傷害×{Number(restore.PercentOfDamage!.Value)}%";

    private static string ChanceText(int? chance) => chance is { } percent ? $"（機率 {percent}%）" : "";

    private static string ElementName(int element) => element switch
    {
        1 => "地",
        2 => "水",
        3 => "火",
        4 => "木",
        5 => "風",
        6 => "毒",
        _ => "無",
    };

    private static string StatValue(StatChangeEffect change) =>
        change.Percent is { } percent ? $"{Signed(percent)}%"
        : change.Amount is { } amount ? $"{Signed(amount)}{(IsPointStat(change.Stat) ? "%" : "")}"
        : $"+{Scaling(change.Scaling!)}";

    // 命中、迴避、暴擊率、暴擊上限、逃跑率的固定值是百分點
    private static bool IsPointStat(SkillStat stat) => stat is SkillStat.HitRate or SkillStat.Evasion or SkillStat.PhysicalCriticalChance
        or SkillStat.MagicCriticalChance or SkillStat.PhysicalCriticalCap or SkillStat.MagicCriticalCap or SkillStat.EscapeRate;

    private static string Scaling(IReadOnlyList<SkillScaling> scaling) =>
        string.Join(" + ", scaling.Select(term => $"{BasisName(term.Basis)}×{Number(term.Ratio * 100)}%"));

    private static string Signed(double value) => value >= 0 ? $"+{Number(value)}" : $"-{Number(-value)}";

    private static string Number(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    private static string StatName(SkillStat stat) => stat switch
    {
        SkillStat.Attack => "物攻",
        SkillStat.MagicAttack => "魔攻",
        SkillStat.Defense => "物防",
        SkillStat.MagicDefense => "魔防",
        SkillStat.Agility => "速度",
        SkillStat.Charisma => "魅力",
        SkillStat.HitRate => "命中",
        SkillStat.Evasion => "迴避",
        SkillStat.PhysicalCriticalChance => "物攻必殺率",
        SkillStat.MagicCriticalChance => "魔攻必殺率",
        SkillStat.PhysicalCriticalCap => "物攻必殺上限",
        SkillStat.MagicCriticalCap => "魔攻必殺上限",
        SkillStat.EscapeRate => "逃跑率",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "未知的能力"),
    };

    private static string BasisName(SkillScalingBasis basis) => basis switch
    {
        SkillScalingBasis.Strength => "力量",
        SkillScalingBasis.Intelligence => "智力",
        SkillScalingBasis.Vitality => "體質",
        SkillScalingBasis.Agility => "敏捷",
        SkillScalingBasis.Charisma => "魅力",
        SkillScalingBasis.Faith => "信仰",
        SkillScalingBasis.Level => "等級",
        SkillScalingBasis.Attack => "物攻",
        SkillScalingBasis.MagicAttack => "魔攻",
        SkillScalingBasis.Defense => "物防",
        SkillScalingBasis.SkillPoints => "SP",
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "未知的能力基準"),
    };

    private static string AttackKindName(SkillAttackKind kind) => kind switch
    {
        SkillAttackKind.Physical => "物理",
        SkillAttackKind.Magic => "魔法",
        _ => "",
    };
}
