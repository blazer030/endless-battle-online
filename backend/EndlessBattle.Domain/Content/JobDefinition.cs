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

// Stat 是 str、smart、life、vit、agi、au、be 之一
public sealed record StatRequirement(string Stat, int Minimum);

public sealed record JobDefinition(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirement> StatRequirements,
    JobGrowth Growth);
