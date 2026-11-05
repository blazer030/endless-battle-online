using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

// SkillPoints 是目前職業的技能點 sk_
public sealed record SkillOverview(string JobId, long SkillPoints, IReadOnlyList<SkillView> Skills, IReadOnlyList<SkillSlot> Slots);

// Level 0 表示尚未學習；Rounds、Chance、Cooldown 是目前等級的值（未學為 0）；Timing 是觸發時機；Trigger 是實際的發動條件（常駐被動為 null）；
// Next 在已滿級時為 null
public sealed record SkillView(
    string Id,
    string Name,
    SkillType Type,
    SkillTiming Timing,
    string Description,
    string JobId,
    int Level,
    int MaxLevel,
    int Rounds,
    int Chance,
    int Cooldown,
    SkillTrigger? Trigger,
    NextLevelView? Next);

public sealed record NextLevelView(int Level, int Sp, int Rounds, int Chance, int Cooldown, long Money, long Proficiency, bool CanLearn, IReadOnlyList<string> Reasons);
