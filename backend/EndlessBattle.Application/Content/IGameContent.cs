using EndlessBattle.Domain.Content;

namespace EndlessBattle.Application.Content;

public interface IGameContent
{
    string StartingStageId { get; }

    // 依內容檔中的順序
    IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    IReadOnlyList<StageDefinition> AllStages { get; }

    IReadOnlyList<JobDefinition> AllJobs { get; }

    StageDefinition GetStage(string stageId);

    MonsterDefinition GetMonster(string monsterId);

    JobDefinition GetJob(string jobId);
}
