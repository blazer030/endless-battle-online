using Inkbound.Domain.Content;

namespace Inkbound.Application.Content;

public interface IGameContent
{
    string StartingStageId { get; }

    StageDefinition GetStage(string stageId);

    MonsterDefinition GetMonster(string monsterId);
}
