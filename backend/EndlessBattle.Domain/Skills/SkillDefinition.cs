namespace EndlessBattle.Domain.Skills;

// Type 只決定能裝在哪一種欄位：主動最多 3 個、被動最多 2 個；其餘規則主動被動相同
public enum SkillType { Active, Passive }

public enum SkillTiming { BeforeAttack, AfterAttack }

public enum SkillTriggerKind { Chance, HpBelow }

// 技能頁還在用的發動條件摘要，由每級的 conditions 換算
public sealed record SkillTrigger(SkillTriggerKind Kind, int Percent = 0)
{
    public static readonly SkillTrigger Chance = new(SkillTriggerKind.Chance);

    public static SkillTrigger HpBelow(int percent) => new(SkillTriggerKind.HpBelow, percent);
}

// 每一級的數值各自設定。Cooldown 以玩家自己的出手計：發動後接下來 Cooldown 次出手都不能再發動，0 表示沒有冷卻
public sealed record SkillLevel(
    int Level,
    long Money,
    long Proficiency,
    int Cooldown,
    IReadOnlyList<SkillCondition> Conditions,
    IReadOnlyList<SkillCost> Costs,
    IReadOnlyList<SkillEffect> Effects);

public sealed record SkillDefinition(
    string Id,
    string Name,
    SkillType Type,
    SkillTiming Timing,
    string Description,
    IReadOnlyList<SkillLevel> Levels)
{
    public int MaxLevel => Levels.Count;

    // 前置技能：要先學到指定等級才能學這個技能
    public SkillPrerequisite? Prerequisite { get; init; }

    // 沒有發動條件就常駐；每一級的種類相同（載入時驗證），看第 1 級即可
    public bool IsAlwaysOn => Levels[0].Conditions is [];

    public SkillLevel LevelAt(int level) =>
        level >= 1 && level <= MaxLevel
            ? Levels[level - 1]
            : throw new ArgumentOutOfRangeException(nameof(level), level, $"技能 {Id} 的等級必須在 1～{MaxLevel}");
}

public sealed record SkillPrerequisite(string SkillId, int Level);
