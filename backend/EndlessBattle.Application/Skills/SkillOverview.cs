using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

// SkillPoints 是目前職業的技能點 sk_
public sealed record SkillOverview(string JobId, long SkillPoints, IReadOnlyList<SkillView> Skills, IReadOnlyList<SkillSlot> Slots);

// Level 0 表示尚未學習；Chance 是目前等級的發動率（未學或被動為 0）；Next 在已滿級時為 null
public sealed record SkillView(
    string Id,
    string Name,
    SkillType Type,
    SkillEffectKind Effect,
    string Description,
    string JobId,
    int Level,
    int MaxLevel,
    int Chance,
    NextLevelView? Next);

public sealed record NextLevelView(int Level, int Sp, int Rounds, int Chance, long Money, long Proficiency, bool CanLearn, IReadOnlyList<string> Reasons);
