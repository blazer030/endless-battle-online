using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

// SkillPoints 是目前職業的技能點 sk_；Jobs 是目前職業與以前當過的職業各一份技能清單（角色頁技能頁用）
public sealed record SkillOverview(
    string JobId, long SkillPoints, IReadOnlyList<SkillView> Skills, IReadOnlyList<SkillSlot> Slots, IReadOnlyList<JobSkills> Jobs);

// 一個職業的技能：目前職業列全部、以前的職業只列已學的；SkillPoints 是這個職業的技能點，各技能都用它判斷
public sealed record JobSkills(string JobId, string JobName, int Tier, bool Current, long SkillPoints, IReadOnlyList<JobSkillView> Skills);

// Rows 是目前等級的數值列（未學為 null）；Next 在滿級時為 null
public sealed record JobSkillView(
    string Id,
    string Name,
    SkillType Type,
    SkillTiming Timing,
    string Description,
    string? Icon,
    int Level,
    int MaxLevel,
    IReadOnlyList<SkillDetailRow>? Rows,
    SkillPrerequisiteCheckView? Prerequisite,
    JobSkillNextLevelView? Next);

public sealed record JobSkillNextLevelView(
    int Level, long Money, long SkillPoints, IReadOnlyList<SkillDetailRow> Rows, bool CanLearn, bool EnoughMoney, bool EnoughSkillPoints);

// Level 0 表示尚未學習；Summary 是目前等級的效果文字（未學為 null）；Prerequisite 在沒有前置技能時為 null；Next 在已滿級時為 null；
// TaughtByCurrentJob、Prerequisite.Met、Next 的 EnoughMoney／EnoughProficiency 是各學習條件有沒有達成
public sealed record SkillView(
    string Id,
    string Name,
    SkillType Type,
    SkillTiming Timing,
    string Description,
    string? Icon,
    int Level,
    int MaxLevel,
    string? Summary,
    bool TaughtByCurrentJob,
    SkillPrerequisiteCheckView? Prerequisite,
    NextLevelView? Next);

public sealed record SkillPrerequisiteView(string SkillId, string Name, int Level);

public sealed record SkillPrerequisiteCheckView(string SkillId, string Name, int Level, bool Met);

public sealed record NextLevelView(int Level, long Money, long Proficiency, string Summary, bool CanLearn, bool EnoughMoney, bool EnoughProficiency);
