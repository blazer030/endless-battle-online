using System.Text.Json.Serialization;

namespace EndlessBattle.Application.Admin.Content;

public sealed record JobRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required int Tier { get; init; }

    public required string? ParentJobId { get; init; }

    public required int UnlockLevel { get; init; }

    public required int ProficiencyRequired { get; init; }

    public required IReadOnlyList<JobStatRequirementRecord> StatRequirements { get; init; }

    public required JobGrowthRecord Growth { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}

public sealed record JobStatRequirementRecord
{
    public required ContentStat Stat { get; init; }

    public required int Minimum { get; init; }
}

public sealed record JobGrowthRecord
{
    public required ContentRange Strength { get; init; }

    public required ContentRange Intelligence { get; init; }

    public required ContentRange Life { get; init; }

    public required ContentRange Vitality { get; init; }

    public required ContentRange Agility { get; init; }

    public required ContentRange Charisma { get; init; }

    public required ContentRange Faith { get; init; }
}
