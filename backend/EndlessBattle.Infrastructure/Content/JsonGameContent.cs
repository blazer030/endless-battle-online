using System.Text.Json;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Infrastructure.Content;

// 啟動時一次載入並驗證全部內容；有錯就立刻失敗，不讓壞資料進到遊戲裡
public sealed class JsonGameContent : IGameContent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const int MinimumElement = 0;
    private const int MaximumElement = 6;
    private const int MaximumMeet = 100;

    private static readonly HashSet<string> StatNames = ["str", "smart", "life", "vit", "agi", "au", "be"];

    private readonly Dictionary<string, MonsterDefinition> monsters;
    private readonly Dictionary<string, StageDefinition> stages;
    private readonly Dictionary<string, JobDefinition> jobs;
    private readonly Dictionary<string, SkillDefinition> skills;

    private JsonGameContent(
        string startingStageId,
        IReadOnlyList<MonsterDefinition> allMonsters,
        IReadOnlyList<StageDefinition> allStages,
        IReadOnlyList<JobDefinition> allJobs,
        IReadOnlyList<SkillDefinition> allSkills,
        Dictionary<string, MonsterDefinition> monsters,
        Dictionary<string, StageDefinition> stages,
        Dictionary<string, JobDefinition> jobs,
        Dictionary<string, SkillDefinition> skills)
    {
        StartingStageId = startingStageId;
        AllMonsters = allMonsters;
        AllStages = allStages;
        AllJobs = allJobs;
        AllSkills = allSkills;
        this.monsters = monsters;
        this.stages = stages;
        this.jobs = jobs;
        this.skills = skills;
    }

    public string StartingStageId { get; }

    public IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    public IReadOnlyList<StageDefinition> AllStages { get; }

    public IReadOnlyList<JobDefinition> AllJobs { get; }

    public IReadOnlyList<SkillDefinition> AllSkills { get; }

    public static JsonGameContent Load(string directory)
    {
        var monsterFile = Read<MonsterFile>(Path.Combine(directory, "monsters.json"));
        var stageFile = Read<StageFile>(Path.Combine(directory, "stages.json"));
        var jobFile = Read<JobFile>(Path.Combine(directory, "jobs.json"));
        var skillFile = Read<SkillFile>(Path.Combine(directory, "skills.json"));

        var monsters = new Dictionary<string, MonsterDefinition>();
        foreach (var monster in monsterFile.Monsters)
        {
            if (!monsters.TryAdd(monster.Id, monster))
            {
                throw new GameContentException($"怪物 id 重複：{monster.Id}");
            }

            ValidateMonster(monster);
        }

        var stages = new Dictionary<string, StageDefinition>();
        foreach (var stage in stageFile.Stages)
        {
            if (!stages.TryAdd(stage.Id, stage))
            {
                throw new GameContentException($"關卡 id 重複：{stage.Id}");
            }

            if (stage.MonsterIds.Count == 0)
            {
                throw new GameContentException($"關卡 {stage.Id} 沒有任何怪物");
            }

            var unknownMonster = stage.MonsterIds.FirstOrDefault(monsterId => !monsters.ContainsKey(monsterId));
            if (unknownMonster is not null)
            {
                throw new GameContentException($"關卡 {stage.Id} 引用了不存在的怪物：{unknownMonster}");
            }
        }

        if (!stages.ContainsKey(stageFile.StartingStageId))
        {
            throw new GameContentException($"起始關卡不存在：{stageFile.StartingStageId}");
        }

        var allJobs = jobFile.Jobs.Select(ToJobDefinition).ToList();
        var jobs = new Dictionary<string, JobDefinition>();
        foreach (var job in allJobs)
        {
            if (!jobs.TryAdd(job.Id, job))
            {
                throw new GameContentException($"職業 id 重複：{job.Id}");
            }
        }

        foreach (var job in allJobs)
        {
            ValidateJob(job, jobs);
        }

        var allSkills = skillFile.Skills.Select(ToSkillDefinition).ToList();
        var skills = new Dictionary<string, SkillDefinition>();
        foreach (var skill in allSkills)
        {
            if (!skills.TryAdd(skill.Id, skill))
            {
                throw new GameContentException($"技能 id 重複：{skill.Id}");
            }

            ValidateSkill(skill, jobs);
        }

        return new JsonGameContent(
            stageFile.StartingStageId, monsterFile.Monsters, stageFile.Stages, allJobs, allSkills, monsters, stages, jobs, skills);
    }

    public StageDefinition GetStage(string stageId) =>
        stages.TryGetValue(stageId, out var stage) ? stage : throw new GameContentException($"找不到關卡：{stageId}");

    public MonsterDefinition GetMonster(string monsterId) =>
        monsters.TryGetValue(monsterId, out var monster) ? monster : throw new GameContentException($"找不到怪物：{monsterId}");

    public JobDefinition GetJob(string jobId) =>
        jobs.TryGetValue(jobId, out var job) ? job : throw new GameContentException($"找不到職業：{jobId}");

    public SkillDefinition GetSkill(string skillId) =>
        skills.TryGetValue(skillId, out var skill) ? skill : throw new GameContentException($"找不到技能：{skillId}");

    public IReadOnlyList<SkillDefinition> SkillsOf(string jobId) =>
        AllSkills.Where(skill => skill.JobId == jobId).ToList();

    private static void ValidateMonster(MonsterDefinition monster)
    {
        // agi 是命中公式的分母
        if (monster.Hp <= 0 || monster.Agi <= 0)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 hp 與 agi 必須大於 0");
        }

        if (monster.Level < 1)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 level 必須至少 1");
        }

        if (monster.Element is < MinimumElement or > MaximumElement)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 element 必須在 {MinimumElement}～{MaximumElement}");
        }

        if (monster.Meet is < 0 or > MaximumMeet)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 meet 必須在 0～{MaximumMeet}");
        }
    }

    private static void ValidateJob(JobDefinition job, Dictionary<string, JobDefinition> jobs)
    {
        if (job.Tier == 1 && job.ParentId is not null)
        {
            throw new GameContentException($"一轉職業 {job.Id} 不能有 parentId");
        }

        if (job.Tier == 2 && job.ParentId is null)
        {
            throw new GameContentException($"二轉職業 {job.Id} 必須有 parentId");
        }

        if (job.ParentId is not null && !jobs.ContainsKey(job.ParentId))
        {
            throw new GameContentException($"職業 {job.Id} 的 parentId 不存在：{job.ParentId}");
        }

        var growth = job.Growth;
        StatRange[] ranges = [growth.Str, growth.Smart, growth.Life, growth.Vit, growth.Agi, growth.Au, growth.Be];
        if (ranges.Any(range => range.Min < 0 || range.Min > range.Max))
        {
            throw new GameContentException($"職業 {job.Id} 的成長範圍必須 0 ≤ min ≤ max");
        }

        var unknownStat = job.StatRequirements.FirstOrDefault(requirement => !StatNames.Contains(requirement.Stat));
        if (unknownStat is not null)
        {
            throw new GameContentException($"職業 {job.Id} 的能力需求引用了不存在的能力值：{unknownStat.Stat}");
        }
    }

    private static void ValidateSkill(SkillDefinition skill, Dictionary<string, JobDefinition> jobs)
    {
        if (!jobs.ContainsKey(skill.JobId))
        {
            throw new GameContentException($"技能 {skill.Id} 引用了不存在的職業：{skill.JobId}");
        }

        if (skill.Levels.Count == 0)
        {
            throw new GameContentException($"技能 {skill.Id} 至少要有一個等級");
        }

        for (var index = 0; index < skill.Levels.Count; index++)
        {
            var level = skill.Levels[index];
            if (level.Level != index + 1)
            {
                throw new GameContentException($"技能 {skill.Id} 的等級必須從 1 起連續遞增");
            }

            if (level.Sp < 0 || level.Rounds < 1 || level.Money < 0 || level.Proficiency < 0)
            {
                throw new GameContentException($"技能 {skill.Id} 第 {level.Level} 級的 sp、money、proficiency 不可為負，rounds 至少 1");
            }

            if (skill.Type == SkillType.Passive && level.Chance is < 1 or > 100)
            {
                throw new GameContentException($"被動技能 {skill.Id} 第 {level.Level} 級的 chance 必須在 1～100");
            }

            if (skill.Type == SkillType.Active && level.Chance != 0)
            {
                throw new GameContentException($"主動技能 {skill.Id} 第 {level.Level} 級的 chance 必須為 0");
            }
        }
    }

    private static SkillDefinition ToSkillDefinition(SkillEntry entry)
    {
        T ParseEnum<T>(string value, string field) where T : struct, Enum =>
            Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new GameContentException($"技能 {entry.Id} 的 {field} 無法辨識：{value}");

        return new SkillDefinition(
            entry.Id,
            entry.Name,
            entry.JobId,
            ParseEnum<SkillType>(entry.Type, "type"),
            ParseEnum<SkillTiming>(entry.Timing, "timing"),
            ParseEnum<SkillEffectKind>(entry.Effect, "effect"),
            entry.Description,
            entry.Levels);
    }

    // 成長在內容檔裡寫成 [min, max]
    private static JobDefinition ToJobDefinition(JobEntry entry)
    {
        StatRange ToRange(int[]? range) => range is [var min, var max]
            ? new StatRange(min, max)
            : throw new GameContentException($"職業 {entry.Id} 的成長範圍必須是 [min, max]");

        var growth = entry.Growth;
        return new JobDefinition(
            entry.Id,
            entry.Name,
            entry.Tier,
            entry.ParentId,
            entry.UnlockLevel,
            entry.ProficiencyRequired,
            entry.StatRequirements,
            new JobGrowth(
                ToRange(growth.Str),
                ToRange(growth.Smart),
                ToRange(growth.Life),
                ToRange(growth.Vit),
                ToRange(growth.Agi),
                ToRange(growth.Au),
                ToRange(growth.Be)));
    }

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions)
        ?? throw new GameContentException($"內容檔是空的：{path}");

    private sealed record MonsterFile(IReadOnlyList<MonsterDefinition> Monsters);

    private sealed record StageFile(string StartingStageId, IReadOnlyList<StageDefinition> Stages);

    private sealed record SkillFile(IReadOnlyList<SkillEntry> Skills);

    private sealed record SkillEntry(
        string Id,
        string Name,
        string JobId,
        string Type,
        string Timing,
        string Effect,
        string Description,
        IReadOnlyList<SkillLevel> Levels);

    private sealed record JobFile(IReadOnlyList<JobEntry> Jobs);

    private sealed record JobEntry(
        string Id,
        string Name,
        int Tier,
        string? ParentId,
        int UnlockLevel,
        int ProficiencyRequired,
        IReadOnlyList<StatRequirement> StatRequirements,
        GrowthEntry Growth);

    private sealed record GrowthEntry(int[]? Str, int[]? Smart, int[]? Life, int[]? Vit, int[]? Agi, int[]? Au, int[]? Be);
}
