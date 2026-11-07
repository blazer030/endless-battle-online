using System.Text.Json.Serialization;
using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// skills.json 與 job-skills.json ↔ SkillRecord；條件、消耗、效果的形狀在內容檔與紀錄相同，直接共用；關聯表寫回時依技能順序重新產生
public sealed class SkillCodec : IContentCodec<SkillRecord>
{
    private const string SkillsFile = "skills.json";
    private const string JobSkillsFile = "job-skills.json";

    public IReadOnlyList<string> FileNames { get; } = [SkillsFile, JobSkillsFile];

    public IReadOnlyList<SkillRecord> Read(IReadOnlyDictionary<string, string> files)
    {
        var skills = ContentFileJson.ReadArray<SkillEntry>(files[SkillsFile], SkillsFile, "skills");
        var rows = ContentFileJson.ReadArray<JobSkillEntry>(files[JobSkillsFile], JobSkillsFile, "jobSkills");
        var skillIds = skills.Select(skill => skill.Id).ToHashSet();
        var orphan = rows.FirstOrDefault(row => !skillIds.Contains(row.SkillId));
        if (orphan is not null)
        {
            throw new GameContentException($"{JobSkillsFile} 引用了不存在的技能：{orphan.SkillId}");
        }

        var jobsBySkill = rows.ToLookup(row => row.SkillId, row => row.JobId);
        return skills.Select(skill => ToRecord(skill, jobsBySkill[skill.Id])).ToList();
    }

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<SkillRecord> records, IReadOnlyDictionary<string, string> files) =>
        new Dictionary<string, string>
        {
            [SkillsFile] = ContentFileJson.WriteArray("skills", records.Select(ToEntry)),
            [JobSkillsFile] = ContentFileJson.WriteArray(
                "jobSkills",
                records.SelectMany(record => record.JobIds.Select(jobId => new JobSkillEntry { JobId = jobId, SkillId = record.Id }))),
        };

    private static SkillRecord ToRecord(SkillEntry entry, IEnumerable<string> jobIds) => new()
    {
        Id = entry.Id,
        Name = entry.Name,
        SlotType = entry.SlotType,
        Timing = entry.Timing,
        Description = entry.Description,
        MaxLevel = entry.MaxLevel,
        Prerequisite = entry.Prerequisite,
        JobIds = jobIds.ToList(),
        Levels = entry.Levels.Select(level => new SkillLevelRecord
        {
            Level = level.Level,
            LearningCost = level.Money,
            ProficiencyRequired = level.Proficiency,
            Cooldown = level.Cooldown,
            Conditions = level.Conditions,
            Costs = level.Costs,
            Effects = level.Effects,
        }).ToList(),
    };

    private static SkillEntry ToEntry(SkillRecord record) => new()
    {
        Id = record.Id,
        Name = record.Name,
        SlotType = record.SlotType,
        Timing = record.Timing,
        Description = record.Description,
        MaxLevel = record.MaxLevel,
        Prerequisite = record.Prerequisite,
        Levels = record.Levels.Select(level => new SkillLevelEntry
        {
            Level = level.Level,
            Money = level.LearningCost,
            Proficiency = level.ProficiencyRequired,
            Cooldown = level.Cooldown,
            Conditions = level.Conditions,
            Costs = level.Costs,
            Effects = level.Effects,
        }).ToList(),
    };

    private sealed record SkillEntry
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required ContentSkillSlotType SlotType { get; init; }

        public required EndlessBattle.Domain.Skills.SkillTiming Timing { get; init; }

        public required string Description { get; init; }

        public required int MaxLevel { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SkillPrerequisiteRecord? Prerequisite { get; init; }

        public required IReadOnlyList<SkillLevelEntry> Levels { get; init; }
    }

    private sealed record SkillLevelEntry
    {
        public required int Level { get; init; }

        public required long Money { get; init; }

        public required int Proficiency { get; init; }

        public required int Cooldown { get; init; }

        public required IReadOnlyList<SkillConditionRecord> Conditions { get; init; }

        public required IReadOnlyList<SkillCostRecord> Costs { get; init; }

        public required IReadOnlyList<SkillEffectRecord> Effects { get; init; }
    }

    private sealed record JobSkillEntry
    {
        public required string JobId { get; init; }

        public required string SkillId { get; init; }
    }
}
