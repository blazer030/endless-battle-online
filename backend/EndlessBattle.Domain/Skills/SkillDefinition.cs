namespace EndlessBattle.Domain.Skills;

public enum SkillType { Active, Passive }

public enum SkillTiming { BeforeAttack, AfterAttack }

public enum SkillEffectKind
{
    AtMultiplier,
    MatMultiplier,
    DfMultiplier,
    MdfMultiplier,
    AgiMultiplier,
    MultiHit,
    MagicMultiHit,
    EnemyAgiDown,
    EnemyAttackDown,
    Heal,
    WarGodWrath,
    Counter,
    CritChanceUp,
    Bloodthirst,
    BonusDamage,
    Stun,
}

// Chance 只對被動技能有意義；主動技能的發動率由玩家欄位決定，這裡固定為 0
public sealed record SkillLevel(int Level, int Sp, int Rounds, int Chance, long Money, long Proficiency);

public sealed record SkillDefinition(
    string Id,
    string Name,
    string JobId,
    SkillType Type,
    SkillTiming Timing,
    SkillEffectKind Effect,
    string Description,
    IReadOnlyList<SkillLevel> Levels)
{
    public int MaxLevel => Levels.Count;

    public SkillLevel LevelAt(int level) =>
        level >= 1 && level <= MaxLevel
            ? Levels[level - 1]
            : throw new ArgumentOutOfRangeException(nameof(level), level, $"技能 {Id} 的等級必須在 1～{MaxLevel}");
}
