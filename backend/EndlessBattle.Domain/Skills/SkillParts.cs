namespace EndlessBattle.Domain.Skills;

// 技能每一級由發動條件、消耗、效果組成；數值都寫在內容檔，戰鬥只依種類與數值計算

public enum SkillConditionKind { Chance, HitPointsBelow }

// Chance：Percent 是 1～100 的機率；HitPointsBelow：自己的 HP 低於最大 HP 的 Percent%
public sealed record SkillCondition(SkillConditionKind Kind, int Percent);

public enum SkillCostKind { SkillPoints }

public sealed record SkillCost(SkillCostKind Kind, int Amount);

public enum SkillTarget { Caster, Opponent }

public enum SkillStat { Attack, MagicAttack, Defense, MagicDefense, Agility, HitRate, PhysicalCriticalChance }

public enum SkillScalingBasis { Strength, Intelligence, Agility, Charisma, Attack, MagicAttack, Defense }

public sealed record SkillScaling(SkillScalingBasis Basis, double Ratio);

public enum SkillAttackKind { Physical, Magic, Any }

public enum SkillResource { HitPoints }

// Rounds：0 是只在發動的那一回合生效，大於 0 是持續的回合數（含發動回合）
public abstract record SkillEffect(int Rounds);

// Percent、Amount、Scaling 恰好一項有值；可為負
public sealed record StatChangeEffect(
    SkillTarget Target, SkillStat Stat, double? Percent, int? Amount, IReadOnlyList<SkillScaling>? Scaling, int Rounds) : SkillEffect(Rounds);

public sealed record BonusDamageEffect(SkillAttackKind AttackKind, IReadOnlyList<SkillScaling> Scaling, int Rounds) : SkillEffect(Rounds);

public sealed record MultiHitEffect(SkillAttackKind AttackKind, int Hits, int Rounds) : SkillEffect(Rounds);

public sealed record DamageReductionEffect(SkillAttackKind AttackKind, double Percent, int Rounds) : SkillEffect(Rounds);

// Scaling、PercentOfDamage 恰好一項有值；PercentOfDamage 是這次造成的傷害的比例
public sealed record RestoreEffect(
    SkillResource Resource, IReadOnlyList<SkillScaling>? Scaling, double? PercentOfDamage, int Rounds) : SkillEffect(Rounds);

// 持續期間，對手每次出手前擲 Chance，擲中就無法行動
public sealed record StunEffect(int Chance, int Rounds) : SkillEffect(Rounds);
