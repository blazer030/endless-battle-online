using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Rest;
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
    private const int MaximumBoxClass = 3;
    private const int MaximumStonesPerBox = 3;
    private const int MaximumRefineKind = 18;
    private const int MaximumMissionIdLength = 32;
    private const int MaximumMissionQuantity = 99;
    // WOG 寵物蛋的最大倍率
    private const int MaximumPetRewardLevel = 530;
    private const int MaximumBirth = 4;
    private const int MaximumCompanionIdLength = 32;
    private const int MaximumMercenaryNameLength = 8;
    private const int MaximumPercent = 100;
    private const int MaximumExchangeQuantity = 99;
    private const int MinimumKeySeconds = 60;
    private const int MaximumKeySeconds = 604_800;
    private const double MinimumElixirMultiplier = 1.01;
    private const double MaximumElixirMultiplier = 3.00;
    private const int MinimumElixirSeconds = 60;
    private const int MaximumElixirSeconds = 86_400;

    private static readonly Dictionary<string, BaseStat> BaseStatsByCode = new()
    {
        ["str"] = BaseStat.Strength,
        ["smart"] = BaseStat.Intelligence,
        ["life"] = BaseStat.Life,
        ["vit"] = BaseStat.Vitality,
        ["agi"] = BaseStat.Agility,
        ["au"] = BaseStat.Charisma,
        ["be"] = BaseStat.Faith,
    };

    private readonly Dictionary<string, MonsterDefinition> monsters;
    private readonly Dictionary<string, JobDefinition> jobs;
    private readonly Dictionary<string, SkillDefinition> skills;
    private readonly HashSet<(string JobId, string SkillId)> jobSkills;
    private readonly Dictionary<int, PlaceDefinition> places;
    private readonly Dictionary<string, ItemDefinition> items;
    private readonly Dictionary<string, Recipe> recipes;
    private readonly Dictionary<string, MissionDefinition> missions;
    private readonly Dictionary<string, MercenaryDefinition> mercenaries;

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
        IReadOnlyList<MissionDefinition> allMissions,
        IReadOnlyList<MercenaryDefinition> allMercenaries,
        IReadOnlyList<HonourExchange> honourShop,
        InnDefinition inn,
        HashSet<(string JobId, string SkillId)> jobSkills)
    {
        this.jobSkills = jobSkills;
        Inn = inn;
        AllMercenaries = allMercenaries;
        mercenaries = allMercenaries.ToDictionary(mercenary => mercenary.Id);
        HonourShop = honourShop;
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

    public IReadOnlyList<MercenaryDefinition> AllMercenaries { get; }

    public IReadOnlyList<HonourExchange> HonourShop { get; }

    public InnDefinition Inn { get; }

    public static JsonGameContent Load(string directory)
    {
        var monsterFile = Read<MonsterFile>(Path.Combine(directory, "monsters.json"));
        var jobFile = Read<JobFile>(Path.Combine(directory, "jobs.json"));
        var skillFile = Read<SkillFile>(Path.Combine(directory, "skills.json"));
        var worldFile = Read<WorldFile>(Path.Combine(directory, "world.json"));
        var itemFile = Read<ItemFile>(Path.Combine(directory, "items.json"));
        var recipeFile = Read<RecipeFile>(Path.Combine(directory, "recipes.json"));
        var missionFile = Read<MissionFile>(Path.Combine(directory, "missions.json"));
        var mercenaryFile = Read<MercenaryFile>(Path.Combine(directory, "mercenaries.json"));
        var honourShopFile = Read<HonourShopFile>(Path.Combine(directory, "honour-shop.json"));
        var innFile = Read<InnFile>(Path.Combine(directory, "inn.json"));

        var (allAreas, allPlaces, places) = BuildWorld(worldFile);

        var allMonsters = monsterFile.Monsters.Select(ToMonsterDefinition).ToList();
        var monsters = new Dictionary<string, MonsterDefinition>();
        foreach (var monster in allMonsters)
        {
            if (!monsters.TryAdd(monster.Id, monster))
            {
                throw new GameContentException($"怪物 id 重複：{monster.Id}");
            }

            ValidateMonster(monster, places);
        }

        // 地區沒有怪物就無法遇敵，載入時就擋下；任務怪平常遇不到，不算數
        var emptyPlace = allPlaces.FirstOrDefault(place =>
            !allMonsters.Any(monster =>
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

            ValidateSkill(skill);
        }

        ValidatePrerequisites(allSkills, skills);
        var jobSkills = ReadJobSkills(directory, jobs, skills);
        AttachMonsterSkills(directory, allMonsters, monsters, skills);

        var allItems = itemFile.Items.Select(ToItemDefinition).ToList();
        var items = new Dictionary<string, ItemDefinition>();
        foreach (var item in allItems)
        {
            if (!items.TryAdd(item.Id, item))
            {
                throw new GameContentException($"道具 id 重複：{item.Id}");
            }

            ValidateItem(item, jobs);
            ValidateWeaponSkill(item, skills);
        }

        foreach (var item in allItems.Where(item => item.WeaponSkill is not null).ToList())
        {
            var named = item with { WeaponSkill = item.WeaponSkill! with { SkillName = skills[item.WeaponSkill!.SkillId].Name } };
            allItems[allItems.IndexOf(item)] = named;
            items[item.Id] = named;
        }

        ValidateKeys(allItems, allPlaces);
        ValidateBoxes(allItems);

        foreach (var monster in allMonsters)
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
            ValidateMission(mission, missions, monsters, items, skills, jobs, places, jobSkills);
        }

        var cyclic = allMissions.FirstOrDefault(mission => HasPrerequisiteCycle(mission, missions));
        if (cyclic is not null)
        {
            throw new GameContentException($"任務 {cyclic.Id} 的前置鏈有循環");
        }

        foreach (var monster in allMonsters)
        {
            ValidateMissionMonster(monster, missions);
        }

        // 每日紀錄會過期，不能當前置
        foreach (var recipe in allRecipes)
        {
            if (recipe.RequiredMissionId is { } requiredMissionId
                && (!missions.TryGetValue(requiredMissionId, out var requiredMission) || requiredMission.Daily))
            {
                throw new GameContentException($"配方 {recipe.Id} 的 requiredMissionId 必須是存在的一般任務：{requiredMissionId}");
            }
        }

        var allMercenaries = mercenaryFile.Mercenaries.Select(ToMercenaryDefinition).ToList();
        var mercenaries = new Dictionary<string, MercenaryDefinition>();
        foreach (var mercenary in allMercenaries)
        {
            ValidateCompanionId("傭兵", mercenary.Id);
            if (!mercenaries.TryAdd(mercenary.Id, mercenary))
            {
                throw new GameContentException($"傭兵 id 重複：{mercenary.Id}");
            }

            ValidateMercenary(mercenary);
        }

        var honourShop = honourShopFile.Exchanges.Select(ToHonourExchange).ToList();
        var exchangeIds = new HashSet<string>();
        foreach (var exchange in honourShop)
        {
            ValidateCompanionId("勳章所兌換", exchange.Id);
            if (!exchangeIds.Add(exchange.Id))
            {
                throw new GameContentException($"勳章所兌換 id 重複：{exchange.Id}");
            }

            ValidateExchange(exchange, items, mercenaries);
        }

        var inn = ToInn(innFile);

        return new JsonGameContent(
            allMonsters, allJobs, allSkills, monsters, jobs, skills,
            worldFile, allAreas, allPlaces, places, allItems, items, allRecipes, allMissions,
            allMercenaries, honourShop, inn, jobSkills);
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

    public ItemDefinition? FindItem(string itemId) => items.GetValueOrDefault(itemId);

    public Recipe GetRecipe(string recipeId) =>
        recipes.TryGetValue(recipeId, out var recipe) ? recipe : throw new GameContentException($"找不到配方：{recipeId}");

    public MissionDefinition GetMission(string missionId) =>
        missions.TryGetValue(missionId, out var mission) ? mission : throw new GameContentException($"找不到任務：{missionId}");

    public MercenaryDefinition GetMercenary(string mercenaryId) =>
        mercenaries.TryGetValue(mercenaryId, out var mercenary) ? mercenary : throw new GameContentException($"找不到傭兵：{mercenaryId}");

    public IReadOnlyList<ItemDefinition> ItemsForShop(ItemCategory category, int level) =>
        AllItems.Where(item => item.ShopSellable && item.Category == category && item.ShopLevel == level).ToList();

    public IReadOnlyList<MonsterDefinition> MonstersOf(int placeId) =>
        AllMonsters.Where(monster => monster.PlaceId == placeId || monster.PlaceId == MonsterPool.GlobalPlaceId).ToList();

    public IReadOnlyList<SkillDefinition> SkillsOf(string jobId) =>
        AllSkills.Where(skill => Teaches(jobId, skill.Id)).ToList();

    public bool Teaches(string jobId, string skillId) => jobSkills.Contains((jobId, skillId));

    // 怪物與技能的關聯表：一列是一隻怪的一個技能，發動率取代技能的 Chance 條件；欄位順序就是檔案順序
    private static void AttachMonsterSkills(
        string directory, List<MonsterDefinition> allMonsters, Dictionary<string, MonsterDefinition> monsters, Dictionary<string, SkillDefinition> skills)
    {
        var path = Path.Combine(directory, "monster-skills.json");
        if (!File.Exists(path))
        {
            throw new GameContentException($"缺少內容檔 monster-skills.json：{path}");
        }

        var skillsByMonster = new Dictionary<string, List<EquippedSkill>>();
        foreach (var row in Read<MonsterSkillFile>(path).MonsterSkills)
        {
            var name = $"{row.MonsterId}＋{row.SkillId}";
            if (!monsters.ContainsKey(row.MonsterId) || !skills.TryGetValue(row.SkillId, out var skill))
            {
                throw new GameContentException($"怪物技能 {name} 引用了不存在的怪物或技能");
            }

            if (row.Level < 1 || row.Level > skill.MaxLevel)
            {
                throw new GameContentException($"怪物技能 {name} 的等級 {row.Level} 必須在 1～{skill.MaxLevel}");
            }

            if (row.Rate is < 1 or > 100)
            {
                throw new GameContentException($"怪物技能 {name} 的發動率 {row.Rate} 必須在 1～100");
            }

            var own = skillsByMonster.TryGetValue(row.MonsterId, out var existing) ? existing : skillsByMonster[row.MonsterId] = [];
            if (own.Any(equipped => equipped.Skill.Id == row.SkillId))
            {
                throw new GameContentException($"怪物技能 {name} 重複");
            }

            own.Add(new EquippedSkill(skill.WithActivationRate(row.Rate), row.Level, own.Count + 1));
        }

        for (var index = 0; index < allMonsters.Count; index++)
        {
            if (skillsByMonster.TryGetValue(allMonsters[index].Id, out var own))
            {
                allMonsters[index] = allMonsters[index] with { Skills = own };
                monsters[allMonsters[index].Id] = allMonsters[index];
            }
        }
    }

    // 職業與技能的關聯表：一列是「這個職業能學這個技能」
    private static HashSet<(string JobId, string SkillId)> ReadJobSkills(
        string directory, Dictionary<string, JobDefinition> jobs, Dictionary<string, SkillDefinition> skills)
    {
        var pairs = new HashSet<(string JobId, string SkillId)>();
        foreach (var row in Read<JobSkillFile>(Path.Combine(directory, "job-skills.json")).JobSkills)
        {
            var pair = $"{row.JobId}＋{row.SkillId}";
            if (!jobs.ContainsKey(row.JobId) || !skills.ContainsKey(row.SkillId))
            {
                throw new GameContentException($"職業與技能的關聯 {pair} 引用了不存在的職業或技能");
            }

            if (!pairs.Add((row.JobId, row.SkillId)))
            {
                throw new GameContentException($"職業與技能的關聯 {pair} 重複");
            }
        }

        return pairs;
    }

    private static void ValidatePrerequisites(IReadOnlyList<SkillDefinition> allSkills, Dictionary<string, SkillDefinition> skills)
    {
        foreach (var skill in allSkills)
        {
            if (skill.Prerequisite is { } prerequisite
                && (!skills.TryGetValue(prerequisite.SkillId, out var required) || prerequisite.Level < 1 || prerequisite.Level > required.MaxLevel
                    || prerequisite.SkillId == skill.Id))
            {
                throw new GameContentException($"技能 {skill.Id} 的前置技能 {prerequisite.SkillId} 第 {prerequisite.Level} 級不存在");
            }
        }

        var cyclic = allSkills.FirstOrDefault(skill => HasPrerequisiteCycle(skill, skills));
        if (cyclic is not null)
        {
            throw new GameContentException($"技能 {cyclic.Id} 的前置鏈有循環");
        }
    }

    // 每個技能至多一個前置，沿著前置走回到走過的技能就是循環
    private static bool HasPrerequisiteCycle(SkillDefinition skill, Dictionary<string, SkillDefinition> skills)
    {
        var visited = new HashSet<string> { skill.Id };
        var current = skill;
        while (current.Prerequisite is { } prerequisite)
        {
            if (!visited.Add(prerequisite.SkillId))
            {
                return true;
            }

            current = skills[prerequisite.SkillId];
        }

        return false;
    }

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
        // agi 是命中公式的分母；NPC 不會進入戰鬥
        if (!monster.Npc && (monster.HealthPoints <= 0 || monster.Agility <= 0))
        {
            throw new GameContentException($"怪物 {monster.Id} 的 hp 與 agi 必須大於 0");
        }

        if (!monster.Npc && monster.Level < 1)
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

        if (!place.HasSubPlaces && monster.SubPlaces.Count > 0)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 subPlaces 與地區 {place.Id} 不符：沒有子地區的地區必須為空");
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
        StatRange[] ranges = [growth.Strength, growth.Intelligence, growth.Life, growth.Vitality, growth.Agility, growth.Charisma, growth.Faith];
        if (ranges.Any(range => range.Minimum < 0 || range.Minimum > range.Maximum))
        {
            throw new GameContentException($"職業 {job.Id} 的成長範圍必須 0 ≤ min ≤ max");
        }

    }

    private static void ValidateSkill(SkillDefinition skill)
    {
        if (skill.Levels.Count == 0)
        {
            throw new GameContentException($"技能 {skill.Id} 至少要有一個等級");
        }

        ValidateComposedSkill(skill);
    }

    // 新格式：每一級的種類與第 1 級相同；常駐（沒有條件）的級數不能有消耗、持續回合與冷卻
    private static void ValidateComposedSkill(SkillDefinition skill)
    {
        static string Kinds(SkillLevel level) => string.Join(",",
            level.Conditions!.Select(condition => condition.Kind.ToString())
                .Concat(["|"]).Concat(level.Costs!.Select(cost => cost.Kind.ToString()))
                .Concat(["|"]).Concat(level.Effects!.Select(effect => effect.GetType().Name)));

        var signature = Kinds(skill.Levels[0]);
        foreach (var level in skill.Levels)
        {
            void Require(bool condition, string detail)
            {
                if (!condition)
                {
                    throw new GameContentException($"技能 {skill.Id} 第 {level.Level} 級{detail}");
                }
            }

            Require(level.Money >= 0 && level.Proficiency >= 0 && level.Cooldown >= 0, "的 money、proficiency、cooldown 不可為負");
            Require(level.Effects!.Count > 0, "至少要有一個效果");
            Require(Kinds(level) == signature, "的條件、消耗、效果種類與順序必須與第 1 級相同");
            Require(level.Effects.All(effect => SkillPartRules.EffectChance(effect) is null or >= 1 and <= 100), "的效果機率必須在 1～100");
            Require(level.Effects.OfType<StatChangeEffect>().All(change => change.Chance is null || change.Target == SkillTarget.Opponent),
                "的效果機率只能用在對手的能力增減");
            if (level.Conditions is [])
            {
                // 常駐不會「發動」，效果自己的機率沒有擲的時機
                Require(level.Effects.All(effect => SkillPartRules.EffectChance(effect) is null), "沒有發動條件（常駐），效果不能有機率");
                Require(level.Costs is [] && level.Cooldown == 0 && level.Effects!.All(effect => effect.Rounds == 0),
                    "沒有發動條件（常駐），不能有消耗、冷卻或持續回合");
            }

            foreach (var condition in level.Conditions!)
            {
                Require(condition.Kind switch
                {
                    SkillConditionKind.Chance => condition.Percent is >= 1 and <= 100,
                    SkillConditionKind.HitPointsBelow => condition.Percent is >= 1 and <= 99,
                    SkillConditionKind.StatAbove => condition.Stat is not (null or SkillScalingBasis.SkillPoints),
                    _ => true,
                }, $"的條件 {condition.Kind} 的數值超出範圍");
            }

            Require(level.Costs!.All(cost => cost.All || cost.Percent is > 0 and <= 100 || cost.Percent is null && cost.Amount >= 1),
                "的消耗必須至少 1，百分比在 0～100");
            foreach (var effect in level.Effects!)
            {
                Require(effect.Rounds >= 0, "的效果 rounds 不可為負");
                var valid = effect switch
                {
                    StatChangeEffect change when SkillPartRules.IsPercentagePoint(change.Stat) && change.Percent is not null => false,
                    StatChangeEffect { Target: SkillTarget.Opponent } change => change.Stat is SkillStat.Attack or SkillStat.MagicAttack
                        or SkillStat.Defense or SkillStat.MagicDefense or SkillStat.Agility or SkillStat.Charisma or SkillStat.HitRate,
                    MultiHitEffect multiHit => multiHit.Hits >= 2,
                    DamageReductionEffect reduction => reduction.Percent is > 0 and < 100,
                    StunEffect stun => stun.Chance is >= 1 and <= 100
                        && stun.ChanceByStat is null or { Percent: >= 1 and <= 100, Stat: not SkillScalingBasis.SkillPoints },
                    SilenceEffect silence => silence.Chance is >= 1 and <= 100,
                    PercentDamageEffect damage => damage.Percent is > 0 and <= 100,
                    ReduceSkillPointsEffect loss => loss.Amount is null or >= 1 && loss.PercentOfMaximum is null or > 0 and <= 100,
                    ElementAdvantageEffect advantage => advantage.Percent > 0,
                    _ => true,
                };
                Require(valid, $"的效果 {effect.GetType().Name.Replace("Effect", "")} 的數值不在支援的範圍");
            }
        }
    }

    private static void ValidateWeaponSkill(ItemDefinition item, Dictionary<string, SkillDefinition> skills)
    {
        if (item.WeaponSkill is not { } weaponSkill)
        {
            return;
        }

        if (item.Category != ItemCategory.Weapon)
        {
            throw new GameContentException($"道具 {item.Id} 不是武器，不能附帶技能");
        }

        if (!skills.TryGetValue(weaponSkill.SkillId, out var skill))
        {
            throw new GameContentException($"道具 {item.Id} 的附帶技能 {weaponSkill.SkillId} 不存在");
        }

        if (weaponSkill.Level < 1 || weaponSkill.Level > skill.MaxLevel)
        {
            throw new GameContentException($"道具 {item.Id} 的附帶技能等級 {weaponSkill.Level} 必須在 1～{skill.MaxLevel}");
        }

        if (weaponSkill.Rate is < 1 or > 100)
        {
            throw new GameContentException($"道具 {item.Id} 的附帶技能發動率 {weaponSkill.Rate} 必須在 1～100");
        }
    }

    private static void ValidateItem(ItemDefinition item, Dictionary<string, JobDefinition> jobs)
    {
        void Require([DoesNotReturnIf(false)] bool condition, string message)
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
                    && item.Refine is null && item.Stone is null && item.Key is null && item.Elixir is null,
                $"裝備 {id} 不能有 consumable、supplyHp、supplySp、amount、refine、stone、key、elixir");
            return;
        }

        Require(item.Requires == EquipmentRequirement.None && item.JobId is null, $"非裝備道具 {id} 不能有能力需求（requires）或 jobId");
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

        var usesAmount = item.Consumable is ConsumableKind.BagExpansion or ConsumableKind.DepotExpansion or ConsumableKind.Capture
            or ConsumableKind.StoneBox or ConsumableKind.RefineBox or ConsumableKind.PetEgg
            or ConsumableKind.ProficiencyBook or ConsumableKind.ExperienceCapsule;
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
            Require(item.Consumable != ConsumableKind.StoneBox || item.Amount <= MaximumStonesPerBox,
                $"魔石寶箱 {id} 的 amount 必須在 1～{MaximumStonesPerBox}");
        }

        Require(item.Consumable == ConsumableKind.Supply || (item.SupplyHp == 0 && item.SupplySp == 0),
            $"道具 {id} 不是補給品，supplyHp 與 supplySp 必須為 0");
        Require(usesAmount || item.Amount == 0, $"道具 {id} 的 amount 只用於擴充、捕捉道具、寶箱、寵物蛋、心得書與經驗膠囊");
        Require(item.Consumable == ConsumableKind.StatPotion || item.Bonus == StatBonus.Zero,
            $"道具 {id} 不能有 bonus（只有能力藥水可以）");

        if (item.Category == ItemCategory.RefineStone)
        {
            Require(item.Refine is not null, $"精鍊石 {id} 必須有 refine");
            ValidateRefineStone(id, item.Refine);
        }
        else
        {
            Require(item.Refine is null, $"道具 {id} 不是精鍊石，不能有 refine");
        }

        if (item.Category == ItemCategory.Stone)
        {
            Require(item.Stone is not null, $"魔石 {id} 必須有 stone");
            ValidateStone(id, item.Stone);
        }
        else
        {
            Require(item.Stone is null, $"道具 {id} 不是魔石，不能有 stone");
        }

        if (item.Category == ItemCategory.Key)
        {
            Require(item.Key is not null, $"鑰匙 {id} 必須有 key");
            Require(item.Key.Seconds is >= MinimumKeySeconds and <= MaximumKeySeconds,
                $"鑰匙 {id} 的 seconds 必須在 {MinimumKeySeconds}～{MaximumKeySeconds}");
        }
        else
        {
            Require(item.Key is null, $"道具 {id} 不是鑰匙，不能有 key");
        }

        if (item.Consumable == ConsumableKind.Elixir)
        {
            Require(item.Elixir is not null, $"密藥 {id} 必須有 elixir");
            Require(item.Elixir.Multiplier is >= MinimumElixirMultiplier and <= MaximumElixirMultiplier,
                $"密藥 {id} 的 multiplier 必須在 {MinimumElixirMultiplier:0.00}～{MaximumElixirMultiplier:0.00}");
            // 資料庫欄位是 DECIMAL(4,2)
            Require((decimal)item.Elixir.Multiplier == Math.Round((decimal)item.Elixir.Multiplier, 2),
                $"密藥 {id} 的 multiplier 小數最多兩位：{item.Elixir.Multiplier}");
            Require(item.Elixir.Seconds is >= MinimumElixirSeconds and <= MaximumElixirSeconds,
                $"密藥 {id} 的 seconds 必須在 {MinimumElixirSeconds}～{MaximumElixirSeconds}");
        }
        else
        {
            Require(item.Elixir is null, $"道具 {id} 不是密藥，不能有 elixir");
        }
    }

    // 鑰匙必須指向禁區，每個禁區恰好一把
    private static void ValidateKeys(IReadOnlyList<ItemDefinition> items, IReadOnlyList<PlaceDefinition> places)
    {
        var restricted = places.Where(place => place.Restricted).Select(place => place.Id).ToHashSet();
        var keyed = new HashSet<int>();
        foreach (var item in items)
        {
            if (item.Key is not { PlaceId: var placeId })
            {
                continue;
            }

            if (!restricted.Contains(placeId))
            {
                throw new GameContentException($"鑰匙 {item.Id} 的 placeId {placeId} 不是禁區");
            }

            if (!keyed.Add(placeId))
            {
                throw new GameContentException($"禁區 {placeId} 的鑰匙不只一把：{item.Id}");
            }
        }

        var missing = restricted.Except(keyed).Order().ToList();
        if (missing.Count > 0)
        {
            throw new GameContentException($"禁區 {string.Join("、", missing)} 沒有鑰匙");
        }
    }

    // 魔石寶箱至少要能開出一顆 boxClass 1 的魔石；精鍊寶盒的 amount 必須是存在的精鍊石等級
    private static void ValidateBoxes(IReadOnlyList<ItemDefinition> items)
    {
        var hasClassOneStone = items.Any(item => item.Stone?.BoxClass == 1);
        var refineLevels = items.Select(item => item.Refine).OfType<RefineStoneSpec>().Select(refine => refine.Level).ToHashSet();
        foreach (var item in items)
        {
            if (item.Consumable == ConsumableKind.StoneBox && !hasClassOneStone)
            {
                throw new GameContentException($"魔石寶箱 {item.Id} 開不出東西：沒有 boxClass 1 的魔石");
            }

            if (item.Consumable == ConsumableKind.RefineBox && !refineLevels.Contains(item.Amount))
            {
                throw new GameContentException($"精鍊寶盒 {item.Id} 的 amount {item.Amount} 不是存在的精鍊石等級");
            }
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

        if (ranges.Any(range => range is not null && (range.Minimum < 0 || range.Minimum > range.Maximum)))
        {
            throw new GameContentException($"精鍊石 {id} 的能力範圍必須 0 ≤ min ≤ max");
        }
    }

    private static void ValidateStone(string id, StoneSpec stone)
    {
        if (stone.BoxClass is < 0 or > MaximumBoxClass)
        {
            throw new GameContentException($"魔石 {id} 的 boxClass 必須在 0～{MaximumBoxClass}");
        }

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
        bonus.Strength, bonus.Intelligence, bonus.Life, bonus.Vitality, bonus.Agility, bonus.Charisma, bonus.Faith,
        bonus.Attack, bonus.MagicAttack, bonus.Defense, bonus.MagicDefense, bonus.MaxHealthPoints,
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
        Dictionary<int, PlaceDefinition> places,
        HashSet<(string JobId, string SkillId)> jobSkills)
    {
        var id = mission.Id;

        void Require([DoesNotReturnIf(false)] bool condition, string message)
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
            Require(!target.Daily, $"的 {field} 不能是每日任務：{targetId}");
        }

        MonsterDefinition RequireKill(KillTarget kill)
        {
            Require(monsters.TryGetValue(kill.MonsterId, out var monster), $"引用了不存在的怪物：{kill.MonsterId}");
            Require(kill.Count >= 1, $"的擊殺目標 {kill.MonsterId} 的 count 必須至少 1");
            return monster;
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
                Require(requirements.JobId is { } jobId && jobSkills.Contains((jobId, skill.Id)),
                    $"的技能獎勵 {skillReward.SkillId} 必須是 requirements.jobId 的職業能學的技能（job-skills.json）");
            }

            if (reward.Pet is { } pet)
            {
                RequirePetSpecies($"{field} pet", pet.MonsterId);
                Require(pet.Level is >= 1 and <= MaximumPetRewardLevel,
                    $"的 {field} 寵物 {pet.MonsterId} 的 level 必須在 1～{MaximumPetRewardLevel}");
            }
        }

        // 寵物物種：存在、不是任務怪
        void RequirePetSpecies(string field, string monsterId)
        {
            Require(monsters.TryGetValue(monsterId, out var monster), $"的 {field} 引用了不存在的怪物：{monsterId}");
            Require(monster.MissionId is null, $"的 {field} 寵物不能是任務怪：{monsterId}");
        }

        if (mission.Daily)
        {
            Require(mission.PlaceId is null, "是每日任務，placeId 必須為 null");
            Require(mission.Kills.Count == 0 && mission.Deliver.Count == 0 && mission.DeliverPet is null, "是每日任務，不能有 kills、deliver 或 deliverPet");
            Require(mission.RequiredSkill is null, "是每日任務，不能有 requiredSkill");
            Require(requirements.PrerequisiteId is null && requirements.ConflictsWith is null,
                "是每日任務，不能有 prerequisiteId 或 conflictsWith");
            Require(mission.Rewards is { Money: 0, Items.Count: 0, Skill: null, Pet: null },
                "是每日任務，rewards 必須為空（改用 dailyRewards）");
            Require(mission.DailyKills.Count > 0 ^ mission.DailyPets.Count > 0, "是每日任務，dailyKills 與 dailyPets 必須恰好一個非空");
            Require(mission.DailyRewards.Count >= 1, "是每日任務，dailyRewards 至少要有一項");
            foreach (var species in mission.DailyPets)
            {
                RequirePetSpecies("dailyPets", species);
            }

            Require(mission.DailyPets.Distinct().Count() == mission.DailyPets.Count, "的 dailyPets 物種重複");
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

        if (mission.PlaceId is not { } placeId || !places.TryGetValue(placeId, out var place))
        {
            throw new GameContentException($"任務 {id} 的 placeId 必須是存在的地區：{mission.PlaceId?.ToString() ?? "null"}");
        }

        Require(!place.Restricted, $"的地區 {place.Id} 是禁區");
        Require(mission.Kills.Count > 0 || mission.Deliver.Count > 0 || mission.DeliverPet is not null, "至少要有擊殺目標（kills）、交付道具（deliver）或交付寵物（deliverPet）");
        Require(mission.DailyKills.Count == 0, "不是每日任務，不能有 dailyKills");
        Require(mission.DailyPets.Count == 0, "不是每日任務，不能有 dailyPets");
        Require(mission.DailyRewards.Count == 0, "不是每日任務，不能有 dailyRewards");
        Require(mission.Kills.Select(kill => kill.MonsterId).Distinct().Count() == mission.Kills.Count, "的 kills 怪物重複");
        foreach (var fixedKill in mission.Kills)
        {
            var monster = RequireKill(fixedKill);
            Require(monster.PlaceId == place.Id, $"的擊殺目標 {fixedKill.MonsterId} 不在地區 {place.Id}");
            Require(monster.MissionId is null || monster.MissionId == id,
                $"的擊殺目標 {fixedKill.MonsterId} 是任務 {monster.MissionId} 的任務怪");
        }

        if (mission.DeliverPet is { } deliverPet)
        {
            Require(monsters.TryGetValue(deliverPet, out var species), $"的 deliverPet 引用了不存在的怪物：{deliverPet}");
            Require(species.MissionId is null || species.MissionId == id, $"的 deliverPet {deliverPet} 是任務 {species.MissionId} 的任務怪");
            Require(!species.Npc, $"的 deliverPet {deliverPet} 是 NPC");
        }

        if (mission.RequiredSkill is { } requiredSkill)
        {
            Require(skills.TryGetValue(requiredSkill.SkillId, out var skill), $"的 requiredSkill 引用了不存在的技能：{requiredSkill.SkillId}");
            Require(requiredSkill.Level >= 1 && requiredSkill.Level <= skill.MaxLevel,
                $"的 requiredSkill {requiredSkill.SkillId} 的 level 必須在 1～{skill.MaxLevel}");
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
        if (monster.Npc && (monster.MissionId is null || monster.Drops.Count > 0 || monster.Skills.Count > 0))
        {
            throw new GameContentException($"NPC {monster.Id} 必須有 missionId，且不能有掉落與技能");
        }

        if (monster.MissionId is not { } missionId)
        {
            return;
        }

        if (monster.PlaceId == MonsterPool.GlobalPlaceId)
        {
            throw new GameContentException($"怪物 {monster.Id} 是全域怪，不能是任務怪（missionId {missionId}）");
        }

        if (!missions.TryGetValue(missionId, out var mission) || mission.Daily)
        {
            throw new GameContentException($"怪物 {monster.Id} 的 missionId {missionId} 必須指向一般任務");
        }

        if (monster.Npc && mission.Kills.All(kill => kill.MonsterId != monster.Id))
        {
            throw new GameContentException($"NPC {monster.Id} 必須是任務 {missionId} 的擊殺目標");
        }
    }

    private static void ValidateCompanionId(string kind, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new GameContentException($"{kind} id 不可為空");
        }

        if (id.Length > MaximumCompanionIdLength)
        {
            throw new GameContentException($"{kind} id 長度不可超過 {MaximumCompanionIdLength}：{id}");
        }
    }

    private static void ValidateMercenary(MercenaryDefinition mercenary)
    {
        void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new GameContentException($"傭兵 {mercenary.Id} {message}");
            }
        }

        Require(!string.IsNullOrWhiteSpace(mercenary.Name) && mercenary.Name.Length <= MaximumMercenaryNameLength,
            $"的 name 必須是 1～{MaximumMercenaryNameLength} 字");
        Require(mercenary.Price is null or >= 1, "的 price 必須是 null（只能在勳章所兌換）或至少 1");
        Require(mercenary.Fights >= 1, "的 fights 必須至少 1");
        (string Field, int Value)[] percents =
        [
            ("moneyPercent", mercenary.MoneyPercent), ("dropPercent", mercenary.DropPercent), ("expPercent", mercenary.ExpPercent),
        ];
        foreach (var (field, value) in percents)
        {
            Require(value is >= 1 and <= MaximumPercent, $"的 {field} 必須在 1～{MaximumPercent}");
        }

        Require(mercenary.DeathCost >= 1, "的 deathCost 必須至少 1");
    }

    private static void ValidateExchange(
        HonourExchange exchange, Dictionary<string, ItemDefinition> items, Dictionary<string, MercenaryDefinition> mercenaries)
    {
        void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new GameContentException($"勳章所兌換 {exchange.Id} {message}");
            }
        }

        Require(exchange.ItemId is null != exchange.MercenaryId is null, "的 itemId 與 mercenaryId 必須恰好一個");
        if (exchange.ItemId is { } itemId)
        {
            Require(items.ContainsKey(itemId), $"引用了不存在的道具：{itemId}");
            Require(exchange.Quantity is >= 1 and <= MaximumExchangeQuantity, $"的 quantity 必須在 1～{MaximumExchangeQuantity}");
        }
        else if (exchange.MercenaryId is { } mercenaryId)
        {
            Require(mercenaries.ContainsKey(mercenaryId), $"引用了不存在的傭兵：{mercenaryId}");
            Require(exchange.Quantity == 1, "兌換傭兵，quantity 必須為 1");
        }

        Require(exchange.Cost.Count >= 1, "的 cost 至少要有一項");
        foreach (var (costItemId, quantity) in exchange.Cost)
        {
            Require(items.TryGetValue(costItemId, out var costItem), $"的 cost 引用了不存在的道具：{costItemId}");
            Require(costItem.Category == ItemCategory.Medal, $"的 cost {costItemId} 必須是勳章");
            Require(quantity is >= 1 and <= MaximumExchangeQuantity,
                $"的 cost {costItemId} 的 quantity 必須在 1～{MaximumExchangeQuantity}");
        }

        Require(exchange.Cost.Select(cost => cost.ItemId).Distinct().Count() == exchange.Cost.Count, "的 cost 道具重複");
    }

    private static MercenaryDefinition ToMercenaryDefinition(MercenaryEntry entry) =>
        new(entry.Id ?? "", entry.Name ?? "", entry.Price, entry.Fights, entry.MoneyPercent, entry.DropPercent, entry.ExpPercent,
            entry.DeathCost);

    private static InnDefinition ToInn(InnFile file)
    {
        void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new GameContentException($"旅館 {message}");
            }
        }

        Require(file.Seconds is >= MinimumElixirSeconds and <= MaximumElixirSeconds,
            $"的 seconds 必須在 {MinimumElixirSeconds}～{MaximumElixirSeconds}");
        Require(file.PricePerLevel >= 1, "的 pricePerLevel 必須至少 1");
        Require(file.Rooms is { Count: > 0 }, "的 rooms 不可為空");

        var ids = new HashSet<string>();
        var rooms = new List<InnRoom>();
        foreach (var entry in file.Rooms)
        {
            var id = entry.Id ?? "";
            Require(id.Length > 0, "的房型必須有 id");
            Require(ids.Add(id), $"房型 id 重複：{id}");
            Require(!string.IsNullOrWhiteSpace(entry.Name), $"房型 {id} 必須有 name");
            Require(Enum.TryParse<BuffKind>(entry.Kind, ignoreCase: true, out var kind) && Enum.IsDefined(kind),
                $"房型 {id} 的 kind 無法辨識：{entry.Kind}");
            Require(entry.Multiplier is >= MinimumElixirMultiplier and <= MaximumElixirMultiplier,
                $"房型 {id} 的 multiplier 必須在 {MinimumElixirMultiplier:0.00}～{MaximumElixirMultiplier:0.00}");
            Require((decimal)entry.Multiplier == Math.Round((decimal)entry.Multiplier, 2),
                $"房型 {id} 的 multiplier 小數最多兩位：{entry.Multiplier}");
            Require(entry.PriceRate >= 1, $"房型 {id} 的 priceRate 必須至少 1");

            rooms.Add(new InnRoom(id, entry.Name!, kind, entry.Multiplier, entry.PriceRate));
        }

        return new InnDefinition(rooms, file.Seconds, file.PricePerLevel);
    }

    private static HonourExchange ToHonourExchange(ExchangeEntry entry) =>
        new(entry.Id ?? "", entry.ItemId, entry.MercenaryId, entry.Quantity, entry.Cost ?? []);

    // 漏寫 placeId 不能默默變成全域怪：全域怪必須明寫 0
    private static MonsterDefinition ToMonsterDefinition(JsonElement element)
    {
        var entry = element.Deserialize<MonsterEntry>(SerializerOptions) ?? throw new GameContentException("怪物不可為 null");
        if (!element.TryGetProperty("placeId", out _))
        {
            throw new GameContentException($"怪物 {entry.Id} 必須寫明 placeId（全域怪寫 0）");
        }

        return new MonsterDefinition(
            entry.Id,
            entry.Name,
            entry.Level,
            entry.Hp,
            entry.Sp,
            entry.At,
            entry.Mat,
            entry.Df,
            entry.Mdf,
            entry.Agi,
            entry.Au,
            entry.Element,
            entry.JobExp,
            entry.Meet,
            entry.PlaceId,
            entry.SubPlaces,
            entry.Drops,
            entry.MissionId,
            entry.Str,
            entry.Smart,
            entry.Vit,
            entry.Be)
        {
            Npc = entry.Npc,
            NpcMessage = entry.NpcMessage,
        };
    }

    // 巢狀物件裡漏寫的必填字串在這裡擋下，之後的驗證才不會拿 null 去查表（ArgumentNullException）
    private static MissionDefinition ToMissionDefinition(MissionEntry entry)
    {
        var id = entry.Id ?? "";

        void Require(string? value, string field)
        {
            if (value is null)
            {
                throw new GameContentException($"任務 {id} 的 {field} 不可省略");
            }
        }

        void RequireItems(IReadOnlyList<ItemQuantity>? items, string field)
        {
            foreach (var item in items ?? [])
            {
                Require(item.ItemId, $"{field} 的 itemId");
            }
        }

        MissionReward ToReward(RewardEntry? reward, string field)
        {
            if (reward is null)
            {
                return new MissionReward(0, []);
            }

            RequireItems(reward.Items, $"{field} items");
            if (reward.Skill is { } skill)
            {
                Require(skill.SkillId, $"{field} skill 的 skillId");
            }

            if (reward.Pet is { } pet)
            {
                Require(pet.MonsterId, $"{field} pet 的 monsterId");
            }

            return new MissionReward(reward.Money, reward.Items ?? [], reward.Skill, reward.Pet);
        }

        foreach (var kill in entry.Kills ?? [])
        {
            Require(kill.MonsterId, "kills 的 monsterId");
        }

        RequireItems(entry.Deliver, "deliver");
        foreach (var dailyKill in entry.DailyKills ?? [])
        {
            Require(dailyKill.MonsterId, "dailyKills 的 monsterId");
        }

        foreach (var species in entry.DailyPets ?? [])
        {
            Require(species, "dailyPets 的怪物 id");
        }

        return new MissionDefinition(
            entry.Id ?? "",
            entry.Giver ?? "",
            entry.Title ?? "",
            entry.Description ?? "",
            entry.CompletionMessage ?? "",
            entry.Level,
            entry.PlaceId,
            entry.Daily,
            entry.Kills ?? [],
            entry.Deliver ?? [],
            entry.Requirements ?? new MissionRequirements(),
            ToReward(entry.Rewards, "rewards"),
            entry.DailyKills ?? [],
            (entry.DailyRewards ?? []).Select(reward => ToReward(reward, "dailyRewards")).ToList(),
            entry.DailyPets ?? []) { DeliverPet = entry.DeliverPet, RequiredSkill = entry.RequiredSkill };
    }

    private static SkillDefinition ToSkillDefinition(SkillEntry entry)
    {
        T ParseEnum<T>(string value, string field) where T : struct, Enum =>
            Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new GameContentException($"技能 {entry.Id} 的 {field} 無法辨識：{value}");

        if (entry.MaxLevel is not { } maxLevel || maxLevel < 1 || entry.Levels.Count != maxLevel
            || entry.Levels.Select(level => level.Level).Where((value, index) => value != index + 1).Any())
        {
            throw new GameContentException($"技能 {entry.Id} 的 maxLevel 必須至少 1，且等級剛好是 1～maxLevel 各一級");
        }

        var levels = entry.Levels.Select(level =>
        {
            if (level.Effects is null || level.Conditions is null || level.Costs is null)
            {
                throw new GameContentException($"技能 {entry.Id} 第 {level.Level} 級必須有 conditions、costs、effects");
            }

            var reader = new SkillPartsReader(entry.Id, level.Level);
            return new SkillLevel(level.Level, level.Money, level.Proficiency, level.Cooldown,
                [.. level.Conditions.Select(reader.Condition)], [.. level.Costs.Select(reader.Cost)], [.. level.Effects.Select(reader.Effect)]);
        });

        return new SkillDefinition(
            entry.Id, entry.Name, ParseEnum<SkillType>(entry.SlotType ?? "", "slotType"), ParseEnum<SkillTiming>(entry.Timing, "timing"), entry.Description, [.. levels])
        {
            Icon = entry.Icon,
            Prerequisite = entry.Prerequisite is { } prerequisite ? new SkillPrerequisite(prerequisite.SkillId, prerequisite.Level) : null,
        };
    }

    // 成長在內容檔裡寫成 [min, max]
    private static JobDefinition ToJobDefinition(JobEntry entry)
    {
        BaseStat ToBaseStat(string code) => BaseStatsByCode.TryGetValue(code, out var stat)
            ? stat
            : throw new GameContentException($"職業 {entry.Id} 的能力需求引用了不存在的能力值：{code}");

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
            entry.StatRequirements.Select(requirement => new StatRequirement(ToBaseStat(requirement.Stat), requirement.Minimum)).ToList(),
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
            ? new StoneSpec(
                stoneEntry.Random, stoneEntry.Fixed?.ToDomain() ?? StatBonus.Zero, stoneEntry.Min?.ToDomain(), stoneEntry.Max?.ToDomain(), stoneEntry.BoxClass)
            : null;

        return new ItemDefinition(
            entry.Id,
            entry.Name,
            ParseEnum<ItemCategory>(entry.Category, "category"),
            ParseEnum<ConsumableKind>(entry.Consumable ?? nameof(ConsumableKind.None), "consumable"),
            entry.Bonus?.ToDomain() ?? StatBonus.Zero,
            entry.SupplyHp,
            entry.SupplySp,
            entry.Amount,
            entry.Requires?.ToDomain() ?? EquipmentRequirement.None,
            entry.JobId,
            entry.Element,
            entry.Holes,
            entry.RefineMax,
            entry.Price,
            entry.ShopLevel,
            entry.ShopSellable,
            entry.Bound,
            refine,
            stone,
            entry.Key,
            entry.Elixir is { } elixir
                ? new ElixirSpec(ParseEnum<BuffKind>(elixir.Kind, "elixir.kind"), elixir.Multiplier, elixir.Seconds)
                : null) { WeaponSkill = entry.WeaponSkill };
    }

    private static Recipe ToRecipe(RecipeEntry entry) =>
        new(
            entry.Id,
            entry.ResultItemId,
            entry.ResultQuantity,
            entry.Materials.Select(material => (material.ItemId, material.Quantity)).ToList(),
            entry.SuccessRate) { RequiredMissionId = entry.RequiredMissionId };

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions)
        ?? throw new GameContentException($"內容檔是空的：{path}");

    // 逐筆讀成 JsonElement，才能分辨 placeId 是漏寫還是寫 0
    private sealed record MonsterFile(IReadOnlyList<JsonElement> Monsters);

    // 省略的能力為 0；Str、Smart、Vit、Be 只給技能條件比較用
    private sealed record MonsterEntry(
        string Id,
        string Name,
        int Level,
        int Hp,
        int Sp,
        int At,
        int Mat,
        int Df,
        int Mdf,
        int Agi,
        int Au,
        int Element,
        long JobExp,
        int Meet,
        int PlaceId,
        IReadOnlyList<int>? SubPlaces,
        IReadOnlyList<DropEntry>? Drops,
        string? MissionId,
        int Str,
        int Smart,
        int Vit,
        int Be,
        bool Npc,
        string? NpcMessage);

    private sealed record WorldFile(
        int StartingPlaceId, int? StartingSubPlace, IReadOnlyList<AreaEntry> Areas, IReadOnlyList<PlaceEntry> Places);

    private sealed record AreaEntry(string Id, string Name, IReadOnlyList<int> PlaceIds);

    private sealed record PlaceEntry(
        int Id, string Name, IReadOnlyList<string> SubPlaces, int[] Recommended, bool Restricted);

    private sealed record SkillFile(IReadOnlyList<SkillEntry> Skills);

    private sealed record SkillEntry(
        string Id,
        string Name,
        string? SlotType,
        string Timing,
        string Description,
        IReadOnlyList<SkillLevelEntry> Levels,
        int? MaxLevel,
        PrerequisiteEntry? Prerequisite,
        string? Icon);

    private sealed record SkillLevelEntry(
        int Level,
        long Money,
        long Proficiency,
        int Cooldown,
        IReadOnlyList<JsonElement>? Conditions,
        IReadOnlyList<JsonElement>? Costs,
        IReadOnlyList<JsonElement>? Effects);

    private sealed record PrerequisiteEntry(string SkillId, int Level);

    private sealed record JobSkillFile(IReadOnlyList<JobSkillEntry> JobSkills);

    private sealed record JobSkillEntry(string JobId, string SkillId);

    private sealed record MonsterSkillFile(IReadOnlyList<MonsterSkillEntry> MonsterSkills);

    private sealed record MonsterSkillEntry(string MonsterId, string SkillId, int Level, int Rate);

    private sealed record JobFile(IReadOnlyList<JobEntry> Jobs);

    private sealed record JobEntry(
        string Id,
        string Name,
        int Tier,
        string? ParentId,
        int UnlockLevel,
        int ProficiencyRequired,
        IReadOnlyList<StatRequirementCodeEntry> StatRequirements,
        GrowthEntry Growth);

    // 職業需求的能力在內容檔裡寫成代碼（str、smart…）
    private sealed record StatRequirementCodeEntry(string Stat, int Minimum);

    private sealed record GrowthEntry(int[]? Str, int[]? Smart, int[]? Life, int[]? Vit, int[]? Agi, int[]? Au, int[]? Be);

    private sealed record ItemFile(IReadOnlyList<ItemEntry> Items);

    private sealed record ItemEntry(
        string Id,
        string Name,
        string Category,
        string? Consumable,
        StatBonusEntry? Bonus,
        int SupplyHp,
        int SupplySp,
        int Amount,
        StatRequirementEntry? Requires,
        string? JobId,
        int Element,
        int Holes,
        int RefineMax,
        long Price,
        int ShopLevel,
        bool ShopSellable,
        bool Bound,
        RefineEntry? Refine,
        StoneEntry? Stone,
        AreaKeySpec? Key,
        ElixirEntry? Elixir,
        WeaponSkillSpec? WeaponSkill);

    private sealed record ElixirEntry(string Kind, double Multiplier, int Seconds);

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

    private sealed record StoneEntry(bool Random, StatBonusEntry? Fixed, StatBonusEntry? Min, StatBonusEntry? Max, int BoxClass = 0);

    // 內容檔的能力鍵是儲存格式，與 Domain 的命名脫鉤；省略的能力為 0
    private sealed record StatBonusEntry(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax)
    {
        public StatBonus ToDomain() => new(Str, Smart, Life, Vit, Agi, Au, Be, At, Mat, Df, Mdf, HpMax);
    }

    private sealed record StatRequirementEntry(int Str, int Smart, int Agi, int Au)
    {
        public EquipmentRequirement ToDomain() => new(Str, Smart, Agi, Au);
    }

    private sealed record RecipeFile(IReadOnlyList<RecipeEntry> Recipes);

    private sealed record RecipeEntry(
        string Id, string ResultItemId, int ResultQuantity, IReadOnlyList<MaterialEntry> Materials, int SuccessRate, string? RequiredMissionId);

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
        IReadOnlyList<KillTarget>? Kills,
        IReadOnlyList<ItemQuantity>? Deliver,
        MissionRequirements? Requirements,
        RewardEntry? Rewards,
        IReadOnlyList<KillTarget>? DailyKills,
        IReadOnlyList<RewardEntry>? DailyRewards,
        IReadOnlyList<string>? DailyPets,
        string? DeliverPet,
        RequiredSkill? RequiredSkill);

    private sealed record RewardEntry(long Money, IReadOnlyList<ItemQuantity>? Items, SkillReward? Skill, PetReward? Pet);

    private sealed record MercenaryFile(IReadOnlyList<MercenaryEntry> Mercenaries);

    private sealed record MercenaryEntry(
        string? Id, string? Name, long? Price, int Fights, int MoneyPercent, int DropPercent, int ExpPercent, int DeathCost);

    private sealed record HonourShopFile(IReadOnlyList<ExchangeEntry> Exchanges);

    private sealed record ExchangeEntry(string? Id, string? ItemId, string? MercenaryId, int Quantity, IReadOnlyList<ItemQuantity>? Cost);

    private sealed record InnFile(int Seconds, int PricePerLevel, IReadOnlyList<InnRoomEntry>? Rooms);

    private sealed record InnRoomEntry(string? Id, string? Name, string? Kind, double Multiplier, int PriceRate);
}
