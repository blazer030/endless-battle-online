using System.Text.Json;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Infrastructure.Content;

// 啟動時一次載入並驗證全部內容；有錯就立刻失敗，不讓壞資料進到遊戲裡
public sealed class JsonGameContent : IGameContent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private const int MinimumElement = 0;
    private const int MaximumElement = 6;
    private const int MaximumMeet = 100;
    private const int MinimumPlaceId = 1;
    private const int MaximumPlaceId = 21;
    private const int SubPlacesPerPlace = 4;
    private const int MinimumShopLevel = 1;
    private const int MaximumShopLevel = 7;
    private const int MaximumHoles = 4;
    private const int MaximumRefineLevel = 10;
    private const int MaximumRefineKind = 18;
    private const int MaximumMissionIdLength = 32;
    private const int MaximumMissionQuantity = 99;
    private const int MaximumBirth = 4;

    private static readonly HashSet<string> StatNames = ["str", "smart", "life", "vit", "agi", "au", "be"];

    private readonly Dictionary<string, MonsterDefinition> monsters;
    private readonly Dictionary<string, JobDefinition> jobs;
    private readonly Dictionary<string, SkillDefinition> skills;
    private readonly Dictionary<int, PlaceDefinition> places;
    private readonly Dictionary<string, ItemDefinition> items;
    private readonly Dictionary<string, Recipe> recipes;
    private readonly Dictionary<string, MissionDefinition> missions;

    private JsonGameContent(
        IReadOnlyList<MonsterDefinition> allMonsters,
        IReadOnlyList<JobDefinition> allJobs,
        IReadOnlyList<SkillDefinition> allSkills,
        Dictionary<string, MonsterDefinition> monsters,
        Dictionary<string, JobDefinition> jobs,
        Dictionary<string, SkillDefinition> skills,
        WorldFile world,
        IReadOnlyList<AreaDefinition> allAreas,
        IReadOnlyList<PlaceDefinition> allPlaces,
        Dictionary<int, PlaceDefinition> places,
        IReadOnlyList<ItemDefinition> allItems,
        Dictionary<string, ItemDefinition> items,
        IReadOnlyList<Recipe> allRecipes,
        IReadOnlyList<MissionDefinition> allMissions)
    {
        AllItems = allItems;
        this.items = items;
        AllRecipes = allRecipes;
        recipes = allRecipes.ToDictionary(recipe => recipe.Id);
        AllMissions = allMissions;
        missions = allMissions.ToDictionary(mission => mission.Id);
        StartingPlaceId = world.StartingPlaceId;
        StartingSubPlace = world.StartingSubPlace;
        AllAreas = allAreas;
        AllPlaces = allPlaces;
        this.places = places;
        AllMonsters = allMonsters;
        AllJobs = allJobs;
        AllSkills = allSkills;
        this.monsters = monsters;
        this.jobs = jobs;
        this.skills = skills;
    }

    public int StartingPlaceId { get; }

    public int? StartingSubPlace { get; }

    public IReadOnlyList<AreaDefinition> AllAreas { get; }

    public IReadOnlyList<PlaceDefinition> AllPlaces { get; }

    public IReadOnlyList<MonsterDefinition> AllMonsters { get; }

    public IReadOnlyList<JobDefinition> AllJobs { get; }

    public IReadOnlyList<SkillDefinition> AllSkills { get; }

    public IReadOnlyList<ItemDefinition> AllItems { get; }

    public IReadOnlyList<Recipe> AllRecipes { get; }

    public IReadOnlyList<MissionDefinition> AllMissions { get; }

    public static JsonGameContent Load(string directory)
    {
        var monsterFile = Read<MonsterFile>(Path.Combine(directory, "monsters.json"));
        var jobFile = Read<JobFile>(Path.Combine(directory, "jobs.json"));
        var skillFile = Read<SkillFile>(Path.Combine(directory, "skills.json"));
        var worldFile = Read<WorldFile>(Path.Combine(directory, "world.json"));
        var itemFile = Read<ItemFile>(Path.Combine(directory, "items.json"));
        var recipeFile = Read<RecipeFile>(Path.Combine(directory, "recipes.json"));
        var missionFile = Read<MissionFile>(Path.Combine(directory, "missions.json"));

        var (allAreas, allPlaces, places) = BuildWorld(worldFile);

        var monsters = new Dictionary<string, MonsterDefinition>();
        foreach (var monster in monsterFile.Monsters)
        {
            if (!monsters.TryAdd(monster.Id, monster))
            {
                throw new GameContentException($"怪物 id 重複：{monster.Id}");
            }

            ValidateMonster(monster, places);
        }

        // 地區沒有怪物就無法遇敵，載入時就擋下；任務怪平常遇不到，不算數
        var emptyPlace = allPlaces.FirstOrDefault(place =>
            !monsterFile.Monsters.Any(monster =>
                monster.MissionId is null && (monster.PlaceId == place.Id || monster.PlaceId == MonsterPool.GlobalPlaceId)));
        if (emptyPlace is not null)
        {
            throw new GameContentException($"地區 {emptyPlace.Id} 沒有任何怪物（含全域怪）");
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

        var allItems = itemFile.Items.Select(ToItemDefinition).ToList();
        var items = new Dictionary<string, ItemDefinition>();
        foreach (var item in allItems)
        {
            if (!items.TryAdd(item.Id, item))
            {
                throw new GameContentException($"道具 id 重複：{item.Id}");
            }

            ValidateItem(item, jobs);
        }

        foreach (var monster in monsterFile.Monsters)
        {
            ValidateDrops(monster, items);
        }

        var allRecipes = recipeFile.Recipes.Select(ToRecipe).ToList();
        var recipeIds = new HashSet<string>();
        foreach (var recipe in allRecipes)
        {
            if (!recipeIds.Add(recipe.Id))
            {
                throw new GameContentException($"配方 id 重複：{recipe.Id}");
            }

            ValidateRecipe(recipe, items);
        }

        var allMissions = missionFile.Missions.Select(ToMissionDefinition).ToList();
        var missions = new Dictionary<string, MissionDefinition>();
        foreach (var mission in allMissions)
        {
            if (string.IsNullOrWhiteSpace(mission.Id))
            {
                throw new GameContentException("任務 id 不可為空");
            }

            if (mission.Id.Length > MaximumMissionIdLength)
            {
                throw new GameContentException($"任務 id 長度不可超過 {MaximumMissionIdLength}：{mission.Id}");
            }

            if (!missions.TryAdd(mission.Id, mission))
            {
                throw new GameContentException($"任務 id 重複：{mission.Id}");
            }
        }

        foreach (var mission in allMissions)
        {
            ValidateMission(mission, missions, monsters, items, skills, jobs, places);
        }

        var cyclic = allMissions.FirstOrDefault(mission => HasPrerequisiteCycle(mission, missions));
        if (cyclic is not null)
        {
            throw new GameContentException($"任務 {cyclic.Id} 的前置鏈有循環");
        }

        foreach (var monster in monsterFile.Monsters)
        {
            ValidateMissionMonster(monster, missions);
        }

        return new JsonGameContent(
            monsterFile.Monsters, allJobs, allSkills, monsters, jobs, skills,
            worldFile, allAreas, allPlaces, places, allItems, items, allRecipes, allMissions);
    }

    public PlaceDefinition GetPlace(int placeId) =>
        places.TryGetValue(placeId, out var place) ? place : throw new GameContentException($"找不到地區：{placeId}");

    // 區域只有個位數，線性查找即可
    public AreaDefinition GetArea(string areaId) =>
        AllAreas.FirstOrDefault(area => area.Id == areaId) ?? throw new GameContentException($"找不到區域：{areaId}");

    public MonsterDefinition GetMonster(string monsterId) =>
        monsters.TryGetValue(monsterId, out var monster) ? monster : throw new GameContentException($"找不到怪物：{monsterId}");

    public JobDefinition GetJob(string jobId) =>
        jobs.TryGetValue(jobId, out var job) ? job : throw new GameContentException($"找不到職業：{jobId}");

    public SkillDefinition GetSkill(string skillId) =>
        skills.TryGetValue(skillId, out var skill) ? skill : throw new GameContentException($"找不到技能：{skillId}");

    public ItemDefinition GetItem(string itemId) =>
        items.TryGetValue(itemId, out var item) ? item : throw new GameContentException($"找不到道具：{itemId}");

    public Recipe GetRecipe(string recipeId) =>
        recipes.TryGetValue(recipeId, out var recipe) ? recipe : throw new GameContentException($"找不到配方：{recipeId}");

    public MissionDefinition GetMission(string missionId) =>
        missions.TryGetValue(missionId, out var mission) ? mission : throw new GameContentException($"找不到任務：{missionId}");

    public IReadOnlyList<ItemDefinition> ItemsForShop(ItemCategory category, int level) =>
        AllItems.Where(item => item.ShopSellable && item.Category == category && item.ShopLevel == level).ToList();

    public IReadOnlyList<MonsterDefinition> MonstersOf(int placeId) =>
        AllMonsters.Where(monster => monster.PlaceId == placeId || monster.PlaceId == MonsterPool.GlobalPlaceId).ToList();

    public IReadOnlyList<SkillDefinition> SkillsOf(string jobId) =>
        AllSkills.Where(skill => skill.JobId == jobId).ToList();

    private static (IReadOnlyList<AreaDefinition>, IReadOnlyList<PlaceDefinition>, Dictionary<int, PlaceDefinition>) BuildWorld(
        WorldFile world)
    {
        var entries = new Dictionary<int, PlaceEntry>();
        foreach (var entry in world.Places)
        {
            if (entry.Id is < MinimumPlaceId or > MaximumPlaceId)
            {
                throw new GameContentException($"地區 id 必須在 {MinimumPlaceId}～{MaximumPlaceId}：{entry.Id}");
            }

            if (!entries.TryAdd(entry.Id, entry))
            {
                throw new GameContentException($"地區 id 重複：{entry.Id}");
            }

            if (entry.SubPlaces.Count is not (0 or SubPlacesPerPlace))
            {
                throw new GameContentException($"地區 {entry.Id} 的子地區必須是 0 或 {SubPlacesPerPlace} 個");
            }

            if (entry.Recommended is not [var min, var max] || min < 1 || min > max)
            {
                throw new GameContentException($"地區 {entry.Id} 的推薦等級必須是 [min, max]，且 1 ≤ min ≤ max");
            }
        }

        var areaOfPlace = new Dictionary<int, string>();
        foreach (var area in world.Areas)
        {
            foreach (var placeId in area.PlaceIds)
            {
                if (!entries.ContainsKey(placeId))
                {
                    throw new GameContentException($"區域 {area.Id} 引用了不存在的地區：{placeId}");
                }

                if (!areaOfPlace.TryAdd(placeId, area.Id))
                {
                    throw new GameContentException($"地區 {placeId} 同時屬於多個區域");
                }
            }
        }

        var orphan = entries.Keys.FirstOrDefault(placeId => !areaOfPlace.ContainsKey(placeId));
        if (orphan != 0)
        {
            throw new GameContentException($"地區 {orphan} 不屬於任何區域");
        }

        if (!entries.TryGetValue(world.StartingPlaceId, out var starting))
        {
            throw new GameContentException($"起始地區不存在：{world.StartingPlaceId}");
        }

        if (starting.SubPlaces.Count > 0 != world.StartingSubPlace.HasValue
            || world.StartingSubPlace is < 1 or > SubPlacesPerPlace)
        {
            throw new GameContentException($"起始地區 {starting.Id} 的子地區設定不符：有子地區才需要，且為 1～{SubPlacesPerPlace}");
        }

        var allPlaces = world.Places
            .Select(entry => new PlaceDefinition(
                entry.Id,
                entry.Name,
                areaOfPlace[entry.Id],
                entry.SubPlaces.Select((name, index) => new SubPlaceDefinition(index + 1, name)).ToList(),
                new LevelRange(entry.Recommended[0], entry.Recommended[1]),
                entry.Restricted))
            .ToList();
        var allAreas = world.Areas.Select(area => new AreaDefinition(area.Id, area.Name, area.PlaceIds)).ToList();
        return (allAreas, allPlaces, allPlaces.ToDictionary(place => place.Id));
    }

    private static void ValidateDrops(MonsterDefinition monster, Dictionary<string, ItemDefinition> items)
    {
        foreach (var drop in monster.Drops)
        {
            if (!items.ContainsKey(drop.ItemId))
            {
                throw new GameContentException($"怪物 {monster.Id} 的掉落引用了不存在的道具：{drop.ItemId}");
            }

            if (drop.Topr < 1)
            {
                throw new GameContentException($"怪物 {monster.Id} 的掉落 {drop.ItemId} 的 topr 必須至少 1");
            }
        }
    }

    private static void ValidateMonster(MonsterDefinition monster, Dictionary<int, PlaceDefinition> places)
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

        if (monster.SubPlaces.Any(subPlace => subPlace is < 1 or > SubPlacesPerPlace)
            || monster.SubPlaces.Distinct().Count() != monster.SubPlaces.Count)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 subPlaces 必須是 1～{SubPlacesPerPlace} 且不重複");
        }

        if (monster.PlaceId == MonsterPool.GlobalPlaceId)
        {
            if (monster.SubPlaces.Count > 0)
            {
                throw new GameContentException($"全域怪物 {monster.Id} 不能指定 subPlaces");
            }

            return;
        }

        if (!places.TryGetValue(monster.PlaceId, out var place))
        {
            throw new GameContentException($"怪物 {monster.Id} 引用了不存在的地區：{monster.PlaceId}");
        }

        if (place.HasSubPlaces != monster.SubPlaces.Count > 0)
        {
            throw new GameContentException(
                $"怪物 {monster.Id} 的 subPlaces 與地區 {place.Id} 不符：有子地區的地區至少指定一個，沒有的必須為空");
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

    private static void ValidateItem(ItemDefinition item, Dictionary<string, JobDefinition> jobs)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new GameContentException(message);
            }
        }

        var id = item.Id;
        Require(item.ShopLevel is >= MinimumShopLevel and <= MaximumShopLevel,
            $"道具 {id} 的 shopLevel 必須在 {MinimumShopLevel}～{MaximumShopLevel}");
        Require(item.Holes is >= 0 and <= MaximumHoles, $"道具 {id} 的 holes 必須在 0～{MaximumHoles}");
        Require(item.RefineMax >= 0, $"道具 {id} 的 refineMax 不可為負");
        Require(item.Price >= 0, $"道具 {id} 的 price 不可為負");
        Require(item.Element is >= MinimumElement and <= MaximumElement,
            $"道具 {id} 的 element 必須在 {MinimumElement}～{MaximumElement}");
        Require(item.JobId is null || jobs.ContainsKey(item.JobId), $"道具 {id} 引用了不存在的職業：{item.JobId}");

        if (item.IsEquipment)
        {
            Require(item.Consumable == ConsumableKind.None && item.SupplyHp == 0 && item.SupplySp == 0 && item.Amount == 0
                    && item.Refine is null && item.Stone is null,
                $"裝備 {id} 不能有 consumable、supplyHp、supplySp、amount、refine、stone");
            return;
        }

        Require(item.Requires == StatRequirement7.None && item.JobId is null, $"非裝備道具 {id} 不能有能力需求（requires）或 jobId");
        Require(item.Holes == 0, $"非裝備道具 {id} 的 holes 必須為 0");
        Require(item.RefineMax == 0, $"非裝備道具 {id} 的 refineMax 必須為 0");

        if (item.Category == ItemCategory.Consumable)
        {
            Require(item.Consumable != ConsumableKind.None, $"消耗品 {id} 必須指定 consumable");
        }
        else
        {
            Require(item.Consumable == ConsumableKind.None, $"道具 {id} 不是消耗品，consumable 必須為 None");
        }

        var usesAmount = item.Consumable is ConsumableKind.StaminaPotion or ConsumableKind.BagExpansion or ConsumableKind.DepotExpansion;
        // 先檢查該種消耗品自己需要的欄位，再檢查用不到的欄位為 0
        if (item.Consumable == ConsumableKind.Supply)
        {
            Require(item.SupplyHp >= 0 && item.SupplySp >= 0 && item.SupplyHp + item.SupplySp > 0,
                $"補給品 {id} 的 supplyHp、supplySp 不可為負，且至少一項大於 0");
        }
        else if (item.Consumable == ConsumableKind.StatPotion)
        {
            Require(Values(item.Bonus).All(value => value >= 0) && item.Bonus != StatBonus.Zero,
                $"能力藥水 {id} 的 bonus 不可為負，且至少一項大於 0");
        }
        else if (usesAmount)
        {
            Require(item.Amount >= 1, $"道具 {id} 的 amount 必須至少 1");
        }

        Require(item.Consumable == ConsumableKind.Supply || (item.SupplyHp == 0 && item.SupplySp == 0),
            $"道具 {id} 不是補給品，supplyHp 與 supplySp 必須為 0");
        Require(usesAmount || item.Amount == 0, $"道具 {id} 的 amount 只用於行動力藥水與擴充");
        Require(item.Consumable == ConsumableKind.StatPotion || item.Bonus == StatBonus.Zero,
            $"道具 {id} 不能有 bonus（只有能力藥水可以）");

        if (item.Category == ItemCategory.RefineStone)
        {
            Require(item.Refine is not null, $"精鍊石 {id} 必須有 refine");
            ValidateRefineStone(id, item.Refine!);
        }
        else
        {
            Require(item.Refine is null, $"道具 {id} 不是精鍊石，不能有 refine");
        }

        if (item.Category == ItemCategory.Stone)
        {
            Require(item.Stone is not null, $"魔石 {id} 必須有 stone");
            ValidateStone(id, item.Stone!);
        }
        else
        {
            Require(item.Stone is null, $"道具 {id} 不是魔石，不能有 stone");
        }
    }

    private static void ValidateRefineStone(string id, RefineStoneSpec refine)
    {
        if (refine.Level is < 1 or > MaximumRefineLevel)
        {
            throw new GameContentException($"精鍊石 {id} 的 level 必須在 1～{MaximumRefineLevel}");
        }

        if (refine.Kind is < 1 or > MaximumRefineKind)
        {
            throw new GameContentException($"精鍊石 {id} 的 kind 必須在 1～{MaximumRefineKind}");
        }

        StatRange?[] ranges =
        [
            refine.At, refine.Mat, refine.Df, refine.Mdf, refine.Str, refine.Smart, refine.Life, refine.Vit,
            refine.Agi, refine.Au, refine.Be,
        ];
        if (ranges.All(range => range is null))
        {
            throw new GameContentException($"精鍊石 {id} 至少要有一項能力範圍");
        }

        if (ranges.Any(range => range is not null && (range.Min < 0 || range.Min > range.Max)))
        {
            throw new GameContentException($"精鍊石 {id} 的能力範圍必須 0 ≤ min ≤ max");
        }
    }

    private static void ValidateStone(string id, StoneSpec stone)
    {
        if (!stone.Random)
        {
            if (stone.Fixed == StatBonus.Zero)
            {
                throw new GameContentException($"固定魔石 {id} 的 fixed 至少一項不為 0");
            }

            if (stone.Min is not null || stone.Max is not null)
            {
                throw new GameContentException($"固定魔石 {id} 不能有 min、max");
            }

            return;
        }

        if (stone.Fixed != StatBonus.Zero)
        {
            throw new GameContentException($"隨機魔石 {id} 不能有 fixed");
        }

        if (stone.Min is null || stone.Max is null)
        {
            throw new GameContentException($"隨機魔石 {id} 必須有 min 與 max");
        }

        if (Values(stone.Min).Zip(Values(stone.Max)).Any(pair => pair.First < 0 || pair.First > pair.Second))
        {
            throw new GameContentException($"隨機魔石 {id} 的每項能力必須 0 ≤ min ≤ max");
        }
    }

    private static int[] Values(StatBonus bonus) =>
    [
        bonus.Str, bonus.Smart, bonus.Life, bonus.Vit, bonus.Agi, bonus.Au, bonus.Be,
        bonus.At, bonus.Mat, bonus.Df, bonus.Mdf, bonus.HpMax,
    ];

    private static void ValidateRecipe(Recipe recipe, Dictionary<string, ItemDefinition> items)
    {
        if (!items.ContainsKey(recipe.ResultItemId))
        {
            throw new GameContentException($"配方 {recipe.Id} 的成品不存在：{recipe.ResultItemId}");
        }

        if (recipe.ResultQuantity < 1)
        {
            throw new GameContentException($"配方 {recipe.Id} 的 resultQuantity 必須至少 1");
        }

        if (recipe.SuccessRate is < 1 or > 100)
        {
            throw new GameContentException($"配方 {recipe.Id} 的 successRate 必須在 1～100");
        }

        if (recipe.Materials.Count == 0)
        {
            throw new GameContentException($"配方 {recipe.Id} 的 materials 至少要有一項");
        }

        foreach (var (itemId, quantity) in recipe.Materials)
        {
            if (!items.ContainsKey(itemId))
            {
                throw new GameContentException($"配方 {recipe.Id} 的材料不存在：{itemId}");
            }

            if (quantity < 1)
            {
                throw new GameContentException($"配方 {recipe.Id} 的材料 {itemId} 的 quantity 必須至少 1");
            }
        }
    }

    private static void ValidateMission(
        MissionDefinition mission,
        Dictionary<string, MissionDefinition> missions,
        Dictionary<string, MonsterDefinition> monsters,
        Dictionary<string, ItemDefinition> items,
        Dictionary<string, SkillDefinition> skills,
        Dictionary<string, JobDefinition> jobs,
        Dictionary<int, PlaceDefinition> places)
    {
        var id = mission.Id;

        void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new GameContentException($"任務 {id} {message}");
            }
        }

        Require(!string.IsNullOrWhiteSpace(mission.Giver), "的 giver 不可為空");
        Require(!string.IsNullOrWhiteSpace(mission.Title), "的 title 不可為空");
        Require(mission.Level >= 1, "的 level 必須至少 1");

        var requirements = mission.Requirements;
        Require(requirements.JobId is null || jobs.ContainsKey(requirements.JobId), $"引用了不存在的職業：{requirements.JobId}");
        Require(requirements.Sex is null or 1 or 2, "的 sex 必須是 1 或 2");
        Require(requirements.Birth is null || requirements.Birth is >= 1 and <= MaximumBirth, $"的 birth 必須在 1～{MaximumBirth}");
        (string Field, string? TargetId)[] references =
            [("prerequisiteId", requirements.PrerequisiteId), ("conflictsWith", requirements.ConflictsWith)];
        foreach (var (field, targetId) in references)
        {
            if (targetId is null)
            {
                continue;
            }

            Require(targetId != id, $"的 {field} 不能是自己");
            Require(missions.TryGetValue(targetId, out var target), $"的 {field} 引用了不存在的任務：{targetId}");
            Require(!target!.Daily, $"的 {field} 不能是每日任務：{targetId}");
        }

        MonsterDefinition RequireKill(KillTarget kill)
        {
            Require(monsters.TryGetValue(kill.MonsterId, out var monster), $"引用了不存在的怪物：{kill.MonsterId}");
            Require(kill.Count >= 1, $"的擊殺目標 {kill.MonsterId} 的 count 必須至少 1");
            return monster!;
        }

        void RequireQuantities(string field, IReadOnlyList<ItemQuantity> entries)
        {
            foreach (var (itemId, quantity) in entries)
            {
                Require(items.ContainsKey(itemId), $"的 {field} 引用了不存在的道具：{itemId}");
                Require(quantity is >= 1 and <= MaximumMissionQuantity,
                    $"的 {field} {itemId} 的 quantity 必須在 1～{MaximumMissionQuantity}");
            }

            Require(entries.Select(entry => entry.ItemId).Distinct().Count() == entries.Count, $"的 {field} 道具重複");
        }

        void RequireReward(string field, MissionReward reward)
        {
            Require(reward.Money >= 0, $"的 {field} money 不可為負");
            RequireQuantities($"{field} items", reward.Items);
            if (reward.Skill is { } skillReward)
            {
                Require(skills.TryGetValue(skillReward.SkillId, out var skill), $"的 {field} 引用了不存在的技能：{skillReward.SkillId}");
                Require(skillReward.Level == 1, $"的技能獎勵 {skillReward.SkillId} 的 level 必須為 1");
                Require(requirements.JobId == skill!.JobId,
                    $"的技能獎勵 {skillReward.SkillId} 屬於職業 {skill.JobId}，requirements.jobId 必須相同");
            }

            Require(reward.Pet is null, "有寵物獎勵；寵物獎勵要到計畫 9 才開放");
        }

        if (mission.Daily)
        {
            Require(mission.PlaceId is null, "是每日任務，placeId 必須為 null");
            Require(mission.Kill is null && mission.Deliver.Count == 0, "是每日任務，不能有 kill 或 deliver");
            Require(requirements.PrerequisiteId is null && requirements.ConflictsWith is null,
                "是每日任務，不能有 prerequisiteId 或 conflictsWith");
            Require(mission.Rewards is { Money: 0, Items.Count: 0, Skill: null, Pet: null },
                "是每日任務，rewards 必須為空（改用 dailyRewards）");
            Require(mission.DailyKills.Count >= 1, "是每日任務，dailyKills 至少要有一項");
            Require(mission.DailyRewards.Count >= 1, "是每日任務，dailyRewards 至少要有一項");
            foreach (var kill in mission.DailyKills)
            {
                var monster = RequireKill(kill);
                Require(monster.MissionId is null, $"的 dailyKills 不能用任務怪：{kill.MonsterId}");
            }

            foreach (var reward in mission.DailyRewards)
            {
                RequireReward("dailyRewards", reward);
            }

            return;
        }

        Require(mission.PlaceId is { } placeId && places.ContainsKey(placeId),
            $"的 placeId 必須是存在的地區：{mission.PlaceId?.ToString() ?? "null"}");
        var place = places[mission.PlaceId!.Value];
        Require(!place.Restricted, $"的地區 {place.Id} 是禁區");
        Require(mission.Kill is not null || mission.Deliver.Count > 0, "至少要有擊殺目標（kill）或交付道具（deliver）");
        Require(mission.DailyKills.Count == 0, "不是每日任務，不能有 dailyKills");
        Require(mission.DailyRewards.Count == 0, "不是每日任務，不能有 dailyRewards");
        if (mission.Kill is { } fixedKill)
        {
            var monster = RequireKill(fixedKill);
            Require(monster.PlaceId == place.Id, $"的擊殺目標 {fixedKill.MonsterId} 不在地區 {place.Id}");
            Require(monster.MissionId is null || monster.MissionId == id,
                $"的擊殺目標 {fixedKill.MonsterId} 是任務 {monster.MissionId} 的任務怪");
        }

        RequireQuantities("deliver", mission.Deliver);
        RequireReward("rewards", mission.Rewards);
    }

    // 每個任務至多一個前置，沿著前置走回到走過的任務就是循環
    private static bool HasPrerequisiteCycle(MissionDefinition mission, Dictionary<string, MissionDefinition> missions)
    {
        var visited = new HashSet<string> { mission.Id };
        var current = mission;
        while (current.Requirements.PrerequisiteId is { } prerequisiteId)
        {
            if (!visited.Add(prerequisiteId))
            {
                return true;
            }

            current = missions[prerequisiteId];
        }

        return false;
    }

    private static void ValidateMissionMonster(MonsterDefinition monster, Dictionary<string, MissionDefinition> missions)
    {
        if (monster.MissionId is not { } missionId)
        {
            return;
        }

        if (monster.PlaceId == MonsterPool.GlobalPlaceId)
        {
            throw new GameContentException($"怪物 {monster.Id} 是全域怪，不能是任務怪（missionId {missionId}）");
        }

        if (!missions.TryGetValue(missionId, out var mission) || mission.Daily || mission.Kill?.MonsterId != monster.Id)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 missionId {missionId} 必須指向以牠為擊殺目標的一般任務");
        }
    }

    private static MissionDefinition ToMissionDefinition(MissionEntry entry)
    {
        static MissionReward ToReward(RewardEntry? reward) =>
            reward is null ? new MissionReward(0, []) : new MissionReward(reward.Money, reward.Items ?? [], reward.Skill, reward.Pet);

        return new MissionDefinition(
            entry.Id ?? "",
            entry.Giver ?? "",
            entry.Title ?? "",
            entry.Description ?? "",
            entry.CompletionMessage ?? "",
            entry.Level,
            entry.PlaceId,
            entry.Daily,
            entry.Kill,
            entry.Deliver ?? [],
            entry.Requirements ?? new MissionRequirements(),
            ToReward(entry.Rewards),
            entry.DailyKills ?? [],
            (entry.DailyRewards ?? []).Select(ToReward).ToList());
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

    private static ItemDefinition ToItemDefinition(ItemEntry entry)
    {
        T ParseEnum<T>(string value, string field) where T : struct, Enum =>
            Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new GameContentException($"道具 {entry.Id} 的 {field} 無法辨識：{value}");

        StatRange? ToRange(int[]? range) => range switch
        {
            null => null,
            [var min, var max] => new StatRange(min, max),
            _ => throw new GameContentException($"精鍊石 {entry.Id} 的能力範圍必須是 [min, max]"),
        };

        var refine = entry.Refine is { } spec
            ? new RefineStoneSpec(
                spec.Level, spec.Kind,
                ToRange(spec.At), ToRange(spec.Mat), ToRange(spec.Df), ToRange(spec.Mdf),
                ToRange(spec.Str), ToRange(spec.Smart), ToRange(spec.Life), ToRange(spec.Vit),
                ToRange(spec.Agi), ToRange(spec.Au), ToRange(spec.Be))
            : null;
        var stone = entry.Stone is { } stoneEntry
            ? new StoneSpec(stoneEntry.Random, stoneEntry.Fixed ?? StatBonus.Zero, stoneEntry.Min, stoneEntry.Max)
            : null;

        return new ItemDefinition(
            entry.Id,
            entry.Name,
            ParseEnum<ItemCategory>(entry.Category, "category"),
            ParseEnum<ConsumableKind>(entry.Consumable ?? nameof(ConsumableKind.None), "consumable"),
            entry.Bonus ?? StatBonus.Zero,
            entry.SupplyHp,
            entry.SupplySp,
            entry.Amount,
            entry.Requires ?? StatRequirement7.None,
            entry.JobId,
            entry.Element,
            entry.Holes,
            entry.RefineMax,
            entry.Price,
            entry.ShopLevel,
            entry.ShopSellable,
            entry.Bound,
            refine,
            stone);
    }

    private static Recipe ToRecipe(RecipeEntry entry) =>
        new(
            entry.Id,
            entry.ResultItemId,
            entry.ResultQuantity,
            entry.Materials.Select(material => (material.ItemId, material.Quantity)).ToList(),
            entry.SuccessRate);

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions)
        ?? throw new GameContentException($"內容檔是空的：{path}");

    private sealed record MonsterFile(IReadOnlyList<MonsterDefinition> Monsters);

    private sealed record WorldFile(
        int StartingPlaceId, int? StartingSubPlace, IReadOnlyList<AreaEntry> Areas, IReadOnlyList<PlaceEntry> Places);

    private sealed record AreaEntry(string Id, string Name, IReadOnlyList<int> PlaceIds);

    private sealed record PlaceEntry(
        int Id, string Name, IReadOnlyList<string> SubPlaces, int[] Recommended, bool Restricted);

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

    private sealed record ItemFile(IReadOnlyList<ItemEntry> Items);

    private sealed record ItemEntry(
        string Id,
        string Name,
        string Category,
        string? Consumable,
        StatBonus? Bonus,
        int SupplyHp,
        int SupplySp,
        int Amount,
        StatRequirement7? Requires,
        string? JobId,
        int Element,
        int Holes,
        int RefineMax,
        long Price,
        int ShopLevel,
        bool ShopSellable,
        bool Bound,
        RefineEntry? Refine,
        StoneEntry? Stone);

    // 能力範圍在內容檔裡寫成 [min, max]
    private sealed record RefineEntry(
        int Level,
        int Kind,
        int[]? At,
        int[]? Mat,
        int[]? Df,
        int[]? Mdf,
        int[]? Str,
        int[]? Smart,
        int[]? Life,
        int[]? Vit,
        int[]? Agi,
        int[]? Au,
        int[]? Be);

    private sealed record StoneEntry(bool Random, StatBonus? Fixed, StatBonus? Min, StatBonus? Max);

    private sealed record RecipeFile(IReadOnlyList<RecipeEntry> Recipes);

    private sealed record RecipeEntry(
        string Id, string ResultItemId, int ResultQuantity, IReadOnlyList<MaterialEntry> Materials, int SuccessRate);

    private sealed record MaterialEntry(string ItemId, int Quantity);

    // 省略的清單視為空、省略的物件視為 null
    private sealed record MissionFile(IReadOnlyList<MissionEntry> Missions);

    private sealed record MissionEntry(
        string? Id,
        string? Giver,
        string? Title,
        string? Description,
        string? CompletionMessage,
        int Level,
        int? PlaceId,
        bool Daily,
        KillTarget? Kill,
        IReadOnlyList<ItemQuantity>? Deliver,
        MissionRequirements? Requirements,
        RewardEntry? Rewards,
        IReadOnlyList<KillTarget>? DailyKills,
        IReadOnlyList<RewardEntry>? DailyRewards);

    private sealed record RewardEntry(long Money, IReadOnlyList<ItemQuantity>? Items, SkillReward? Skill, PetReward? Pet);
}
