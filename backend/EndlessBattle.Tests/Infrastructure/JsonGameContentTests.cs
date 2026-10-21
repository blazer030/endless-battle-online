using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class JsonGameContentTests
{
    private static readonly string ShippedContentDirectory = Path.Combine(BackendPaths.BackendDirectory, "content");

    private const string ValidMonster =
        """{ "id": "sprite", "name": "雪精", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0 }""";

    private const string ValidMonsters = $$"""{ "monsters": [{{ValidMonster}}] }""";

    private const string ValidGrowth =
        """{ "str": [2, 3], "smart": [0, 1], "life": [1, 2], "vit": [1, 2], "agi": [1, 2], "au": [0, 1], "be": [0, 1] }""";

    private const string ValidWarrior =
        $$"""{ "id": "warrior", "name": "戰士", "tier": 1, "parentId": null, "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [], "growth": {{ValidGrowth}} }""";

    private const string ValidWorld = """
        { "startingPlaceId": 1, "startingSubPlace": 1,
          "areas": [{ "id": "central", "name": "中", "placeIds": [1, 2] }],
          "places": [
            { "id": 1, "name": "甲", "subPlaces": ["a", "b", "c", "d"], "recommended": [1, 10], "restricted": false },
            { "id": 2, "name": "乙", "subPlaces": [], "recommended": [5, 15], "restricted": false }] }
        """;

    private const string ValidSkills = """{ "skills": [] }""";

    private const string ValidItems = """{ "items": [] }""";

    private const string ValidRecipes = """{ "recipes": [] }""";

    private const string ValidLevel = """{ "level": 1, "sp": 5, "rounds": 3, "chance": 0, "money": 500, "proficiency": 300 }""";

    private const string ValidJobs = $$"""{ "jobs": [{{ValidWarrior}}] }""";

    // 怪物數值範圍：hp、agi（命中公式的分母）必須大於 0，屬性 0～6，遇敵率 0～100，等級至少 1
    public static TheoryData<string, string> InvalidMonsterStats => new()
    {
        { "\"hp\": 60", "\"hp\": 0" },
        { "\"agi\": 9", "\"agi\": 0" },
        { "\"element\": 2", "\"element\": 7" },
        { "\"element\": 2", "\"element\": -1" },
        { "\"meet\": 0", "\"meet\": 101" },
        { "\"meet\": 0", "\"meet\": -1" },
        { "\"level\": 3", "\"level\": 0" },
    };

    public static TheoryData<string, string> InvalidJobs => new()
    {
        { $$"""{ "jobs": [{ "id": "orphan", "name": "孤兒", "tier": 2, "parentId": "nobody", "unlockLevel": 30, "proficiencyRequired": 3500, "statRequirements": [], "growth": {{ValidGrowth}} }] }""", "orphan" },
        { $$"""{ "jobs": [{{ValidWarrior}}, { "id": "rooted", "name": "有根", "tier": 1, "parentId": "warrior", "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [], "growth": {{ValidGrowth}} }] }""", "rooted" },
        { $$"""{ "jobs": [{ "id": "shrunk", "name": "縮水", "tier": 1, "parentId": null, "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [], "growth": {{ValidGrowth.Replace("\"str\": [2, 3]", "\"str\": [3, 2]")}} }] }""", "shrunk" },
        { $$"""{ "jobs": [{ "id": "gambler", "name": "賭徒", "tier": 1, "parentId": null, "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [{ "stat": "luck", "minimum": 10 }], "growth": {{ValidGrowth}} }] }""", "gambler" },
        { $$"""{ "jobs": [{{ValidWarrior}}, {{ValidWarrior}}] }""", "warrior" },
        { $$"""{ "jobs": [{ "id": "rootless", "name": "無根", "tier": 2, "parentId": null, "unlockLevel": 30, "proficiencyRequired": 3500, "statRequirements": [], "growth": {{ValidGrowth}} }] }""", "rootless" },
        { $$"""{ "jobs": [{ "id": "drained", "name": "枯竭", "tier": 1, "parentId": null, "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [], "growth": {{ValidGrowth.Replace("\"be\": [0, 1]", "\"be\": [-1, 1]")}} }] }""", "drained" },
    };

    [Fact]
    public void Load_ShippedContent_IsValid()
    {
        var content = JsonGameContent.Load(ShippedContentDirectory);
        Assert.Contains(content.AllMonsters, monster => monster.PlaceId == content.StartingPlaceId);
        Assert.Equal(12, content.AllJobs.Count);
        Assert.All(
            content.AllJobs.Where(job => job.Tier == 2),
            job => Assert.Equal(1, content.GetJob(job.ParentId!).Tier));
    }

    public static TheoryData<string> ShippedJobIds =>
        new(JsonGameContent.Load(ShippedContentDirectory).AllJobs.Select(job => job.Id));

    // spec §5.3：每級期望總成長（七項 (min+max)/2 之和）基礎 7～9、進階 9～12
    [Theory]
    [MemberData(nameof(ShippedJobIds))]
    public void JobGrowth_TotalsWithinSpecBands(string jobId)
    {
        var job = JsonGameContent.Load(ShippedContentDirectory).GetJob(jobId);
        var growth = job.Growth;
        var total = new[] { growth.Str, growth.Smart, growth.Life, growth.Vit, growth.Agi, growth.Au, growth.Be }
            .Sum(range => (range.Min + range.Max) / 2.0);

        var (minimum, maximum) = job.Tier == 1 ? (7.0, 9.0) : (9.0, 12.0);
        Assert.InRange(total, minimum, maximum);
    }

    // spec §5.2 的例子：騎士 體質 ≥ 60
    [Fact]
    public void Knight_RequiresVitSixty()
    {
        var knight = JsonGameContent.Load(ShippedContentDirectory).GetJob("knight");

        Assert.Equal([new StatRequirement("vit", 60)], knight.StatRequirements);
    }

    [Fact]
    public void Load_TestContent_ExposesJobs()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal(7, content.AllJobs.Count);
        var berserker = content.GetJob("berserker");
        Assert.Equal("warrior", berserker.ParentId);
        Assert.Equal(new StatRequirement("str", 60), berserker.StatRequirements[0]);
        Assert.Equal(new StatRange(3, 4), berserker.Growth.Str);
    }

    [Fact]
    public void Load_TestContent_ExposesMonsters()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal(["test-sprite", "test-wolf", "test-bat", "test-guardian", "test-wraith"], content.AllMonsters.Select(monster => monster.Id));
        Assert.Equal(["test-sprite", "test-wolf"], content.MonstersOf(1).Select(monster => monster.Id));
        // SubPlaces、Drops 是清單，record 的 Equals 只比參考，改用結構比對
        Assert.Equivalent(
            new MonsterDefinition("test-wolf", "測試寒狼", Level: 5, Hp: 95, Sp: 10, At: 20, Mat: 6, Df: 9, Mdf: 5, Agi: 13, Au: 7, Element: 2, JobExp: 18, Meet: 30, PlaceId: 1, SubPlaces: [1, 3], Drops: [new DropEntry("test-ore", 3)]),
            content.GetMonster("test-wolf"),
            strict: true);
        Assert.Equivalent(
            new MonsterDefinition("test-sprite", "測試雪精", Level: 3, Hp: 60, Sp: 10, At: 14, Mat: 8, Df: 6, Mdf: 4, Agi: 9, Au: 6, Element: 2, JobExp: 12, Meet: 0, PlaceId: 1, SubPlaces: [1, 2], Drops: [new DropEntry("test-supply", 1)]),
            content.GetMonster("test-sprite"),
            strict: true);
    }

    // 既有的雪精、寒狼改歸 16 冷峰寒地，依公式重算（雪精 Lv36 普通；寒狼 Lv40 速：agi ×1.4、at ×0.9、hp ×0.8）
    [Fact]
    public void Load_ShippedContent_DescribesFrostPeakMonsters()
    {
        var content = JsonGameContent.Load(ShippedContentDirectory);
        Assert.Equivalent(
            new MonsterDefinition("snow-sprite", "雪精", Level: 36, Hp: 1067, Sp: 82, At: 128, Mat: 76, Df: 41, Mdf: 34, Agi: 53, Au: 44, Element: 2, JobExp: 66, Meet: 0, PlaceId: 16, SubPlaces: [1, 2, 3, 4],
                Drops: [new DropEntry("medium-supply", 4), new DropEntry("iron-ore", 6)]),
            content.GetMonster("snow-sprite"),
            strict: true);
        Assert.Equivalent(
            new MonsterDefinition("frost-wolf", "寒狼", Level: 40, Hp: 996, Sp: 90, At: 127, Mat: 84, Df: 45, Mdf: 38, Agi: 81, Au: 48, Element: 2, JobExp: 72, Meet: 30, PlaceId: 16, SubPlaces: [2, 3],
                Drops: [new DropEntry("medium-supply", 5), new DropEntry("magic-dust", 7), new DropEntry("arcane-refine-stone-2", 175)]),
            content.GetMonster("frost-wolf"),
            strict: true);
    }

    [Fact]
    public void Load_Rejects_DuplicateMonsterId()
    {
        AssertLoadRejects($$"""{ "monsters": [{{ValidMonster}}, {{ValidMonster}}] }""", ValidJobs, "sprite");
    }

    [Theory]
    [MemberData(nameof(InvalidMonsterStats))]
    public void Load_Rejects_MonsterStatsOutOfRange(string validField, string invalidField)
    {
        Assert.Contains(validField, ValidMonster);
        var monstersJson = $$"""{ "monsters": [{{ValidMonster.Replace(validField, invalidField)}}] }""";
        AssertLoadRejects(monstersJson, ValidJobs, "sprite");
    }

    private static string MonsterAt(string placeId, string subPlaces) =>
        $$"""{ "monsters": [{{ValidMonster.Replace("\"meet\": 0", $"\"meet\": 0, \"placeId\": {placeId}, \"subPlaces\": {subPlaces}")}}] }""";

    [Theory]
    [InlineData("[0]")]
    [InlineData("[5]")]
    [InlineData("[1, 1]")]
    public void Monster_RejectsSubPlaceOutsideRange(string subPlaces) =>
        AssertLoadRejects(MonsterAt("1", subPlaces), ValidJobs, "sprite");

    private const string HerbItems =
        """{ "items": [{ "id": "herb", "name": "藥草", "category": "Material", "price": 1, "shopLevel": 1, "shopSellable": false }] }""";

    private static string MonsterDropping(string itemId, int topr) =>
        $$"""{ "monsters": [{{ValidMonster.Replace("\"meet\": 0", $"\"meet\": 0, \"drops\": [{{ \"itemId\": \"{itemId}\", \"topr\": {topr} }}]")}}] }""";

    [Fact]
    public void Monster_RejectsUnknownDropItem() =>
        AssertLoadRejects(MonsterDropping("ghost-item", 5), ValidJobs, "ghost-item", itemsJson: HerbItems);

    [Fact]
    public void Monster_RejectsDropWithToprBelowOne() =>
        AssertLoadRejects(MonsterDropping("herb", 0), ValidJobs, "topr", itemsJson: HerbItems);

    [Fact]
    public void Monster_RejectsUnknownPlace() =>
        AssertLoadRejects(MonsterAt("7", "[1]"), ValidJobs, "sprite");

    // ValidWorld 的地區 2 沒有子地區；placeId 0 為全域怪
    [Theory]
    [InlineData("2")]
    [InlineData("0")]
    public void Monster_RejectsSubPlacesForPlaceWithoutThem(string placeId) =>
        AssertLoadRejects(MonsterAt(placeId, "[1]"), ValidJobs, "sprite");

    [Fact]
    public void Monster_RequiresSubPlaceForPlaceWithThem() =>
        AssertLoadRejects(MonsterAt("1", "[]"), ValidJobs, "sprite");

    // ValidWorld 的地區 2 沒有任何怪物（也沒有全域怪）
    [Fact]
    public void Content_RejectsPlaceWithoutMonsters() =>
        AssertLoadRejects(MonsterAt("1", "[1]"), ValidJobs, "地區 2");

    [Fact]
    public void Monster_AcceptsPlaceAndSubPlaces()
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), MonsterAt("1", "[2, 4]").Replace(
                "}] }", "}, " + ValidMonster.Replace("\"sprite\"", "\"anywhere\"") + "] }"));
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), ValidItems);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            var monster = JsonGameContent.Load(directory.FullName).GetMonster("sprite");
            Assert.Equal(1, monster.PlaceId);
            Assert.Equal([2, 4], monster.SubPlaces);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidJobs))]
    public void Load_Rejects_InvalidJobs(string jobsJson, string offendingId)
    {
        AssertLoadRejects(ValidMonsters, jobsJson, offendingId);
    }

    private static string SkillJson(string id, string jobId, string type, string levels) =>
        $$"""{ "id": "{{id}}", "name": "技", "jobId": "{{jobId}}", "type": "{{type}}", "timing": "beforeAttack", "effect": "AtMultiplier", "description": "說明", "levels": [{{levels}}] }""";

    private static void AssertSkillsRejected(string skills, string offendingText) =>
        AssertLoadRejects(ValidMonsters, ValidJobs, offendingText, $$"""{ "skills": [{{skills}}] }""");

    [Fact]
    public void Skills_LoadSixteenWithFiveLevels()
    {
        var content = JsonGameContent.Load(ShippedContentDirectory);
        Assert.Equal(16, content.AllSkills.Count);
        Assert.Equal(16, content.AllSkills.Select(skill => skill.Id).Distinct().Count());
        foreach (var skill in content.AllSkills)
        {
            Assert.Equal(5, skill.MaxLevel);
            foreach (var level in skill.Levels)
            {
                var lv = level.Level;
                Assert.Equal(3 * lv + 2, level.Sp);
                Assert.Equal(500L * lv * lv, level.Money);
                Assert.Equal(300L * lv, level.Proficiency);
                Assert.Equal(skill.Type == SkillType.Passive ? 20 + 10 * lv : 0, level.Chance);
                var oneRound = skill.Effect is SkillEffectKind.Heal or SkillEffectKind.Bloodthirst or SkillEffectKind.BonusDamage;
                Assert.Equal(oneRound ? 1 : 3, level.Rounds);
            }
        }
    }

    [Fact]
    public void Skills_RejectUnknownJob() =>
        AssertSkillsRejected(SkillJson("ghost-skill", "nobody", "active", ValidLevel), "nobody");

    [Fact]
    public void Skills_RejectDuplicateId() =>
        AssertSkillsRejected(
            $"{SkillJson("twin", "warrior", "active", ValidLevel)}, {SkillJson("twin", "warrior", "active", ValidLevel)}",
            "twin");

    [Fact]
    public void Skills_RejectNonIncreasingLevels() =>
        AssertSkillsRejected(
            SkillJson("jumpy", "warrior", "active", $"{ValidLevel}, {ValidLevel.Replace("\"level\": 1", "\"level\": 3")}"),
            "jumpy");

    [Fact]
    public void Skills_RejectPassiveWithZeroChance() =>
        AssertSkillsRejected(SkillJson("lazy", "warrior", "passive", ValidLevel), "lazy");

    [Fact]
    public void Skills_RejectActiveWithNonZeroChance() =>
        AssertSkillsRejected(
            SkillJson("eager", "warrior", "active", ValidLevel.Replace("\"chance\": 0", "\"chance\": 50")),
            "eager");

    [Fact]
    public void Skills_RejectPassiveChanceAboveHundred()
    {
        var chance100 = ValidLevel.Replace("\"chance\": 0", "\"chance\": 100");
        var loaded = LoadSkills(SkillJson("sure", "warrior", "passive", chance100));
        Assert.Equal(100, loaded.GetSkill("sure").LevelAt(1).Chance);
        AssertSkillsRejected(
            SkillJson("overflow", "warrior", "passive", ValidLevel.Replace("\"chance\": 0", "\"chance\": 101")),
            "overflow");
    }

    [Fact]
    public void Skills_RejectFirstLevelNotOne() =>
        AssertSkillsRejected(
            SkillJson("late", "warrior", "active", ValidLevel.Replace("\"level\": 1", "\"level\": 2")),
            "late");

    [Fact]
    public void Skills_RejectZeroRounds() =>
        AssertSkillsRejected(
            SkillJson("instant", "warrior", "active", ValidLevel.Replace("\"rounds\": 3", "\"rounds\": 0")),
            "instant");

    [Fact]
    public void Skills_RejectUnknownEnumValue()
    {
        AssertSkillsRejected(SkillJson("bogus-type", "warrior", "bogus", ValidLevel), "bogus-type");
        AssertSkillsRejected(
            SkillJson("numeric-effect", "warrior", "active", ValidLevel).Replace("AtMultiplier", "99"),
            "numeric-effect");
    }

    [Fact]
    public void GetSkill_UnknownId_Throws()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        var exception = Assert.Throws<GameContentException>(() => content.GetSkill("nope"));
        Assert.Contains("nope", exception.Message);
    }

    private static JsonGameContent LoadSkills(string skills)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), $$"""{ "skills": [{{skills}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), ValidItems);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            return JsonGameContent.Load(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void SkillsOf_ReturnsOnlyThatJob()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal(["burst", "iron-skin"], content.SkillsOf("warrior").Select(skill => skill.Id));
        Assert.Equal("heal", content.GetSkill("heal").Id);
        Assert.Empty(content.SkillsOf("sorcerer"));
        Assert.Equal(["lullaby", "heal"], content.SkillsOf("bard").Select(skill => skill.Id));
    }

    [Fact]
    public void GetMonster_UnknownId_Throws()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        var exception = Assert.Throws<GameContentException>(() => content.GetMonster("nobody"));
        Assert.Contains("nobody", exception.Message);
    }

    private static void AssertWorldRejected(string world, string offendingText) =>
        AssertLoadRejects(ValidMonsters, ValidJobs, offendingText, ValidSkills, world);

    private static string PlaceJson(int id, string subPlaces = """["a", "b", "c", "d"]""", string recommended = "[1, 10]") =>
        $$"""{ "id": {{id}}, "name": "地{{id}}", "subPlaces": {{subPlaces}}, "recommended": {{recommended}}, "restricted": false }""";

    private static string WorldJson(string areaPlaceIds, string places, string starting = "\"startingPlaceId\": 1, \"startingSubPlace\": 1") =>
        $$"""{ {{starting}}, "areas": [{ "id": "central", "name": "中", "placeIds": [{{areaPlaceIds}}] }], "places": [{{places}}] }""";

    [Fact]
    public void World_LoadsTwentyOnePlacesInFiveAreas()
    {
        var content = JsonGameContent.Load(ShippedContentDirectory);

        Assert.Equal(21, content.AllPlaces.Count);
        Assert.Equal(Enumerable.Range(1, 21), content.AllPlaces.Select(place => place.Id).Order());
        Assert.Equal(["central", "magic", "jungle", "doom", "restricted"], content.AllAreas.Select(area => area.Id));
        Assert.Equal([1, 3, 7, 13], content.AllAreas[0].PlaceIds);
        Assert.Equal([2, 8, 12, 15], content.AllAreas[1].PlaceIds);
        Assert.Equal([4, 11, 16, 9], content.AllAreas[2].PlaceIds);
        Assert.Equal([10, 5, 6, 14, 21], content.AllAreas[3].PlaceIds);
        Assert.Equal([17, 18, 19, 20], content.AllAreas[4].PlaceIds);
        Assert.Equal(1, content.StartingPlaceId);
        Assert.Equal(1, content.StartingSubPlace);

        Assert.All(content.AllPlaces, place =>
        {
            Assert.Equal(place.Id != 20 ? 4 : 0, place.SubPlaces.Count);
            Assert.Equal(place.Id is >= 17 and <= 20, place.Restricted);
            Assert.InRange(place.Recommended.Min, 1, place.Recommended.Max);
            Assert.Equal(content.AllAreas.Single(area => area.PlaceIds.Contains(place.Id)).Id, place.AreaId);
        });
        Assert.False(content.GetPlace(20).HasSubPlaces);

        Assert.Equal(new LevelRange(1, 10), content.GetPlace(1).Recommended);
        Assert.Equal(new LevelRange(20, 32), content.GetPlace(7).Recommended);
        Assert.Equal(new LevelRange(26, 40), content.GetPlace(12).Recommended);
        Assert.Equal(new LevelRange(35, 48), content.GetPlace(16).Recommended);
        Assert.Equal(new LevelRange(110, 140), content.GetPlace(21).Recommended);
        Assert.Equal(new LevelRange(130, 160), content.GetPlace(20).Recommended);

        Assert.Equal("中央平原", content.GetPlace(1).Name);
        Assert.Equal(["香茅綠地", "綠風草地", "白沙海灣", "眠獸洞穴"], content.GetPlace(1).SubPlaces.Select(sub => sub.Name));
        Assert.Equal([1, 2, 3, 4], content.GetPlace(1).SubPlaces.Select(sub => sub.Index));
        Assert.Equal(["試煉之道", "邪靈棧橋", "中央高塔", "熔岩斷層"], content.GetPlace(7).SubPlaces.Select(sub => sub.Name));
        Assert.Equal(["雲端城堡", "輝煌都市", "天使之廳", "封神殿堂"], content.GetPlace(12).SubPlaces.Select(sub => sub.Name));
        Assert.Equal(["寒氣苔原", "覆雪大地", "暴風雪山", "極寒冰峰"], content.GetPlace(16).SubPlaces.Select(sub => sub.Name));
        Assert.Equal(["十字墓地", "惡魔遺蹟", "暗紅神殿", "焦池煉獄"], content.GetPlace(21).SubPlaces.Select(sub => sub.Name));
    }

    [Fact]
    public void GetArea_ReturnsAreaOrThrows()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal("測試大陸", content.GetArea("central").Name);
        Assert.Contains("nowhere", Assert.Throws<GameContentException>(() => content.GetArea("nowhere")).Message);
    }

    [Fact]
    public void World_TestContent_ExposesPlaces()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal([1, 2, 20, 17], content.AllPlaces.Select(place => place.Id));
        Assert.True(content.GetPlace(17).Restricted);
        Assert.Contains("999", Assert.Throws<GameContentException>(() => content.GetPlace(999)).Message);
    }

    [Fact]
    public void World_RejectsUnknownPlaceInArea() =>
        AssertWorldRejected(WorldJson("1, 7", PlaceJson(1)), "不存在的地區：7");

    [Fact]
    public void World_RejectsPlaceOutsideAnyArea() =>
        AssertWorldRejected(WorldJson("1", $"{PlaceJson(1)}, {PlaceJson(2)}"), "地區 2 不屬於任何區域");

    [Fact]
    public void World_RejectsDuplicatePlaceId() =>
        AssertWorldRejected(WorldJson("1", $"{PlaceJson(1)}, {PlaceJson(1)}"), "重複");

    [Theory]
    [InlineData("""["a", "b", "c"]""")]
    [InlineData("""["a", "b", "c", "d", "e"]""")]
    public void World_RejectsSubPlaceCountOtherThanZeroOrFour(string subPlaces) =>
        AssertWorldRejected(WorldJson("1", PlaceJson(1, subPlaces)), "子地區");

    [Fact]
    public void World_RejectsStartingPlaceWithoutSubPlaceWhenItHasThem() =>
        AssertWorldRejected(
            WorldJson("1", PlaceJson(1), "\"startingPlaceId\": 1, \"startingSubPlace\": null"), "起始");

    [Fact]
    public void World_RejectsStartingSubPlaceWhenPlaceHasNone() =>
        AssertWorldRejected(
            WorldJson("1", PlaceJson(1, "[]"), "\"startingPlaceId\": 1, \"startingSubPlace\": 1"), "起始");

    [Fact]
    public void World_RejectsMissingStartingPlace() =>
        AssertWorldRejected(
            WorldJson("1", PlaceJson(1), "\"startingPlaceId\": 9, \"startingSubPlace\": 1"), "起始");

    [Theory]
    [InlineData("[0, 10]")]
    [InlineData("[10, 5]")]
    public void World_RejectsBadRecommendedRange(string recommended) =>
        AssertWorldRejected(WorldJson("1", PlaceJson(1, recommended: recommended)), "推薦");

    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    public void World_RejectsPlaceIdOutsideOneToTwentyOne(int id) =>
        AssertWorldRejected(WorldJson($"{id}", PlaceJson(id), $"\"startingPlaceId\": {id}, \"startingSubPlace\": 1"), "1～21");

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void World_RejectsStartingSubPlaceOutsideRange(int subPlace) =>
        AssertWorldRejected(
            WorldJson("1", PlaceJson(1), $"\"startingPlaceId\": 1, \"startingSubPlace\": {subPlace}"), "起始");

    [Fact]
    public void World_RejectsPlaceInTwoAreas() =>
        AssertWorldRejected(
            """
            { "startingPlaceId": 1, "startingSubPlace": 1,
              "areas": [{ "id": "a", "name": "甲", "placeIds": [1] }, { "id": "b", "name": "乙", "placeIds": [1] }],
              "places": [{ "id": 1, "name": "地", "subPlaces": ["a", "b", "c", "d"], "recommended": [1, 10], "restricted": false }] }
            """,
            "同時屬於多個區域");

    [Fact]
    public void World_AcceptsEqualRecommendedRange()
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(
                Path.Combine(directory.FullName, "world.json"), WorldJson("1", PlaceJson(1, recommended: "[5, 5]")));
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), ValidItems);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            Assert.Equal(new LevelRange(5, 5), JsonGameContent.Load(directory.FullName).GetPlace(1).Recommended);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static readonly string TestContentDirectory = Path.Combine(AppContext.BaseDirectory, "TestContent");

    [Fact]
    public void Load_TestContent_ExposesItemsAndRecipes()
    {
        var content = JsonGameContent.Load(TestContentDirectory);
        Assert.Equal(
            [
                "test-sword", "test-helm", "test-supply", "test-refine-stone", "test-stone", "test-ore",
                "test-axe", "test-heavy-sword", "test-power-ring", "test-str-potion", "test-stamina-potion",
                "test-bag-expansion", "test-depot-expansion", "test-life-amulet", "test-bound-sword",
            ],
            content.AllItems.Select(item => item.Id));

        var sword = content.GetItem("test-sword");
        Assert.Equal(ItemCategory.Weapon, sword.Category);
        Assert.Equal(ConsumableKind.None, sword.Consumable);
        Assert.Equal(StatBonus.Zero with { At = 8 }, sword.Bonus);
        Assert.Equal(new StatRequirement7(10, 0, 0, 0), sword.Requires);
        Assert.Equal((1, 4, 200L, 1, true), (sword.Holes, sword.RefineMax, sword.Price, sword.ShopLevel, sword.ShopSellable));
        Assert.True(sword.IsEquipment);
        Assert.False(sword.Stackable);

        var supply = content.GetItem("test-supply");
        Assert.Equal((ConsumableKind.Supply, 50, 25), (supply.Consumable, supply.SupplyHp, supply.SupplySp));
        Assert.True(supply.Stackable);

        var refine = content.GetItem("test-refine-stone").Refine!;
        Assert.Equal((1, 1), (refine.Level, refine.Kind));
        Assert.Equal(new StatRange(1, 2), refine.At);
        Assert.Null(refine.Mat);

        var stone = content.GetItem("test-stone").Stone!;
        Assert.False(stone.Random);
        Assert.Equal(StatBonus.Zero with { At = 5 }, stone.Fixed);

        var recipe = Assert.Single(content.AllRecipes);
        Assert.Same(recipe, content.GetRecipe("test-forge-sword"));
        Assert.Equal(("test-sword", 1, 70), (recipe.ResultItemId, recipe.ResultQuantity, recipe.SuccessRate));
        Assert.Equal([("test-ore", 3)], recipe.Materials);
    }

    [Fact]
    public void Content_ItemsForShop_FiltersByCategoryAndLevel()
    {
        var content = JsonGameContent.Load(TestContentDirectory);
        Assert.Equal(["test-sword"], content.ItemsForShop(ItemCategory.Weapon, 1).Select(item => item.Id));
        Assert.Equal(["test-helm"], content.ItemsForShop(ItemCategory.Head, 1).Select(item => item.Id));
        Assert.Equal(["test-supply"], content.ItemsForShop(ItemCategory.Consumable, 1).Select(item => item.Id));
        Assert.Empty(content.ItemsForShop(ItemCategory.Weapon, 2));
        // 不在商店販賣的品項不出現
        Assert.Empty(content.ItemsForShop(ItemCategory.RefineStone, 1));
        Assert.Empty(content.ItemsForShop(ItemCategory.Material, 1));
    }

    [Fact]
    public void GetItem_UnknownThrows()
    {
        var content = JsonGameContent.Load(TestContentDirectory);
        Assert.Contains("ghost-item", Assert.Throws<GameContentException>(() => content.GetItem("ghost-item")).Message);
        Assert.Contains("ghost-recipe", Assert.Throws<GameContentException>(() => content.GetRecipe("ghost-recipe")).Message);
    }

    private const string ValidItem =
        """{ "id": "blade", "name": "刃", "category": "Weapon", "bonus": { "at": 8 }, "requires": { "str": 10 }, "holes": 1, "refineMax": 4, "price": 200, "shopLevel": 1, "shopSellable": true }""";

    private const string ValidOre =
        """{ "id": "ore", "name": "礦", "category": "Material", "price": 10, "shopLevel": 1, "shopSellable": false }""";

    private const string ValidSupply =
        """{ "id": "tonic", "name": "藥", "category": "Consumable", "consumable": "Supply", "supplyHp": 50, "supplySp": 25, "price": 20, "shopLevel": 1, "shopSellable": true }""";

    private const string ValidRefineStone =
        """{ "id": "whetstone", "name": "石", "category": "RefineStone", "price": 100, "shopLevel": 1, "shopSellable": false, "refine": { "level": 1, "kind": 1, "at": [1, 2] } }""";

    private const string ValidStone =
        """{ "id": "gem", "name": "晶", "category": "Stone", "price": 2000, "shopLevel": 1, "shopSellable": false, "stone": { "random": false, "fixed": { "at": 5 } } }""";

    private const string ValidRecipe =
        """{ "id": "forge", "resultItemId": "blade", "resultQuantity": 1, "materials": [{ "itemId": "ore", "quantity": 3 }], "successRate": 70 }""";

    private const string ValidItemList = $"{ValidItem}, {ValidOre}, {ValidSupply}, {ValidRefineStone}, {ValidStone}";

    // 回傳載入失敗的訊息；其他內容檔都用合法值
    private static string ItemRejection(string items, string recipes = ValidRecipe)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), $$"""{ "items": [{{items}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), $$"""{ "recipes": [{{recipes}}] }""");
            return Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName)).Message;
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string Replace(string json, string validField, string invalidField)
    {
        Assert.Contains(validField, json);
        return json.Replace(validField, invalidField);
    }

    [Fact]
    public void Items_AcceptValidSample()
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), $$"""{ "items": [{{ValidItemList}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), $$"""{ "recipes": [{{ValidRecipe}}] }""");
            Assert.Equal(5, JsonGameContent.Load(directory.FullName).AllItems.Count);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Items_RejectDuplicateId()
    {
        var message = ItemRejection($"{ValidItemList}, {ValidItem}");
        Assert.Contains("道具 id 重複：blade", message);
    }

    [Theory]
    [InlineData("\"supplyHp\": 50")]
    [InlineData("\"supplySp\": 25")]
    [InlineData("\"amount\": 5")]
    [InlineData("\"consumable\": \"Supply\"")]
    public void Items_RejectEquipmentWithSupply(string field)
    {
        var message = ItemRejection(
            $"{Replace(ValidItem, "\"holes\": 1", $"{field}, \"holes\": 1")}, {ValidOre}, {ValidSupply}");
        Assert.Contains("blade", message);
        Assert.Contains("裝備", message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void Items_RejectShopLevelOutsideRange(int shopLevel)
    {
        var message = ItemRejection($"{Replace(ValidItem, "\"shopLevel\": 1", $"\"shopLevel\": {shopLevel}")}, {ValidOre}");
        Assert.Contains("blade", message);
        Assert.Contains("shopLevel", message);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-1)]
    public void Items_RejectHolesAboveFour(int holes)
    {
        var message = ItemRejection($"{Replace(ValidItem, "\"holes\": 1", $"\"holes\": {holes}")}, {ValidOre}");
        Assert.Contains("blade", message);
        Assert.Contains("holes", message);
    }

    [Theory]
    [InlineData("\"resultItemId\": \"blade\"", "\"resultItemId\": \"phantom\"")]
    [InlineData("\"itemId\": \"ore\"", "\"itemId\": \"phantom\"")]
    public void Recipes_RejectUnknownItem(string validField, string invalidField)
    {
        var message = ItemRejection(ValidItemList, Replace(ValidRecipe, validField, invalidField));
        Assert.Contains("forge", message);
        Assert.Contains("phantom", message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Recipes_RejectRateOutsideRange(int rate)
    {
        var message = ItemRejection(ValidItemList, Replace(ValidRecipe, "\"successRate\": 70", $"\"successRate\": {rate}"));
        Assert.Contains("forge", message);
        Assert.Contains("successRate", message);
    }

    // 其餘欄位規則：(要改的道具、原欄位、改成、訊息應含的字)
    public static TheoryData<string, string, string, string> InvalidItemFields => new()
    {
        { ValidItem, "\"category\": \"Weapon\"", "\"category\": \"Spoon\"", "category" },
        { ValidItem, "\"refineMax\": 4", "\"refineMax\": -1", "refineMax" },
        { ValidItem, "\"price\": 200", "\"price\": -1", "price" },
        { ValidItem, "\"holes\": 1", "\"element\": 7, \"holes\": 1", "element" },
        { ValidItem, "\"holes\": 1", "\"jobId\": \"nobody\", \"holes\": 1", "nobody" },
        { ValidItem, "\"holes\": 1", "\"refine\": { \"level\": 1, \"kind\": 1, \"at\": [1, 2] }, \"holes\": 1", "裝備" },
        { ValidItem, "\"holes\": 1", "\"stone\": { \"random\": false, \"fixed\": { \"at\": 5 } }, \"holes\": 1", "裝備" },
        { ValidSupply, "\"price\": 20", "\"requires\": { \"str\": 10 }, \"price\": 20", "需求" },
        { ValidSupply, "\"price\": 20", "\"bonus\": { \"at\": 5 }, \"price\": 20", "bonus" },
        { ValidSupply, "\"price\": 20", "\"holes\": 1, \"price\": 20", "holes" },
        { ValidSupply, "\"price\": 20", "\"refineMax\": 3, \"price\": 20", "refineMax" },
        { ValidSupply, "\"consumable\": \"Supply\"", "\"consumable\": \"None\"", "consumable" },
        { ValidSupply, "\"supplyHp\": 50, \"supplySp\": 25", "\"supplyHp\": 0, \"supplySp\": 0", "supply" },
        { ValidSupply, "\"consumable\": \"Supply\"", "\"consumable\": \"StatPotion\"", "bonus" },
        { ValidSupply, "\"consumable\": \"Supply\"", "\"consumable\": \"StaminaPotion\"", "amount" },
        { ValidOre, "\"price\": 10", "\"consumable\": \"Supply\", \"supplyHp\": 50, \"price\": 10", "consumable" },
        { ValidOre, "\"price\": 10", "\"refine\": { \"level\": 1, \"kind\": 1, \"at\": [1, 2] }, \"price\": 10", "refine" },
        { ValidOre, "\"price\": 10", "\"stone\": { \"random\": false, \"fixed\": { \"at\": 5 } }, \"price\": 10", "stone" },
        { ValidRefineStone, "\"level\": 1", "\"level\": 0", "level" },
        { ValidRefineStone, "\"level\": 1", "\"level\": 11", "level" },
        { ValidRefineStone, "\"kind\": 1", "\"kind\": 0", "kind" },
        { ValidRefineStone, "\"kind\": 1", "\"kind\": 19", "kind" },
        { ValidRefineStone, "\"at\": [1, 2]", "\"at\": [3, 2]", "範圍" },
        { ValidRefineStone, "\"at\": [1, 2]", "\"at\": [-1, 2]", "範圍" },
        { ValidRefineStone, ", \"refine\": { \"level\": 1, \"kind\": 1, \"at\": [1, 2] }", "", "refine" },
        { ValidStone, ", \"stone\": { \"random\": false, \"fixed\": { \"at\": 5 } }", "", "stone" },
        { ValidStone, "\"fixed\": { \"at\": 5 }", "\"fixed\": {}", "fixed" },
        { ValidStone, "\"random\": false, \"fixed\": { \"at\": 5 }", "\"random\": true, \"min\": { \"at\": 1 }", "min" },
        { ValidStone, "\"random\": false, \"fixed\": { \"at\": 5 }", "\"random\": true, \"min\": { \"at\": 6 }, \"max\": { \"at\": 5 }", "min" },
        { ValidStone, "\"fixed\": { \"at\": 5 }", "\"fixed\": { \"at\": 5 }, \"min\": { \"at\": 1 }", "min" },
        { ValidStone, "\"random\": false", "\"random\": true, \"min\": { \"at\": 1 }, \"max\": { \"at\": 5 }", "fixed" },
        { ValidRefineStone, ", \"at\": [1, 2]", "", "至少" },
        { ValidOre, "\"price\": 10", "\"supplyHp\": 50, \"price\": 10", "supplyHp" },
        { ValidSupply, "\"price\": 20", "\"amount\": 5, \"price\": 20", "amount" },
        { ValidOre, "\"price\": 10", "\"jobId\": \"warrior\", \"price\": 10", "jobId" },
    };

    [Theory]
    [MemberData(nameof(InvalidItemFields))]
    public void Items_RejectInvalidFields(string item, string validField, string invalidField, string expected)
    {
        var id = item[(item.IndexOf("\"id\": \"", StringComparison.Ordinal) + 7)..].Split('"')[0];
        var others = new[] { ValidItem, ValidOre, ValidSupply, ValidRefineStone, ValidStone }.Where(other => other != item);
        var message = ItemRejection(string.Join(", ", others.Prepend(Replace(item, validField, invalidField))));
        Assert.Contains(id, message);
        Assert.Contains(expected, message);
    }

    [Theory]
    [InlineData("\"resultQuantity\": 1", "\"resultQuantity\": 0", "resultQuantity")]
    [InlineData("\"quantity\": 3", "\"quantity\": 0", "quantity")]
    [InlineData("\"materials\": [{ \"itemId\": \"ore\", \"quantity\": 3 }]", "\"materials\": []", "materials")]
    public void Recipes_RejectInvalidFields(string validField, string invalidField, string expected)
    {
        var message = ItemRejection(ValidItemList, Replace(ValidRecipe, validField, invalidField));
        Assert.Contains("forge", message);
        Assert.Contains(expected, message);
    }

    [Fact]
    public void Recipes_RejectDuplicateId() =>
        Assert.Contains("配方 id 重複：forge", ItemRejection(ValidItemList, $"{ValidRecipe}, {ValidRecipe}"));

    private static void AssertLoadRejects(
        string monstersJson,
        string jobsJson,
        string offendingId,
        string skillsJson = ValidSkills,
        string worldJson = ValidWorld,
        string itemsJson = ValidItems,
        string recipesJson = ValidRecipes)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), monstersJson);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), jobsJson);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), skillsJson);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), worldJson);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), itemsJson);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), recipesJson);
            var exception = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName));
            Assert.Contains(offendingId, exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
