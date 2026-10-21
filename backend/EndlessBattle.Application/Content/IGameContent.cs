using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Content;

public interface IGameContent
{
    string StartingStageId { get; }

    // 依內容檔中的順序
    IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    IReadOnlyList<StageDefinition> AllStages { get; }

    IReadOnlyList<JobDefinition> AllJobs { get; }

    IReadOnlyList<SkillDefinition> AllSkills { get; }

    StageDefinition GetStage(string stageId);

    MonsterDefinition GetMonster(string monsterId);

    JobDefinition GetJob(string jobId);

    SkillDefinition GetSkill(string skillId);

    // 依內容檔中的順序
    IReadOnlyList<SkillDefinition> SkillsOf(string jobId);
}
