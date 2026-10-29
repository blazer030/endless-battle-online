using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Rest;
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

    // 依內容檔中的順序
    IReadOnlyList<MissionDefinition> AllMissions { get; }

    // 依內容檔中的順序
    IReadOnlyList<MercenaryDefinition> AllMercenaries { get; }

    // 依內容檔中的順序
    IReadOnlyList<HonourExchange> HonourShop { get; }

    InnDefinition Inn { get; }

    // 沒有冠軍紀錄時的 NPC 冠軍，滿 HP／SP
    ChampionSeat SeedChampion { get; }

    int StartingPlaceId { get; }

    int? StartingSubPlace { get; }

    PlaceDefinition GetPlace(int placeId);

    AreaDefinition GetArea(string areaId);

    MonsterDefinition GetMonster(string monsterId);

    JobDefinition GetJob(string jobId);

    SkillDefinition GetSkill(string skillId);

    ItemDefinition GetItem(string itemId);

    // 內容檔沒有這個 id 時回 null（例如玩家持有的道具已從內容移除）
    ItemDefinition? FindItem(string itemId);

    Recipe GetRecipe(string recipeId);

    MissionDefinition GetMission(string missionId);

    MercenaryDefinition GetMercenary(string mercenaryId);

    // 該地區與全域怪物，依內容檔中的順序
    IReadOnlyList<MonsterDefinition> MonstersOf(int placeId);

    // 依內容檔中的順序
    IReadOnlyList<SkillDefinition> SkillsOf(string jobId);

    // 商店該分類、該等級頁上販賣的品項，依內容檔中的順序
    IReadOnlyList<ItemDefinition> ItemsForShop(ItemCategory category, int level);
}
