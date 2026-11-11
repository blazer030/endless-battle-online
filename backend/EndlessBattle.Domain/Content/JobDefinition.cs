using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Content;

public sealed record StatRange(int Minimum, int Maximum);

public sealed record JobGrowth(
    StatRange Strength,
    StatRange Intelligence,
    StatRange Life,
    StatRange Vitality,
    StatRange Agility,
    StatRange Charisma,
    StatRange Faith);

public sealed record StatRequirement(BaseStat Stat, int Minimum);

public sealed record JobDefinition(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirement> StatRequirements,
    JobGrowth Growth);
