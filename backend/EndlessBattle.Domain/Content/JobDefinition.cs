namespace EndlessBattle.Domain.Content;

public sealed record StatRange(int Min, int Max);

public sealed record JobGrowth(
    StatRange Str,
    StatRange Smart,
    StatRange Life,
    StatRange Vit,
    StatRange Agi,
    StatRange Au,
    StatRange Be);

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
