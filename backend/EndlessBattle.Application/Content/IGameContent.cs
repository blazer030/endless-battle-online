using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.Content;

public interface IGameContent
{
    // 依內容檔中的順序
    IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    IReadOnlyList<JobDefinition> AllJobs { get; }

    IReadOnlyList<SkillDefinition> AllSkills { get; }

    IReadOnlyList<AreaDefinition> AllAreas { get; }

    IReadOnlyList<PlaceDefinition> AllPlaces { get; }

    int StartingPlaceId { get; }

    int? StartingSubPlace { get; }

    PlaceDefinition GetPlace(int placeId);

    AreaDefinition GetArea(string areaId);

    MonsterDefinition GetMonster(string monsterId);

    JobDefinition GetJob(string jobId);

    SkillDefinition GetSkill(string skillId);

    // 依內容檔中的順序
    IReadOnlyList<SkillDefinition> SkillsOf(string jobId);
}
