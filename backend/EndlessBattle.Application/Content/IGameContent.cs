using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
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

    IReadOnlyList<ItemDefinition> AllItems { get; }

    IReadOnlyList<Recipe> AllRecipes { get; }

    int StartingPlaceId { get; }

    int? StartingSubPlace { get; }

    PlaceDefinition GetPlace(int placeId);

    AreaDefinition GetArea(string areaId);

    MonsterDefinition GetMonster(string monsterId);

    JobDefinition GetJob(string jobId);

    SkillDefinition GetSkill(string skillId);

    ItemDefinition GetItem(string itemId);

    Recipe GetRecipe(string recipeId);

    // 該地區與全域怪物，依內容檔中的順序
    IReadOnlyList<MonsterDefinition> MonstersOf(int placeId);

    // 依內容檔中的順序
    IReadOnlyList<SkillDefinition> SkillsOf(string jobId);

    // 商店該分類、該等級頁上販賣的品項，依內容檔中的順序
    IReadOnlyList<ItemDefinition> ItemsForShop(ItemCategory category, int level);
}
