using Inkbound.Domain.Content;

namespace Inkbound.Application.Content;

public interface IGameContent
{
    string StartingStageId { get; }

    // 依內容檔中的順序
    IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    IReadOnlyList<StageDefinition> AllStages { get; }

    StageDefinition GetStage(string stageId);

    MonsterDefinition GetMonster(string monsterId);
}
