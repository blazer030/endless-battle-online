namespace EndlessBattle.Domain.Skills;

public enum SkillType { Active, Passive }

public enum SkillTiming { BeforeAttack, AfterAttack }

public enum SkillTriggerKind { Chance, HpBelow }

// 發動條件：Chance 照該級 chance 擲骰；HpBelow 在玩家 HP 比例低於 Percent% 時必定發動、不擲骰
public sealed record SkillTrigger(SkillTriggerKind Kind, int Percent = 0)
{
    public static readonly SkillTrigger Chance = new(SkillTriggerKind.Chance);

    public static SkillTrigger HpBelow(int percent) => new(SkillTriggerKind.HpBelow, percent);
}

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
// 被動技能常駐生效，不消耗 SP、沒有持續回合與發動率：Sp、Rounds、Chance 一律為 0。
// Cooldown 以玩家自己的出手計：發動後接下來 Cooldown 次出手都不能再發動，0 表示沒有冷卻；與 Rounds 互相獨立
// 新格式：Conditions、Costs、Effects 有值，Sp、Rounds、Chance 不使用（為 0）；舊格式三者為 null，數值由 Effect 決定
public sealed record SkillLevel(
    int Level,
    int Sp,
    int Rounds,
    int Chance,
    long Money,
    long Proficiency,
    int Cooldown = 0,
    IReadOnlyList<SkillCondition>? Conditions = null,
    IReadOnlyList<SkillCost>? Costs = null,
    IReadOnlyList<SkillEffect>? Effects = null);

public sealed record SkillDefinition(
    string Id,
    string Name,
    string JobId,
    SkillType Type,
    SkillTiming Timing,
    SkillEffectKind? Effect,
    string Description,
    IReadOnlyList<SkillLevel> Levels,
    SkillTrigger? Trigger = null)
{
    public int MaxLevel => Levels.Count;

    // 由發動條件、消耗、效果組成的新格式；每一級的種類相同（載入時驗證），看第 1 級即可
    public bool IsComposed => Levels is [{ Effects: not null }, ..];

    // 新格式沒有發動條件就常駐；舊格式沒有發動條件的被動常駐生效，其餘（主動、掛條件的被動）都要逐次判斷發動
    public bool IsAlwaysOn => IsComposed ? Levels[0].Conditions is [] : Type == SkillType.Passive && Trigger is null;

    // 實際的發動條件：主動沒寫就是擲發動率；常駐被動沒有條件（null）
    public SkillTrigger? EffectiveTrigger => Trigger ?? (Type == SkillType.Active ? SkillTrigger.Chance : null);

    public SkillLevel LevelAt(int level) =>
        level >= 1 && level <= MaxLevel
            ? Levels[level - 1]
            : throw new ArgumentOutOfRangeException(nameof(level), level, $"技能 {Id} 的等級必須在 1～{MaxLevel}");
}
