using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

// SkillPoints 是目前職業的技能點 sk_
public sealed record SkillOverview(string JobId, long SkillPoints, IReadOnlyList<SkillView> Skills, IReadOnlyList<SkillSlot> Slots);

// Level 0 表示尚未學習；Summary 是目前等級的效果文字（未學為 null）；Next 在已滿級時為 null
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
    NextLevelView? Next);

public sealed record NextLevelView(int Level, long Money, long Proficiency, string Summary, bool CanLearn, IReadOnlyList<string> Reasons);
