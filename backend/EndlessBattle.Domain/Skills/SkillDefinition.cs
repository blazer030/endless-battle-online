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

// 主動技能：Sp 為每次施放的消耗、Rounds 為持續回合、Chance 為每回合固定的發動率（30 + 10·lv）。
// 被動技能常駐生效，不消耗 SP、沒有持續回合與發動率：Sp、Rounds、Chance 一律為 0
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
