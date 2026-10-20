using System.Text.Json;
using Inkbound.Application.Content;
using Inkbound.Domain.Content;

namespace Inkbound.Infrastructure.Content;

// 啟動時一次載入並驗證全部內容；有錯就立刻失敗，不讓壞資料進到遊戲裡
public sealed class JsonGameContent : IGameContent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, MonsterDefinition> monsters;
    private readonly Dictionary<string, StageDefinition> stages;

    private JsonGameContent(
        string startingStageId,
        IReadOnlyList<MonsterDefinition> allMonsters,
        IReadOnlyList<StageDefinition> allStages,
        Dictionary<string, MonsterDefinition> monsters,
        Dictionary<string, StageDefinition> stages)
    {
        StartingStageId = startingStageId;
        AllMonsters = allMonsters;
        AllStages = allStages;
        this.monsters = monsters;
        this.stages = stages;
    }

    public string StartingStageId { get; }

    public IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    public IReadOnlyList<StageDefinition> AllStages { get; }

    public static JsonGameContent Load(string directory)
    {
        var monsterFile = Read<MonsterFile>(Path.Combine(directory, "monsters.json"));
        var stageFile = Read<StageFile>(Path.Combine(directory, "stages.json"));

        var monsters = new Dictionary<string, MonsterDefinition>();
        foreach (var monster in monsterFile.Monsters)
        {
            if (!monsters.TryAdd(monster.Id, monster))
            {
                throw new GameContentException($"妖怪 id 重複：{monster.Id}");
            }

            if (monster.Stats.MaxHealth <= 0 || monster.Stats.AttacksPerSecond <= 0)
            {
                throw new GameContentException($"妖怪 {monster.Id} 的 maxHealth 與 attacksPerSecond 必須大於 0");
            }
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
                throw new GameContentException($"關卡 {stage.Id} 沒有任何妖怪");
            }

            var unknownMonster = stage.MonsterIds.FirstOrDefault(monsterId => !monsters.ContainsKey(monsterId));
            if (unknownMonster is not null)
            {
                throw new GameContentException($"關卡 {stage.Id} 引用了不存在的妖怪：{unknownMonster}");
            }
        }

        if (!stages.ContainsKey(stageFile.StartingStageId))
        {
            throw new GameContentException($"起始關卡不存在：{stageFile.StartingStageId}");
        }

        return new JsonGameContent(stageFile.StartingStageId, monsterFile.Monsters, stageFile.Stages, monsters, stages);
    }

    public StageDefinition GetStage(string stageId) =>
        stages.TryGetValue(stageId, out var stage) ? stage : throw new GameContentException($"找不到關卡：{stageId}");

    public MonsterDefinition GetMonster(string monsterId) =>
        monsters.TryGetValue(monsterId, out var monster) ? monster : throw new GameContentException($"找不到妖怪：{monsterId}");

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions)
        ?? throw new GameContentException($"內容檔是空的：{path}");

    private sealed record MonsterFile(IReadOnlyList<MonsterDefinition> Monsters);

    private sealed record StageFile(string StartingStageId, IReadOnlyList<StageDefinition> Stages);
}
