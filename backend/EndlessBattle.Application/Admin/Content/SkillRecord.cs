using System.Text.Json.Serialization;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Admin.Content;

public enum ContentSkillSlotType
{
    Active,
    Passive,
}

public sealed record SkillRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required ContentSkillSlotType SlotType { get; init; }

    public required SkillTiming Timing { get; init; }

    public required string Description { get; init; }

    public required int MaxLevel { get; init; }

    public required SkillPrerequisiteRecord? Prerequisite { get; init; }

    public required IReadOnlyList<string> JobIds { get; init; }

    public required IReadOnlyList<SkillLevelRecord> Levels { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}

public sealed record SkillPrerequisiteRecord
{
    public required string SkillId { get; init; }

    public required int Level { get; init; }
}

public sealed record SkillLevelRecord
{
    public required int Level { get; init; }

    public required long LearningCost { get; init; }

    public required int ProficiencyRequired { get; init; }

    public required int Cooldown { get; init; }

    public required IReadOnlyList<SkillConditionRecord> Conditions { get; init; }

    public required IReadOnlyList<SkillCostRecord> Costs { get; init; }

    public required IReadOnlyList<SkillEffectRecord> Effects { get; init; }
}

// 每個部分以 kind 區分種類，內容檔與 API 都一樣；屬性的宣告順序就是內容檔的欄位順序
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChanceConditionRecord), "Chance")]
[JsonDerivedType(typeof(HitPointsBelowConditionRecord), "HitPointsBelow")]
[JsonDerivedType(typeof(HitPointsBelowOpponentConditionRecord), "HitPointsBelowOpponent")]
[JsonDerivedType(typeof(StatAboveConditionRecord), "StatAbove")]
public abstract record SkillConditionRecord;

public sealed record ChanceConditionRecord : SkillConditionRecord
{
    public required int Percent { get; init; }
}

public sealed record HitPointsBelowConditionRecord : SkillConditionRecord
{
    public required int Percent { get; init; }
}

public sealed record HitPointsBelowOpponentConditionRecord : SkillConditionRecord;

public sealed record StatAboveConditionRecord : SkillConditionRecord
{
    public required SkillScalingBasis Stat { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(HitPointsCostRecord), "HitPoints")]
[JsonDerivedType(typeof(SkillPointsCostRecord), "SkillPoints")]
[JsonDerivedType(typeof(MoneyCostRecord), "Money")]
public abstract record SkillCostRecord;

// amount、percent 擇一，是否恰好一項由內容載入程式檢查
public sealed record HitPointsCostRecord : SkillCostRecord
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Percent { get; init; }
}

// amount、percent、all 三擇一
public sealed record SkillPointsCostRecord : SkillCostRecord
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Percent { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? All { get; init; }
}

public sealed record MoneyCostRecord : SkillCostRecord
{
    public required int Amount { get; init; }
}

public sealed record SkillScalingRecord
{
    public required SkillScalingBasis Basis { get; init; }

    public required double Ratio { get; init; }
}

public sealed record SkillChanceByStatRecord
{
    public required SkillScalingBasis Stat { get; init; }

    public required int Percent { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(StatChangeEffectRecord), "StatChange")]
[JsonDerivedType(typeof(BonusDamageEffectRecord), "BonusDamage")]
[JsonDerivedType(typeof(MultiHitEffectRecord), "MultiHit")]
[JsonDerivedType(typeof(DamageReductionEffectRecord), "DamageReduction")]
[JsonDerivedType(typeof(RestoreEffectRecord), "Restore")]
[JsonDerivedType(typeof(StunEffectRecord), "Stun")]
[JsonDerivedType(typeof(SilenceEffectRecord), "Silence")]
[JsonDerivedType(typeof(PercentDamageEffectRecord), "PercentDamage")]
[JsonDerivedType(typeof(ElementAdvantageEffectRecord), "ElementAdvantage")]
[JsonDerivedType(typeof(DamageOverTimeEffectRecord), "DamageOverTime")]
[JsonDerivedType(typeof(ReduceSkillPointsEffectRecord), "ReduceSkillPoints")]
[JsonDerivedType(typeof(AttackElementEffectRecord), "AttackElement")]
[JsonDerivedType(typeof(DefenseElementEffectRecord), "DefenseElement")]
public abstract record SkillEffectRecord;

// percent、amount、scaling 三擇一
public sealed record StatChangeEffectRecord : SkillEffectRecord
{
    public required SkillTarget Target { get; init; }

    public required SkillStat Stat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Percent { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SkillScalingRecord>? Scaling { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Chance { get; init; }

    public required int Rounds { get; init; }
}

public sealed record BonusDamageEffectRecord : SkillEffectRecord
{
    public required SkillAttackKind AttackKind { get; init; }

    public required IReadOnlyList<SkillScalingRecord> Scaling { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Chance { get; init; }

    public required int Rounds { get; init; }
}

public sealed record MultiHitEffectRecord : SkillEffectRecord
{
    public required SkillAttackKind AttackKind { get; init; }

    public required int Hits { get; init; }

    public required int Rounds { get; init; }
}

public sealed record DamageReductionEffectRecord : SkillEffectRecord
{
    public required SkillAttackKind AttackKind { get; init; }

    public required double Percent { get; init; }

    public required int Rounds { get; init; }
}

// scaling、percentOfDamage、percentOfMaximum 三擇一
public sealed record RestoreEffectRecord : SkillEffectRecord
{
    public required SkillResource Resource { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SkillScalingRecord>? Scaling { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PercentOfDamage { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PercentOfMaximum { get; init; }

    public required int Rounds { get; init; }
}

public sealed record StunEffectRecord : SkillEffectRecord
{
    public required int Chance { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SkillChanceByStatRecord? ChanceByStat { get; init; }

    public required int Rounds { get; init; }
}

public sealed record SilenceEffectRecord : SkillEffectRecord
{
    public required int Chance { get; init; }

    public required int Rounds { get; init; }
}

public sealed record PercentDamageEffectRecord : SkillEffectRecord
{
    public required double Percent { get; init; }

    public required int Rounds { get; init; }
}

public sealed record ElementAdvantageEffectRecord : SkillEffectRecord
{
    public required double Percent { get; init; }

    public required int Rounds { get; init; }
}

public sealed record DamageOverTimeEffectRecord : SkillEffectRecord
{
    public required IReadOnlyList<SkillScalingRecord> Scaling { get; init; }

    public required int Rounds { get; init; }
}

// amount、percentOfMaximum 擇一
public sealed record ReduceSkillPointsEffectRecord : SkillEffectRecord
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PercentOfMaximum { get; init; }

    public required int Rounds { get; init; }
}

public sealed record AttackElementEffectRecord : SkillEffectRecord
{
    public required ContentElement Element { get; init; }

    public required int Rounds { get; init; }
}

public sealed record DefenseElementEffectRecord : SkillEffectRecord
{
    public required ContentElement Element { get; init; }

    public required int Rounds { get; init; }
}
