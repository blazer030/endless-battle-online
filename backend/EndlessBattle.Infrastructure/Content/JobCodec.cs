using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// jobs.json ↔ JobRecord
public sealed class JobCodec : IContentCodec<JobRecord>
{
    private const string JobsFile = "jobs.json";

    public IReadOnlyList<string> FileNames { get; } = [JobsFile];

    public IReadOnlyList<JobRecord> Read(IReadOnlyDictionary<string, string> files) =>
        ContentFileJson.ReadArray<JobEntry>(files[JobsFile], JobsFile, "jobs").Select(ToRecord).ToList();

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<JobRecord> records, IReadOnlyDictionary<string, string> files) =>
        new Dictionary<string, string> { [JobsFile] = ContentFileJson.WriteArray("jobs", records.Select(ToEntry)) };

    private static JobRecord ToRecord(JobEntry entry) => new()
    {
        Id = entry.Id,
        Name = entry.Name,
        Tier = entry.Tier,
        ParentJobId = entry.ParentId,
        UnlockLevel = entry.UnlockLevel,
        ProficiencyRequired = entry.ProficiencyRequired,
        StatRequirements = entry.StatRequirements
            .Select(requirement => new JobStatRequirementRecord { Stat = ContentFileJson.ToStat(requirement.Stat, $"職業 {entry.Id}"), Minimum = requirement.Minimum })
            .ToList(),
        Growth = new JobGrowthRecord
        {
            Strength = ToRange(entry.Growth.Str, entry.Id),
            Intelligence = ToRange(entry.Growth.Smart, entry.Id),
            Life = ToRange(entry.Growth.Life, entry.Id),
            Vitality = ToRange(entry.Growth.Vit, entry.Id),
            Agility = ToRange(entry.Growth.Agi, entry.Id),
            Charisma = ToRange(entry.Growth.Au, entry.Id),
            Faith = ToRange(entry.Growth.Be, entry.Id),
        },
    };

    private static JobEntry ToEntry(JobRecord record) => new()
    {
        Id = record.Id,
        Name = record.Name,
        Tier = record.Tier,
        ParentId = record.ParentJobId,
        UnlockLevel = record.UnlockLevel,
        ProficiencyRequired = record.ProficiencyRequired,
        StatRequirements = record.StatRequirements
            .Select(requirement => new StatRequirementEntry { Stat = ContentFileJson.FromStat(requirement.Stat), Minimum = requirement.Minimum })
            .ToList(),
        Growth = new GrowthEntry
        {
            Str = FromRange(record.Growth.Strength),
            Smart = FromRange(record.Growth.Intelligence),
            Life = FromRange(record.Growth.Life),
            Vit = FromRange(record.Growth.Vitality),
            Agi = FromRange(record.Growth.Agility),
            Au = FromRange(record.Growth.Charisma),
            Be = FromRange(record.Growth.Faith),
        },
    };

    private static ContentRange ToRange(int[] range, string jobId) =>
        ContentFileJson.ToRange(range, $"職業 {jobId} 的成長");

    private static int[] FromRange(ContentRange range) => ContentFileJson.FromRange(range);

    private sealed record JobEntry
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required int Tier { get; init; }

        public required string? ParentId { get; init; }

        public required int UnlockLevel { get; init; }

        public required int ProficiencyRequired { get; init; }

        public required IReadOnlyList<StatRequirementEntry> StatRequirements { get; init; }

        public required GrowthEntry Growth { get; init; }
    }

    private sealed record StatRequirementEntry
    {
        public required string Stat { get; init; }

        public required int Minimum { get; init; }
    }

    private sealed record GrowthEntry
    {
        public required int[] Str { get; init; }

        public required int[] Smart { get; init; }

        public required int[] Life { get; init; }

        public required int[] Vit { get; init; }

        public required int[] Agi { get; init; }

        public required int[] Au { get; init; }

        public required int[] Be { get; init; }
    }
}
