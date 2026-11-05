namespace EndlessBattle.Domain.Skills;

// 技能每一級由發動條件、消耗、效果組成；數值都寫在內容檔，戰鬥只依種類與數值計算

public enum SkillConditionKind { Chance, HitPointsBelow }

// Chance：Percent 是 1～100 的機率；HitPointsBelow：自己的 HP 低於最大 HP 的 Percent%
public sealed record SkillCondition(SkillConditionKind Kind, int Percent);

public enum SkillCostKind { SkillPoints }

public sealed record SkillCost(SkillCostKind Kind, int Amount);

public enum SkillTarget { Caster, Opponent }

// HitRate、Evasion、CriticalChance、CriticalCap、EscapeRate 是點數（百分點），只能用 amount
public enum SkillStat
{
    Attack,
    MagicAttack,
    Defense,
    MagicDefense,
    Agility,
    Charisma,
    HitRate,
    Evasion,
    PhysicalCriticalChance,
    MagicCriticalChance,
    PhysicalCriticalCap,
    MagicCriticalCap,
    EscapeRate,
}

public enum SkillScalingBasis { Strength, Intelligence, Vitality, Agility, Charisma, Faith, Level, Attack, MagicAttack, Defense }

public sealed record SkillScaling(SkillScalingBasis Basis, double Ratio);

public enum SkillAttackKind { Physical, Magic, Any }

public enum SkillResource { HitPoints, SkillPoints }

// Rounds：0 是只在發動的那一回合生效，大於 0 是持續的回合數（含發動回合）
public abstract record SkillEffect(int Rounds);

// Percent、Amount、Scaling 恰好一項有值；可為負。Chance：效果自己的機率，技能發動後才擲（只有對手的能力增減可以有）
public sealed record StatChangeEffect(
    SkillTarget Target, SkillStat Stat, double? Percent, int? Amount, IReadOnlyList<SkillScaling>? Scaling, int Rounds) : SkillEffect(Rounds)
{
    public int? Chance { get; init; }
}

// Chance：效果自己的機率，技能發動後才擲
public sealed record BonusDamageEffect(SkillAttackKind AttackKind, IReadOnlyList<SkillScaling> Scaling, int Rounds) : SkillEffect(Rounds)
{
    public int? Chance { get; init; }
}

// 發動當下削減對手目前 HP 的 Percent%
public sealed record PercentDamageEffect(double Percent, int Rounds) : SkillEffect(Rounds);

// 持續期間每回合結束時對對手造成 Scaling 的傷害
public sealed record DamageOverTimeEffect(IReadOnlyList<SkillScaling> Scaling, int Rounds) : SkillEffect(Rounds);

// 發動當下削減對手的 SP：Amount 或最大 SP 的 PercentOfMaximum% 二擇一
public sealed record ReduceSkillPointsEffect(int? Amount, double? PercentOfMaximum, int Rounds) : SkillEffect(Rounds);

public sealed record MultiHitEffect(SkillAttackKind AttackKind, int Hits, int Rounds) : SkillEffect(Rounds);

public sealed record DamageReductionEffect(SkillAttackKind AttackKind, double Percent, int Rounds) : SkillEffect(Rounds);

// Scaling、PercentOfDamage、PercentOfMaximum 恰好一項有值；PercentOfDamage 是這次造成的傷害的比例，PercentOfMaximum 是該資源的最大值
public sealed record RestoreEffect(
    SkillResource Resource, IReadOnlyList<SkillScaling>? Scaling, double? PercentOfDamage, int Rounds) : SkillEffect(Rounds)
{
    public double? PercentOfMaximum { get; init; }
}

// 持續期間，對手每次出手前擲 Chance，擲中就無法行動
public sealed record StunEffect(int Chance, int Rounds) : SkillEffect(Rounds);

// 持續期間對手的技能不發動、不判斷條件；Chance 是發動後擲的機率
public sealed record SilenceEffect(int Chance, int Rounds) : SkillEffect(Rounds);

// Element 1～6：地、水、火、木、風、毒
public sealed record AttackElementEffect(int Element, int Rounds) : SkillEffect(Rounds);

public sealed record DefenseElementEffect(int Element, int Rounds) : SkillEffect(Rounds);

// 屬性剋制（倍率大於 1）時，倍率再提高 Percent%
public sealed record ElementAdvantageEffect(double Percent, int Rounds) : SkillEffect(Rounds);
