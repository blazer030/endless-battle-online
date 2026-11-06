using System.Globalization;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class JsonGameContentTests
{
    private static readonly string ShippedContentDirectory = Path.Combine(BackendPaths.BackendDirectory, "content");

    private const string ValidMonster =
        """{ "id": "sprite", "name": "雪精", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0, "placeId": 0 }""";

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

    private const string ValidMissions = """{ "missions": [] }""";

    private const string ValidMercenaries = """{ "mercenaries": [] }""";

    private const string ValidHonourShop = """{ "exchanges": [] }""";

    private const string ValidInn =
        """{ "seconds": 28800, "pricePerLevel": 50, "rooms": [{ "id": "loft", "name": "閣樓", "kind": "Exp", "multiplier": 1.10, "priceRate": 2 }] }""";

    // 主動技能的一級；被動常駐，sp／rounds／chance 都是 0
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
        // 原版 12 個職業：一轉 4 個、二轉 8 個
        Assert.Equal(12, content.AllJobs.Count);
        Assert.All(content.AllJobs, job => Assert.False(CustomContent.IsCustom(job.Id)));
        Assert.All(
            content.AllJobs.Where(job => job.Tier == 2),
            job => Assert.Equal(1, content.GetJob(job.ParentId!).Tier));
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
        Assert.Equal(
            ["test-sprite", "test-wolf", "test-bat", "test-guardian", "test-wraith", "test-sprite-chief"],
            content.AllMonsters.Select(monster => monster.Id));
        Assert.Equal(["test-sprite", "test-wolf", "test-sprite-chief"], content.MonstersOf(1).Select(monster => monster.Id));
        Assert.Equal("test-chief-hunt", content.GetMonster("test-sprite-chief").MissionId);
        Assert.Null(content.GetMonster("test-sprite").MissionId);
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
        $$"""{ "monsters": [{{ValidMonster.Replace("\"placeId\": 0", $"\"placeId\": {placeId}, \"subPlaces\": {subPlaces}")}}] }""";

    // 漏寫 placeId 不能被當成全域怪：全域怪必須明寫 "placeId": 0
    [Fact]
    public void Monsters_RejectMissingPlaceId()
    {
        var monsters = $$"""{ "monsters": [{{ValidMonster.Replace(", \"placeId\": 0", "")}}] }""";
        Assert.DoesNotContain("placeId", monsters);
        AssertLoadRejects(monsters, ValidJobs, "sprite");
        AssertLoadRejects(monsters, ValidJobs, "placeId");
    }

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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
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

    private static void AssertSkillsRejected(string skills, string offendingText) =>
        AssertLoadRejects(ValidMonsters, ValidJobs, offendingText, $$"""{ "skills": [{{skills}}] }""");

    // 新格式：每一級由發動條件、消耗、效果組成
    private static string ComposedLevel(int level, string conditions, string effects, string costs = """[{ "kind": "SkillPoints", "amount": 2 }]""") =>
        $$"""{ "level": {{level}}, "money": 500, "proficiency": 300, "cooldown": 0, "conditions": {{conditions}}, "costs": {{costs}}, "effects": {{effects}} }""";

    private const string ChanceForty = """[{ "kind": "Chance", "percent": 40 }]""";

    private const string AttackUp = """[{ "kind": "StatChange", "target": "Caster", "stat": "attack", "percent": 1, "rounds": 3 }]""";

    private static string ComposedSkillJson(string id, int maxLevel, params string[] levels) =>
        $$"""{ "id": "{{id}}", "name": "技", "slotType": "Active", "timing": "BeforeAttack", "description": "說明", "maxLevel": {{maxLevel}}, "levels": [{{string.Join(", ", levels)}}] }""";

    [Fact]
    public void Skill_ComposedLevelsLoad()
    {
        var skill = LoadSkills(ComposedSkillJson("burst", 2, ComposedLevel(1, ChanceForty, AttackUp), ComposedLevel(2, ChanceForty, AttackUp)))
            .GetSkill("burst");

        Assert.Equal(2, skill.MaxLevel);
        Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 40)], skill.LevelAt(2).Conditions!);
        Assert.Equal([new SkillCost(SkillCostKind.SkillPoints, 2)], skill.LevelAt(2).Costs!);
        Assert.Equal(new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, 1, null, null, 3), Assert.Single(skill.LevelAt(1).Effects!));
    }

    [Fact]
    public void Skill_MissingEffectValueIsRejected() =>
        AssertSkillsRejected(
            ComposedSkillJson("burst", 1, ComposedLevel(1, ChanceForty, """[{ "kind": "StatChange", "target": "Caster", "stat": "attack", "rounds": 3 }]""")),
            "burst 第 1 級");

    [Fact]
    public void Skill_LevelsWithDifferentKindsAreRejected() =>
        AssertSkillsRejected(
            ComposedSkillJson("burst", 2,
                ComposedLevel(1, ChanceForty, AttackUp),
                ComposedLevel(2, ChanceForty, """[{ "kind": "Stun", "chance": 10, "rounds": 1 }]""")),
            "burst 第 2 級");

    [Fact]
    public void Skill_LevelsMustMatchMaxLevel() =>
        AssertSkillsRejected(ComposedSkillJson("burst", 2, ComposedLevel(1, ChanceForty, AttackUp)), "burst");

    // 前置技能要存在、等級在 1～前置技能的 maxLevel
    [Theory]
    [InlineData("ghost", 1, "advanced 的前置技能 ghost 第 1 級不存在")]
    [InlineData("burst", 2, "advanced 的前置技能 burst 第 2 級不存在")]
    public void Skill_MissingPrerequisiteIsRejected(string prerequisiteId, int level, string message)
    {
        var burst = ComposedSkillJson("burst", 1, ComposedLevel(1, ChanceForty, AttackUp));
        var advanced = ComposedSkillJson("advanced", 1, ComposedLevel(1, ChanceForty, AttackUp))
            .Replace("\"maxLevel\": 1,", "\"maxLevel\": 1, \"prerequisite\": { \"skillId\": \"" + prerequisiteId + "\", \"level\": " + level + " },");

        AssertSkillsRejected($"{burst}, {advanced}", message);
    }

    // 互為前置的技能永遠學不到
    [Fact]
    public void Skill_PrerequisiteCycleIsRejected()
    {
        string Requiring(string id, string prerequisiteId) =>
            ComposedSkillJson(id, 1, ComposedLevel(1, ChanceForty, AttackUp))
                .Replace("\"maxLevel\": 1,", "\"maxLevel\": 1, \"prerequisite\": { \"skillId\": \"" + prerequisiteId + "\", \"level\": 1 },");

        AssertSkillsRejected($"{Requiring("burst", "advanced")}, {Requiring("advanced", "burst")}", "技能 burst 的前置鏈有循環");
    }

    [Fact]
    public void Skill_AlwaysOnLevelWithCostIsRejected() =>
        AssertSkillsRejected(
            ComposedSkillJson("burst", 1, ComposedLevel(1, "[]", """[{ "kind": "StatChange", "target": "Caster", "stat": "defense", "percent": 1, "rounds": 0 }]""",
                costs: """[{ "kind": "SkillPoints", "amount": 3 }]""")),
            "burst 第 1 級");

    // 以 TestContent 為底，在指定道具加上附帶技能
    private static JsonGameContent LoadWithWeaponSkill(string itemId, string weaponSkill)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            foreach (var file in Directory.GetFiles(TestContentDirectory))
            {
                File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
            }

            var path = Path.Combine(directory.FullName, "items.json");
            var marker = $$"""{"id": "{{itemId}}", """;
            File.WriteAllText(path, File.ReadAllText(path).Replace(marker, $$"""{{marker}}"weaponSkill": {{weaponSkill}}, """));
            return JsonGameContent.Load(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_WeaponSkill_IsOnItem()
    {
        var content = LoadWithWeaponSkill("test-sword", """{ "skillId": "burst", "level": 2, "rate": 12 }""");

        Assert.Equal(new WeaponSkillSpec("burst", 2, 12), content.GetItem("test-sword").WeaponSkill);
        Assert.Null(content.GetItem("test-axe").WeaponSkill);
    }

    [Theory]
    [InlineData("test-helm", """{ "skillId": "burst", "level": 1, "rate": 12 }""")]
    [InlineData("test-sword", """{ "skillId": "skill-x", "level": 1, "rate": 12 }""")]
    [InlineData("test-sword", """{ "skillId": "burst", "level": 0, "rate": 12 }""")]
    [InlineData("test-sword", """{ "skillId": "burst", "level": 99, "rate": 12 }""")]
    [InlineData("test-sword", """{ "skillId": "burst", "level": 1, "rate": 0 }""")]
    [InlineData("test-sword", """{ "skillId": "burst", "level": 1, "rate": 101 }""")]
    public void Load_InvalidWeaponSkill_Throws(string itemId, string weaponSkill)
    {
        var exception = Assert.Throws<GameContentException>(() => LoadWithWeaponSkill(itemId, weaponSkill));

        Assert.Contains(itemId, exception.Message);
    }

    private static JsonGameContent LoadWithMonsterSkills(string? rows)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            foreach (var file in Directory.GetFiles(TestContentDirectory))
            {
                File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
            }

            var path = Path.Combine(directory.FullName, "monster-skills.json");
            if (rows is null)
            {
                File.Delete(path);
            }
            else
            {
                File.WriteAllText(path, $$"""{ "monsterSkills": [{{rows}}] }""");
            }

            return JsonGameContent.Load(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_MonsterSkillRow_IsOnItsMonster()
    {
        var content = LoadWithMonsterSkills("""
            { "monsterId": "test-sprite", "skillId": "burst", "level": 2, "rate": 10 },
            { "monsterId": "test-sprite", "skillId": "combo", "level": 1, "rate": 20 }
            """);

        var skills = content.GetMonster("test-sprite").Skills;

        Assert.Equal([("burst", 2, 1), ("combo", 1, 2)], skills.Select(skill => (skill.Skill.Id, skill.Level, skill.Slot)));
        Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 10)],
            skills[0].Skill.LevelAt(2).Conditions.Where(condition => condition.Kind == SkillConditionKind.Chance));
        Assert.Empty(content.GetMonster("test-wolf").Skills);
    }

    [Theory]
    [InlineData("""{ "monsterId": "monster-x", "skillId": "burst", "level": 1, "rate": 10 }""", "monster-x")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "skill-x", "level": 1, "rate": 10 }""", "skill-x")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "burst", "level": 0, "rate": 10 }""", "test-sprite")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "burst", "level": 99, "rate": 10 }""", "test-sprite")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "burst", "level": 1, "rate": 0 }""", "test-sprite")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "burst", "level": 1, "rate": 101 }""", "test-sprite")]
    [InlineData("""{ "monsterId": "test-sprite", "skillId": "burst", "level": 1, "rate": 10 }, { "monsterId": "test-sprite", "skillId": "burst", "level": 2, "rate": 20 }""", "重複")]
    public void Load_InvalidMonsterSkill_Throws(string rows, string offendingText)
    {
        var exception = Assert.Throws<GameContentException>(() => LoadWithMonsterSkills(rows));

        Assert.Contains(offendingText, exception.Message);
    }

    [Fact]
    public void Load_WithoutMonsterSkillsFile_Throws()
    {
        var exception = Assert.Throws<GameContentException>(() => LoadWithMonsterSkills(null));

        Assert.Contains("monster-skills.json", exception.Message);
    }

    // 以 TestContent 為底，換掉職業與技能的關聯表
    private static void AssertJobSkillsRejected(string jobSkills, string offendingText)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            foreach (var file in Directory.GetFiles(TestContentDirectory))
            {
                File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
            }

            File.WriteAllText(Path.Combine(directory.FullName, "job-skills.json"), $$"""{ "jobSkills": [{{jobSkills}}] }""");
            var exception = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName));
            Assert.Contains(offendingText, exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void JobSkill_MissingSkillIsRejected() =>
        AssertJobSkillsRejected("""{ "jobId": "warrior", "skillId": "ghost-skill" }""", "warrior＋ghost-skill");

    [Fact]
    public void JobSkill_DuplicatePairIsRejected() =>
        AssertJobSkillsRejected(
            """{ "jobId": "warrior", "skillId": "burst" }, { "jobId": "warrior", "skillId": "burst" }""", "warrior＋burst");

    [Fact]
    public void JobSkills_DecideWhichJobTeachesASkill()
    {
        var content = JsonGameContent.Load(TestContentDirectory);

        Assert.True(content.Teaches("warrior", "burst"));
        Assert.False(content.Teaches("thief", "burst"));
        Assert.Equal(new SkillPrerequisite("burst", 3), content.GetSkill("advanced-burst").Prerequisite);
    }

    [Fact]
    public void Skills_RejectDuplicateId()
    {
        var twin = ComposedSkillJson("twin", 1, ComposedLevel(1, ChanceForty, AttackUp));
        AssertSkillsRejected($"{twin}, {twin}", "twin");
    }

    [Fact]
    public void Skills_LoadHitPointsBelowCondition()
    {
        var skill = LoadSkills(ComposedSkillJson("rescue", 1, ComposedLevel(1, """[{ "kind": "HitPointsBelow", "percent": 50 }]""", AttackUp)))
            .GetSkill("rescue");

        Assert.Equal([new SkillCondition(SkillConditionKind.HitPointsBelow, 50)], skill.LevelAt(1).Conditions);
    }

    // HitPointsBelow 1～99、Chance 1～100
    [Theory]
    [InlineData("HitPointsBelow", 0)]
    [InlineData("HitPointsBelow", 100)]
    [InlineData("Chance", 0)]
    [InlineData("Chance", 101)]
    public void Skills_RejectConditionPercentOutOfRange(string kind, int percent) =>
        AssertSkillsRejected(
            ComposedSkillJson("edge", 1, ComposedLevel(1, $$"""[{ "kind": "{{kind}}", "percent": {{percent}} }]""", AttackUp)), "edge 第 1 級");

    [Fact]
    public void Skills_RejectUnknownConditionKind() =>
        AssertSkillsRejected(ComposedSkillJson("odd", 1, ComposedLevel(1, """[{ "kind": "Moonlight", "percent": 50 }]""", AttackUp)), "odd 第 1 級");

    [Fact]
    public void Skills_RejectNegativeCooldown()
    {
        var patient = ComposedLevel(1, ChanceForty, AttackUp).Replace("\"cooldown\": 0", "\"cooldown\": 4");
        Assert.Equal(4, LoadSkills(ComposedSkillJson("patient", 1, patient)).GetSkill("patient").LevelAt(1).Cooldown);
        AssertSkillsRejected(
            ComposedSkillJson("hasty", 1, ComposedLevel(1, ChanceForty, AttackUp).Replace("\"cooldown\": 0", "\"cooldown\": -1")), "hasty");
    }

    // 命中、迴避、暴擊率等百分點只能用 amount；對手只能增減戰鬥用的能力
    [Theory]
    [InlineData("""[{ "kind": "StatChange", "target": "Caster", "stat": "evasion", "percent": 5, "rounds": 3 }]""")]
    [InlineData("""[{ "kind": "StatChange", "target": "Opponent", "stat": "escapeRate", "amount": 5, "rounds": 3 }]""")]
    public void Skills_RejectStatChangesOutsideTheirUse(string effects) =>
        AssertSkillsRejected(ComposedSkillJson("odd-stat", 1, ComposedLevel(1, ChanceForty, effects)), "odd-stat 第 1 級");

    [Fact]
    public void Load_ChanceByStat_IsOnStunEffect()
    {
        const string effects = """[{ "kind": "Stun", "chance": 2, "chanceByStat": { "stat": "intelligence", "percent": 5 }, "rounds": 1 }]""";

        var level = LoadSkills(ComposedSkillJson("apparent-death", 1, ComposedLevel(1, ChanceForty, effects))).GetSkill("apparent-death").LevelAt(1);

        Assert.Equal(new SkillChanceAdjustment(SkillScalingBasis.Intelligence, 5), Assert.IsType<StunEffect>(level.Effects[0]).ChanceByStat);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Load_InvalidChanceByStat_Throws(int percent)
    {
        var effects = $$"""[{ "kind": "Stun", "chance": 2, "chanceByStat": { "stat": "intelligence", "percent": {{percent}} }, "rounds": 1 }]""";

        AssertSkillsRejected(ComposedSkillJson("apparent-death", 1, ComposedLevel(1, ChanceForty, effects)), "apparent-death");
    }

    [Fact]
    public void Skill_DamageControlAndElementEffectsLoad()
    {
        const string effects = """
            [{ "kind": "BonusDamage", "attackKind": "Physical", "scaling": [{ "basis": "agility", "ratio": 0.15 }], "chance": 20, "rounds": 1 },
             { "kind": "Silence", "chance": 10, "rounds": 2 },
             { "kind": "AttackElement", "element": "Fire", "rounds": 5 },
             { "kind": "Restore", "resource": "SkillPoints", "percentOfMaximum": 2, "rounds": 0 }]
            """;

        var level = LoadSkills(ComposedSkillJson("mixed", 1, ComposedLevel(1, ChanceForty, effects))).GetSkill("mixed").LevelAt(1);

        Assert.Equal(20, Assert.IsType<BonusDamageEffect>(level.Effects[0]).Chance);
        Assert.Equal(new SilenceEffect(10, 2), level.Effects[1]);
        Assert.Equal(new AttackElementEffect(3, 5), level.Effects[2]);
        Assert.Equal(2, Assert.IsType<RestoreEffect>(level.Effects[3]).PercentOfMaximum);
    }

    [Fact]
    public void Skill_ChanceOnUnsupportedEffectIsRejected() =>
        AssertSkillsRejected(
            ComposedSkillJson("lucky", 1, ComposedLevel(1, ChanceForty, """[{ "kind": "MultiHit", "attackKind": "Physical", "hits": 2, "chance": 50, "rounds": 1 }]""")),
            "lucky 第 1 級");

    [Fact]
    public void Skill_ConditionsAndCostsLoad()
    {
        const string conditions = """[{ "kind": "StatAbove", "stat": "strength" }, { "kind": "HitPointsBelowOpponent" }, { "kind": "Chance", "percent": 40 }]""";
        const string costs = """[{ "kind": "HitPoints", "percent": 45 }, { "kind": "SkillPoints", "all": true }, { "kind": "Money", "amount": 100 }]""";

        var level = LoadSkills(ComposedSkillJson("bold", 1, ComposedLevel(1, conditions, AttackUp, costs))).GetSkill("bold").LevelAt(1);

        Assert.Equal(SkillScalingBasis.Strength, level.Conditions[0].Stat);
        Assert.Equal(SkillConditionKind.HitPointsBelowOpponent, level.Conditions[1].Kind);
        Assert.Equal((45.0, true, 100), (level.Costs[0].Percent!.Value, level.Costs[1].All, level.Costs[2].Amount));
    }

    [Fact]
    public void Skill_CostWithTwoValuesIsRejected() =>
        AssertSkillsRejected(
            ComposedSkillJson("greedy", 1, ComposedLevel(1, ChanceForty, AttackUp, """[{ "kind": "HitPoints", "amount": 10, "percent": 10 }]""")),
            "greedy 第 1 級");

    [Fact]
    public void Skills_RejectUnknownEnumValue()
    {
        AssertSkillsRejected(ComposedSkillJson("bogus-type", 1, ComposedLevel(1, ChanceForty, AttackUp)).Replace("\"Active\"", "\"bogus\""), "bogus-type");
        AssertSkillsRejected(ComposedSkillJson("bogus-stat", 1, ComposedLevel(1, ChanceForty, AttackUp.Replace("attack", "moonlight"))), "bogus-stat 第 1 級");
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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
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
                "test-axe", "test-heavy-sword", "test-power-ring", "test-str-potion",
                "test-bag-expansion", "test-depot-expansion", "test-life-amulet", "test-bound-sword",
                "test-refine-stone-2", "item-1378", "item-3361", "safe-refine-stone",
                "test-armor", "test-gloves", "test-boots", "test-medal-a", "test-medal-b", "test-capture-net", "test-key",
                "test-arcane-refine-stone-2", "test-vigor-refine-stone", "test-stone-box", "test-pet-egg", "test-job-notes",
                "test-exp-elixir",
            ],
            content.AllItems.Select(item => item.Id));
        Assert.Equal((ItemCategory.Key, new AreaKeySpec(17, 3600)), (content.GetItem("test-key").Category, content.GetItem("test-key").Key));

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
        Assert.Equal(1, stone.BoxClass);
        Assert.Equal((ConsumableKind.StoneBox, 1), (content.GetItem("test-stone-box").Consumable, content.GetItem("test-stone-box").Amount));

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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
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
        { ValidSupply, "\"consumable\": \"Supply\"", "\"consumable\": \"BagExpansion\"", "amount" },
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

    [Fact]
    public void Load_TestContent_ExposesMissions()
    {
        var content = JsonGameContent.Load(TestContentDirectory);

        Assert.Equal(
            [
                "test-hunt", "test-sprite-bounty", "test-delivery", "test-chief-hunt", "test-skill-reward", "test-daily", "test-daily-pet",
                "test-pet-reward", "test-stone-reward",
            ],
            content.AllMissions.Select(mission => mission.Id));
        Assert.Equivalent(
            new MissionDefinition(
                "test-delivery", "測試鐵匠", "測試交付", "帶三塊測試礦石來。", "收下了。",
                Level: 1, PlaceId: 1, Daily: false, Kill: null, Deliver: [new ItemQuantity("test-ore", 3)],
                Requirements: new MissionRequirements(),
                Rewards: new MissionReward(50, [new ItemQuantity("safe-refine-stone", 1)]),
                DailyKills: [], DailyRewards: []),
            content.GetMission("test-delivery"),
            strict: true);
        Assert.Equal(190, content.GetMission("test-hunt").Exp);

        var skillReward = content.GetMission("test-skill-reward");
        Assert.Equal(new MissionRequirements(JobId: "warrior", PrerequisiteId: "test-hunt"), skillReward.Requirements);
        Assert.Equal(new SkillReward("iron-skin", 1), skillReward.Rewards.Skill);
        Assert.Equal(new KillTarget("test-wolf", 2), skillReward.Kill);

        Assert.Equal(["test-sprite", "test-wolf"], content.GetMission("test-daily-pet").DailyPets);
        Assert.Equal(new PetReward("test-wolf", 2), content.GetMission("test-pet-reward").Rewards.Pet);

        var daily = content.GetMission("test-daily");
        Assert.True(daily.Daily);
        Assert.Null(daily.PlaceId);
        Assert.Equal([new KillTarget("test-sprite", 2), new KillTarget("test-wolf", 2)], daily.DailyKills);
        Assert.Equivalent(
            new[] { new MissionReward(100, []), new MissionReward(0, [new ItemQuantity("test-supply", 2)]) },
            daily.DailyRewards,
            strict: true);

        Assert.Contains("ghost-mission", Assert.Throws<GameContentException>(() => content.GetMission("ghost-mission")).Message);
    }

    // 任務驗證用的內容：地區 1（有子地區）、2（沒有）、3（禁區）；任務怪 chief 屬於 chief-hunt；全域怪 puff 讓每個地區都有怪
    private const string MissionWorld = """
        { "startingPlaceId": 1, "startingSubPlace": 1,
          "areas": [{ "id": "central", "name": "中", "placeIds": [1, 2, 3] }],
          "places": [
            { "id": 1, "name": "甲", "subPlaces": ["a", "b", "c", "d"], "recommended": [1, 10], "restricted": false },
            { "id": 2, "name": "乙", "subPlaces": [], "recommended": [5, 15], "restricted": false },
            { "id": 3, "name": "丙", "subPlaces": ["a", "b", "c", "d"], "recommended": [60, 80], "restricted": true }] }
        """;

    // 每個禁區恰好一把鑰匙
    private const string MissionItems =
        """{ "items": [{ "id": "herb", "name": "藥草", "category": "Material", "price": 1, "shopLevel": 1, "shopSellable": false }, { "id": "gate-key", "name": "丙鑰匙", "category": "Key", "price": 0, "shopLevel": 1, "shopSellable": false, "key": { "placeId": 3, "seconds": 3600 } }] }""";

    private const string MissionMonsters = """
        { "monsters": [
          { "id": "sprite", "name": "雪精", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0, "placeId": 1, "subPlaces": [1] },
          { "id": "chief", "name": "首領", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 60, "placeId": 1, "subPlaces": [1], "missionId": "chief-hunt" },
          { "id": "puff", "name": "絨球", "level": 1, "hp": 59, "sp": 12, "at": 16, "mat": 6, "df": 6, "mdf": 3, "agi": 7, "au": 5, "element": 0, "jobExp": 10, "meet": 95, "placeId": 0 }] }
        """;

    private const string NoJobSkills = """{ "jobSkills": [] }""";

    private static readonly string MissionSkills =
        $$"""{ "skills": [{{ComposedSkillJson("guard", 1, ComposedLevel(1, ChanceForty, AttackUp))}}] }""";

    private const string MissionJobSkills = """{ "jobSkills": [{ "jobId": "warrior", "skillId": "guard" }] }""";

    private const string ValidHunt =
        """{ "id": "hunt", "giver": "村長", "title": "討伐", "level": 1, "placeId": 1, "daily": false, "kill": { "monsterId": "sprite", "count": 3 }, "deliver": [{ "itemId": "herb", "quantity": 2 }], "requirements": { "jobId": "warrior" }, "rewards": { "money": 50, "items": [{ "itemId": "herb", "quantity": 1 }], "skill": { "skillId": "guard", "level": 1 } } }""";

    private const string ValidChiefHunt =
        """{ "id": "chief-hunt", "giver": "隊長", "title": "首領", "level": 2, "placeId": 1, "kill": { "monsterId": "chief", "count": 1 }, "requirements": { "sex": 2, "birth": 4, "prerequisiteId": "hunt" } }""";

    private const string ValidRival =
        """{ "id": "rival", "giver": "對手", "title": "對手", "level": 1, "placeId": 1, "kill": { "monsterId": "sprite", "count": 4 }, "requirements": { "conflictsWith": "hunt" } }""";

    private const string ValidDaily =
        """{ "id": "daily", "giver": "公會", "title": "每日", "level": 1, "placeId": null, "daily": true, "dailyKills": [{ "monsterId": "sprite", "count": 2 }], "dailyRewards": [{ "money": 100, "items": [{ "itemId": "herb", "quantity": 1 }] }] }""";

    private const string ValidMissionList = $"{ValidHunt}, {ValidChiefHunt}, {ValidRival}, {ValidDaily}";

    private static T WithMissionContent<T>(string missions, string monsters, Func<string, T> action)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), monsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), MissionSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), MissionWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), MissionItems);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), $$"""{ "missions": [{{missions}}] }""");
            WriteCompanionContent(directory.FullName, jobSkills: MissionJobSkills);
            return action(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string MissionRejection(string missions, string monsters = MissionMonsters) =>
        WithMissionContent(missions, monsters, directory =>
            Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory)).Message);

    // 片段必須在任務清單中恰好出現一次，避免改到別的任務
    private static string ReplaceOnce(string json, string validField, string invalidField)
    {
        var first = json.IndexOf(validField, StringComparison.Ordinal);
        Assert.True(first >= 0, $"找不到片段：{validField}");
        Assert.Equal(-1, json.IndexOf(validField, first + 1, StringComparison.Ordinal));
        return json.Replace(validField, invalidField);
    }

    private static string MissionFieldRejection(string validField, string invalidField) =>
        MissionRejection(ReplaceOnce(ValidMissionList, validField, invalidField));

    [Fact]
    public void Missions_AcceptValidSample()
    {
        var content = WithMissionContent(ValidMissionList, MissionMonsters, JsonGameContent.Load);

        Assert.Equal(["hunt", "chief-hunt", "rival", "daily"], content.AllMissions.Select(mission => mission.Id));
        Assert.Equal(new MissionRequirements(Sex: 2, Birth: 4, PrerequisiteId: "hunt"), content.GetMission("chief-hunt").Requirements);
        Assert.Equal("chief-hunt", content.GetMonster("chief").MissionId);
    }

    // 必填字串漏寫時是內容錯誤（GameContentException），不是 ArgumentNullException；title 是任務的名稱
    [Theory]
    [InlineData("\"title\": \"討伐\", ", "", "hunt", "title")]
    [InlineData("\"giver\": \"村長\", ", "", "hunt", "giver")]
    [InlineData("\"kill\": { \"monsterId\": \"sprite\", \"count\": 3 }", "\"kill\": { \"count\": 3 }", "hunt", "monsterId")]
    [InlineData("\"deliver\": [{ \"itemId\": \"herb\", \"quantity\": 2 }]", "\"deliver\": [{ \"quantity\": 2 }]", "hunt", "itemId")]
    [InlineData("\"skill\": { \"skillId\": \"guard\", \"level\": 1 }", "\"skill\": { \"level\": 1 }", "hunt", "skillId")]
    [InlineData("\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", "\"dailyKills\": [{ \"count\": 2 }]", "daily", "monsterId")]
    [InlineData("\"rewards\": { \"money\": 50, \"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }]", "\"rewards\": { \"money\": 50, \"items\": [{ \"quantity\": 1 }]", "hunt", "itemId")]
    [InlineData("\"skill\": { \"skillId\": \"guard\", \"level\": 1 } }", "\"skill\": { \"skillId\": \"guard\", \"level\": 1 }, \"pet\": { \"level\": 1 } }", "hunt", "pet 的 monsterId")]
    [InlineData("\"dailyRewards\": [{ \"money\": 100, \"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }] }]", "\"dailyRewards\": [{ \"money\": 100, \"items\": [{ \"quantity\": 1 }] }]", "daily", "dailyRewards items 的 itemId")]
    [InlineData("\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", "\"dailyPets\": [null]", "daily", "dailyPets")]
    public void Missions_MissingRequiredStringThrowsGameContentException(string validField, string invalidField, string missionId, string field)
    {
        var message = MissionFieldRejection(validField, invalidField);
        Assert.Contains(missionId, message);
        Assert.Contains(field, message);
    }

    [Fact]
    public void Missions_RejectDuplicateId()
    {
        var message = MissionRejection($"{ValidMissionList}, {ValidHunt}");
        Assert.Contains("任務 id 重複：hunt", message);
    }

    [Fact]
    public void Missions_RejectIdLongerThan32()
    {
        var longId = new string('a', 33);
        Assert.Contains(longId, MissionFieldRejection("\"id\": \"rival\"", $"\"id\": \"{longId}\""));
        // 剛好 32 字可以
        var maximumId = new string('a', 32);
        var content = WithMissionContent(
            ReplaceOnce(ValidMissionList, "\"id\": \"rival\"", $"\"id\": \"{maximumId}\""), MissionMonsters, JsonGameContent.Load);
        Assert.Equal(maximumId, content.GetMission(maximumId).Id);
    }

    [Fact]
    public void Missions_RejectWithoutObjective()
    {
        var message = MissionFieldRejection(
            "\"kill\": { \"monsterId\": \"sprite\", \"count\": 3 }, \"deliver\": [{ \"itemId\": \"herb\", \"quantity\": 2 }], ", "");
        Assert.Contains("hunt", message);
        Assert.Contains("kill", message);
    }

    [Fact]
    public void Missions_RejectKillTargetOutsidePlace()
    {
        var message = MissionFieldRejection("\"placeId\": 1, \"daily\": false", "\"placeId\": 2, \"daily\": false");
        Assert.Contains("hunt", message);
        Assert.Contains("sprite", message);
    }

    [Fact]
    public void Missions_RejectUnknownDeliverItem()
    {
        var message = MissionFieldRejection("\"deliver\": [{ \"itemId\": \"herb\"", "\"deliver\": [{ \"itemId\": \"phantom\"");
        Assert.Contains("hunt", message);
        Assert.Contains("phantom", message);
    }

    [Fact]
    public void Missions_RejectDailyWithFixedObjective()
    {
        var message = MissionFieldRejection("\"daily\": true,", "\"daily\": true, \"kill\": { \"monsterId\": \"sprite\", \"count\": 2 },");
        Assert.Contains("daily", message);
        Assert.Contains("kill", message);
    }

    [Fact]
    public void Missions_RejectDailyPoolWithMissionMonster()
    {
        var message = MissionFieldRejection(
            "\"dailyKills\": [{ \"monsterId\": \"sprite\"", "\"dailyKills\": [{ \"monsterId\": \"chief\"");
        Assert.Contains("daily", message);
        Assert.Contains("chief", message);
    }

    private const string PetRewardField = "\"skill\": { \"skillId\": \"guard\", \"level\": 1 }";

    private static string WithPetReward(string monsterId, int level) =>
        ReplaceOnce(ValidMissionList, PetRewardField, $"{PetRewardField}, \"pet\": {{ \"monsterId\": \"{monsterId}\", \"level\": {level} }}");

    // 每日任務的報酬池也可以有寵物；level 1～280（WOG 寵物蛋倍率 280 為上限）
    [Fact]
    public void Missions_AcceptPetReward()
    {
        var missions = ReplaceOnce(
            WithPetReward("sprite", 280),
            "\"dailyRewards\": [{ \"money\": 100,",
            "\"dailyRewards\": [{ \"money\": 0, \"pet\": { \"monsterId\": \"puff\", \"level\": 1 } }, { \"money\": 100,");

        var content = WithMissionContent(missions, MissionMonsters, JsonGameContent.Load);

        Assert.Equal(new PetReward("sprite", 280), content.GetMission("hunt").Rewards.Pet);
        Assert.Equal(new PetReward("puff", 1), content.GetMission("daily").DailyRewards[0].Pet);
    }

    [Fact]
    public void Missions_RejectPetRewardForMissionMonster()
    {
        var message = MissionRejection(WithPetReward("chief", 1));
        Assert.Contains("hunt", message);
        Assert.Contains("chief", message);
        Assert.Contains("寵物", message);

        Assert.Contains("ghost-monster", MissionRejection(WithPetReward("ghost-monster", 1)));
        Assert.Contains("level", MissionRejection(WithPetReward("sprite", 0)));
        Assert.Contains("level", MissionRejection(WithPetReward("sprite", 281)));
    }

    [Fact]
    public void Missions_AcceptDailyPets()
    {
        var missions = ReplaceOnce(
            ValidMissionList, "\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", "\"dailyPets\": [\"sprite\", \"puff\"]");

        var daily = WithMissionContent(missions, MissionMonsters, JsonGameContent.Load).GetMission("daily");

        Assert.Equal(["sprite", "puff"], daily.DailyPets);
        Assert.Empty(daily.DailyKills);
    }

    [Fact]
    public void Missions_RejectDailyWithBothPools()
    {
        var message = MissionFieldRejection("\"daily\": true,", "\"daily\": true, \"dailyPets\": [\"sprite\"],");
        Assert.Contains("daily", message);
        Assert.Contains("dailyPets", message);

        var neither = MissionFieldRejection("\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", "\"dailyPets\": []");
        Assert.Contains("dailyPets", neither);
    }

    [Theory]
    [InlineData("\"ghost-monster\"", "ghost-monster")]
    [InlineData("\"chief\"", "chief")]
    [InlineData("\"sprite\", \"sprite\"", "重複")]
    public void Missions_RejectInvalidDailyPets(string pool, string expected)
    {
        var message = MissionFieldRejection(
            "\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", $"\"dailyPets\": [{pool}]");
        Assert.Contains("daily", message);
        Assert.Contains(expected, message);
    }

    [Fact]
    public void Missions_RejectDailyPetsOnNormalMission()
    {
        var message = MissionFieldRejection("\"daily\": false,", "\"daily\": false, \"dailyPets\": [\"sprite\"],");
        Assert.Contains("hunt", message);
        Assert.Contains("dailyPets", message);
    }

    [Fact]
    public void Missions_RejectSkillRewardWithoutMatchingJob()
    {
        var message = MissionFieldRejection("\"requirements\": { \"jobId\": \"warrior\" }, ", "");
        Assert.Contains("hunt", message);
        Assert.Contains("guard", message);
    }

    // hunt → chief-hunt → hunt
    [Fact]
    public void Missions_RejectPrerequisiteCycle()
    {
        var message = MissionFieldRejection(
            "\"requirements\": { \"jobId\": \"warrior\" }",
            "\"requirements\": { \"jobId\": \"warrior\", \"prerequisiteId\": \"chief-hunt\" }");
        Assert.Contains("hunt", message);
        Assert.Contains("循環", message);
    }

    // 任務怪的 missionId 必須指向以牠為擊殺目標的一般任務；chief-hunt 改打 sprite，沒有任務以 chief 為目標
    [Theory]
    [InlineData("ghost-mission")]
    [InlineData("hunt")]
    [InlineData("daily")]
    [InlineData("chief-hunt")]
    public void Monsters_RejectMissionIdWithoutMatchingMission(string missionId)
    {
        var monsters = MissionMonsters.Replace("\"missionId\": \"chief-hunt\"", $"\"missionId\": \"{missionId}\"");
        var missions = ReplaceOnce(ValidMissionList, "\"monsterId\": \"chief\"", "\"monsterId\": \"sprite\"");
        var message = MissionRejection(missions, monsters);
        Assert.Contains("怪物 chief", message);
        Assert.Contains(missionId, message);
    }

    [Fact]
    public void Monsters_RejectGlobalMissionMonster()
    {
        // 另加一隻一般的全域怪，讓地區 2、3 仍有怪
        var monsters = MissionMonsters.Replace(
            "\"placeId\": 0 }",
            "\"placeId\": 0, \"missionId\": \"hunt\" }, " + ValidMonster.Replace("\"sprite\"", "\"wanderer\""));
        Assert.NotEqual(MissionMonsters, monsters);
        Assert.Contains("怪物 puff 是全域怪", MissionRejection(ValidMissionList, monsters));
    }

    // 只有任務怪的地區平常遇不到任何怪
    [Fact]
    public void Content_RejectsPlaceWithOnlyMissionMonsters()
    {
        const string monsters = """
            { "monsters": [
              { "id": "sprite", "name": "雪精", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0, "placeId": 1, "subPlaces": [1] },
              { "id": "wraith", "name": "亡靈", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0, "placeId": 3, "subPlaces": [1] },
              { "id": "chief", "name": "首領", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 60, "placeId": 2, "missionId": "cave-hunt" }] }
            """;
        const string missions =
            """{ "id": "cave-hunt", "giver": "隊長", "title": "洞", "level": 1, "placeId": 2, "kill": { "monsterId": "chief", "count": 1 } }""";
        Assert.Contains("地區 2", MissionRejection(missions, monsters));
    }

    // (原片段、改成、應含的任務 id、應含的欄位或說明)
    public static TheoryData<string, string, string, string> InvalidMissionFields => new()
    {
        { "\"id\": \"rival\"", "\"id\": \" \"", "任務 id", "空" },
        { "\"giver\": \"村長\"", "\"giver\": \"\"", "hunt", "giver" },
        { "\"title\": \"討伐\"", "\"title\": \" \"", "hunt", "title" },
        { "\"level\": 2", "\"level\": 0", "chief-hunt", "level" },
        { "\"placeId\": 1, \"daily\": false", "\"placeId\": 7, \"daily\": false", "hunt", "placeId" },
        { "\"placeId\": 1, \"daily\": false", "\"placeId\": null, \"daily\": false", "hunt", "placeId" },
        { "\"placeId\": 1, \"daily\": false", "\"placeId\": 3, \"daily\": false", "hunt", "禁區" },
        { "\"daily\": false,", "\"daily\": false, \"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }],", "hunt", "dailyKills" },
        { "\"daily\": false,", "\"daily\": false, \"dailyRewards\": [{ \"money\": 1 }],", "hunt", "dailyRewards" },
        { "\"count\": 3", "\"count\": 0", "hunt", "count" },
        { "\"monsterId\": \"sprite\", \"count\": 3", "\"monsterId\": \"ghost-monster\", \"count\": 3", "hunt", "ghost-monster" },
        { "\"monsterId\": \"sprite\", \"count\": 4", "\"monsterId\": \"chief\", \"count\": 4", "rival", "chief-hunt" },
        { "\"quantity\": 2", "\"quantity\": 0", "hunt", "quantity" },
        { "\"quantity\": 2", "\"quantity\": 100", "hunt", "quantity" },
        { "\"deliver\": [{ \"itemId\": \"herb\", \"quantity\": 2 }]", "\"deliver\": [{ \"itemId\": \"herb\", \"quantity\": 2 }, { \"itemId\": \"herb\", \"quantity\": 1 }]", "hunt", "重複" },
        { "\"placeId\": null, \"daily\": true", "\"placeId\": 1, \"daily\": true", "daily", "placeId" },
        { "\"daily\": true,", "\"daily\": true, \"deliver\": [{ \"itemId\": \"herb\", \"quantity\": 1 }],", "daily", "deliver" },
        { "\"daily\": true,", "\"daily\": true, \"requirements\": { \"prerequisiteId\": \"hunt\" },", "daily", "prerequisiteId" },
        { "\"daily\": true,", "\"daily\": true, \"requirements\": { \"conflictsWith\": \"hunt\" },", "daily", "conflictsWith" },
        { "\"daily\": true,", "\"daily\": true, \"rewards\": { \"money\": 5 },", "daily", "rewards" },
        { "\"daily\": true,", "\"daily\": true, \"rewards\": { \"money\": 0, \"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }] },", "daily", "rewards" },
        { "\"dailyKills\": [{ \"monsterId\": \"sprite\", \"count\": 2 }]", "\"dailyKills\": []", "daily", "dailyKills" },
        { "\"dailyRewards\": [{ \"money\": 100, \"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }] }]", "\"dailyRewards\": []", "daily", "dailyRewards" },
        { "\"monsterId\": \"sprite\", \"count\": 2", "\"monsterId\": \"ghost-monster\", \"count\": 2", "daily", "ghost-monster" },
        { "\"monsterId\": \"sprite\", \"count\": 2", "\"monsterId\": \"sprite\", \"count\": 0", "daily", "count" },
        { "\"money\": 100, \"items\": [{ \"itemId\": \"herb\"", "\"money\": 100, \"items\": [{ \"itemId\": \"phantom\"", "daily", "phantom" },
        { "\"jobId\": \"warrior\"", "\"jobId\": \"nobody\"", "hunt", "nobody" },
        { "\"sex\": 2", "\"sex\": 3", "chief-hunt", "sex" },
        { "\"sex\": 2", "\"sex\": 0", "chief-hunt", "sex" },
        { "\"birth\": 4", "\"birth\": 5", "chief-hunt", "birth" },
        { "\"birth\": 4", "\"birth\": 0", "chief-hunt", "birth" },
        { "\"prerequisiteId\": \"hunt\"", "\"prerequisiteId\": \"ghost-mission\"", "chief-hunt", "ghost-mission" },
        { "\"prerequisiteId\": \"hunt\"", "\"prerequisiteId\": \"chief-hunt\"", "chief-hunt", "prerequisiteId" },
        { "\"prerequisiteId\": \"hunt\"", "\"prerequisiteId\": \"daily\"", "chief-hunt", "daily" },
        { "\"conflictsWith\": \"hunt\"", "\"conflictsWith\": \"ghost-mission\"", "rival", "ghost-mission" },
        { "\"conflictsWith\": \"hunt\"", "\"conflictsWith\": \"rival\"", "rival", "conflictsWith" },
        { "\"conflictsWith\": \"hunt\"", "\"conflictsWith\": \"daily\"", "rival", "daily" },
        { "\"money\": 50", "\"money\": -1", "hunt", "money" },
        { "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }], \"skill\"", "\"items\": [{ \"itemId\": \"phantom\", \"quantity\": 1 }], \"skill\"", "hunt", "phantom" },
        { "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }], \"skill\"", "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 0 }], \"skill\"", "hunt", "quantity" },
        { "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }], \"skill\"", "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 100 }], \"skill\"", "hunt", "quantity" },
        { "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }], \"skill\"", "\"items\": [{ \"itemId\": \"herb\", \"quantity\": 1 }, { \"itemId\": \"herb\", \"quantity\": 2 }], \"skill\"", "hunt", "重複" },
        { "\"skillId\": \"guard\", \"level\": 1", "\"skillId\": \"ghost-skill\", \"level\": 1", "hunt", "ghost-skill" },
        { "\"skillId\": \"guard\", \"level\": 1", "\"skillId\": \"guard\", \"level\": 2", "hunt", "level" },
    };

    [Theory]
    [MemberData(nameof(InvalidMissionFields))]
    public void Missions_RejectInvalidFields(string validField, string invalidField, string offendingId, string expected)
    {
        var message = MissionFieldRejection(validField, invalidField);
        Assert.Contains(offendingId, message);
        Assert.Contains(expected, message);
    }

    [Fact]
    public void Load_TestContent_ExposesCompanionContent()
    {
        var content = JsonGameContent.Load(TestContentDirectory);

        Assert.Equal(
            [
                new MercenaryDefinition("test-mercenary", "測試傭兵", 100, 3, 50, 100, 50, 2),
                new MercenaryDefinition("test-honour-mercenary", "測試榮譽傭兵", null, 5, 100, 100, 100, 1),
            ],
            content.AllMercenaries);
        Assert.Equal(3, content.GetMercenary("test-mercenary").Fights);
        Assert.Contains("ghost-mercenary", Assert.Throws<GameContentException>(() => content.GetMercenary("ghost-mercenary")).Message);

        Assert.Equivalent(
            new[]
            {
                new HonourExchange("test-exchange-ore", "test-ore", null, 1, [new ItemQuantity("test-medal-a", 2)]),
                new HonourExchange(
                    "test-exchange-mercenary", null, "test-honour-mercenary", 1,
                    [new ItemQuantity("test-medal-a", 1), new ItemQuantity("test-medal-b", 1)]),
            },
            content.HonourShop,
            strict: true);

        var net = content.GetItem("test-capture-net");
        Assert.Equal((ItemCategory.Consumable, ConsumableKind.Capture, 2), (net.Category, net.Consumable, net.Amount));
        Assert.Equal(ItemCategory.Medal, content.GetItem("test-medal-a").Category);
        Assert.Equal(ItemCategory.Medal, content.GetItem("test-medal-b").Category);
    }

    private const string ValidMedal =
        """{ "id": "badge", "name": "章", "category": "Medal", "price": 0, "shopLevel": 1, "shopSellable": false }""";

    private const string ValidCapture =
        """{ "id": "net", "name": "網", "category": "Consumable", "consumable": "Capture", "amount": 2, "price": 50, "shopLevel": 1, "shopSellable": true }""";

    [Fact]
    public void Items_RejectCaptureWithoutAmount()
    {
        var message = ItemRejection($"{ValidItemList}, {Replace(ValidCapture, "\"amount\": 2, ", "")}");
        Assert.Contains("net", message);
        Assert.Contains("amount", message);
    }

    private const string ValidStoneBox =
        """{ "id": "chest", "name": "箱", "category": "Consumable", "consumable": "StoneBox", "amount": 1, "price": 1000, "shopLevel": 1, "shopSellable": false }""";

    private const string ValidRefineBox =
        """{ "id": "casket", "name": "盒", "category": "Consumable", "consumable": "RefineBox", "amount": 1, "price": 1500, "shopLevel": 1, "shopSellable": false }""";

    private static string ClassOneStone => Replace(ValidStone, "\"fixed\": { \"at\": 5 }", "\"fixed\": { \"at\": 5 }, \"boxClass\": 1");

    // 魔石寶箱至少要有一顆 boxClass 1 的魔石；amount 1～3；boxClass 0～3
    [Fact]
    public void Items_RejectStoneBoxWithoutClassOneStone()
    {
        var message = ItemRejection($"{ValidItemList}, {ValidStoneBox}");
        Assert.Contains("chest", message);
        Assert.Contains("boxClass", message);

        Assert.Contains("amount", ItemRejection($"{ValidItem}, {ValidOre}, {ValidSupply}, {ValidRefineStone}, {ClassOneStone}, {Replace(ValidStoneBox, "\"amount\": 1", "\"amount\": 4")}"));
        Assert.Contains("boxClass", ItemRejection($"{ValidItem}, {ValidOre}, {ValidSupply}, {ValidRefineStone}, {Replace(ClassOneStone, "\"boxClass\": 1", "\"boxClass\": 4")}"));
    }

    // 精鍊寶盒的 amount 必須是存在的精鍊石等級（樣本只有 Lv1）
    [Fact]
    public void Items_RejectRefineBoxForMissingLevel()
    {
        var message = ItemRejection($"{ValidItemList}, {Replace(ValidRefineBox, "\"amount\": 1", "\"amount\": 2")}");
        Assert.Contains("casket", message);
        Assert.Contains("amount", message);

        Assert.Contains("amount", ItemRejection($"{ValidItemList}, {Replace(ValidRefineBox, "\"amount\": 1, ", "")}"));
    }

    // 捕捉道具只用 amount（倍率）；勳章不是消耗品，其餘規則同材料
    public static TheoryData<string, string, string, string> InvalidCompanionItems => new()
    {
        { ValidCapture, "\"amount\": 2", "\"amount\": 2, \"supplyHp\": 10", "supplyHp" },
        { ValidCapture, "\"amount\": 2", "\"amount\": 2, \"bonus\": { \"at\": 1 }", "bonus" },
        { ValidCapture, "\"category\": \"Consumable\"", "\"category\": \"Material\"", "consumable" },
        { ValidMedal, "\"price\": 0", "\"consumable\": \"Capture\", \"amount\": 1, \"price\": 0", "consumable" },
        { ValidMedal, "\"price\": 0", "\"holes\": 1, \"price\": 0", "holes" },
        { ValidMedal, "\"price\": 0", "\"requires\": { \"str\": 1 }, \"price\": 0", "需求" },
    };

    [Theory]
    [MemberData(nameof(InvalidCompanionItems))]
    public void Items_RejectInvalidCompanionItems(string item, string validField, string invalidField, string expected)
    {
        var id = item[(item.IndexOf("\"id\": \"", StringComparison.Ordinal) + 7)..].Split('"')[0];
        var message = ItemRejection($"{ValidItemList}, {Replace(item, validField, invalidField)}");
        Assert.Contains(id, message);
        Assert.Contains(expected, message);
    }

    private const string ValidMercenary =
        """{ "id": "hireling", "name": "僱傭兵", "price": 100, "fights": 3, "moneyPercent": 50, "dropPercent": 100, "expPercent": 50, "deathCost": 2 }""";

    private const string ValidHonourMercenary =
        """{ "id": "veteran", "name": "老兵", "price": null, "fights": 5, "moneyPercent": 100, "dropPercent": 100, "expPercent": 100, "deathCost": 1 }""";

    private const string ValidItemExchange =
        """{ "id": "swap-ore", "itemId": "ore", "quantity": 1, "cost": [{ "itemId": "badge", "quantity": 2 }] }""";

    private const string ValidMercenaryExchange =
        """{ "id": "swap-veteran", "mercenaryId": "veteran", "quantity": 1, "cost": [{ "itemId": "badge", "quantity": 1 }] }""";

    private const string ValidMercenaryList = $"{ValidMercenary}, {ValidHonourMercenary}";

    private const string ValidExchangeList = $"{ValidItemExchange}, {ValidMercenaryExchange}";

    private static T WithCompanionContent<T>(string mercenaries, string exchanges, Func<string, T> action)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(
                Path.Combine(directory.FullName, "items.json"), $$"""{ "items": [{{ValidOre}}, {{ValidSupply}}, {{ValidMedal}}, {{ValidCapture}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(
                directory.FullName,
                $$"""{ "mercenaries": [{{mercenaries}}] }""",
                $$"""{ "exchanges": [{{exchanges}}] }""");
            return action(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string CompanionRejection(
        string mercenaries = ValidMercenaryList, string exchanges = ValidExchangeList) =>
        WithCompanionContent(mercenaries, exchanges, directory =>
            Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory)).Message);

    [Fact]
    public void Companions_AcceptValidSample()
    {
        var content = WithCompanionContent(ValidMercenaryList, ValidExchangeList, JsonGameContent.Load);

        Assert.Equal(["hireling", "veteran"], content.AllMercenaries.Select(mercenary => mercenary.Id));
        Assert.Null(content.GetMercenary("veteran").Price);
        Assert.Equal(["swap-ore", "swap-veteran"], content.HonourShop.Select(exchange => exchange.Id));
    }

    [Fact]
    public void Mercenaries_RejectDuplicateId() =>
        Assert.Contains("傭兵 id 重複：hireling", CompanionRejection(mercenaries: $"{ValidMercenaryList}, {ValidMercenary}"));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Mercenaries_RejectPercentOutOfRange(int percent)
    {
        var message = CompanionRejection(
            mercenaries: ReplaceOnce(ValidMercenaryList, "\"moneyPercent\": 50", $"\"moneyPercent\": {percent}"));
        Assert.Contains("hireling", message);
        Assert.Contains("moneyPercent", message);
    }

    [Fact]
    public void HonourShop_RejectNonMedalCost()
    {
        var message = CompanionRejection(
            exchanges: ReplaceOnce(ValidExchangeList, "{ \"itemId\": \"badge\", \"quantity\": 2 }", "{ \"itemId\": \"ore\", \"quantity\": 2 }"));
        Assert.Contains("swap-ore", message);
        Assert.Contains("勳章", message);
    }

    [Fact]
    public void HonourShop_RejectBothItemAndMercenary()
    {
        var message = CompanionRejection(
            exchanges: ReplaceOnce(ValidExchangeList, "\"itemId\": \"ore\",", "\"itemId\": \"ore\", \"mercenaryId\": \"veteran\","));
        Assert.Contains("swap-ore", message);
        Assert.Contains("mercenaryId", message);
    }

    [Fact]
    public void HonourShop_RejectMercenaryQuantityOtherThanOne()
    {
        var message = CompanionRejection(
            exchanges: ReplaceOnce(ValidExchangeList, "\"mercenaryId\": \"veteran\", \"quantity\": 1", "\"mercenaryId\": \"veteran\", \"quantity\": 2"));
        Assert.Contains("swap-veteran", message);
        Assert.Contains("quantity", message);
    }

    [Fact]
    public void HonourShop_RejectDuplicateId() =>
        Assert.Contains("兌換 id 重複：swap-ore", CompanionRejection(exchanges: $"{ValidExchangeList}, {ValidItemExchange}"));

    // (清單、原片段、改成、應含的 id、應含的欄位或說明)
    public static TheoryData<string, string, string, string, string> InvalidCompanionFields => new()
    {
        { "mercenaries", "\"id\": \"hireling\"", "\"id\": \" \"", "傭兵 id", "空" },
        { "mercenaries", "\"id\": \"hireling\"", $"\"id\": \"{new string('m', 33)}\"", new string('m', 33), "32" },
        { "mercenaries", "\"name\": \"僱傭兵\"", "\"name\": \"\"", "hireling", "name" },
        { "mercenaries", "\"name\": \"僱傭兵\"", "\"name\": \"一二三四五六七八九\"", "hireling", "name" },
        { "mercenaries", "\"price\": 100", "\"price\": 0", "hireling", "price" },
        { "mercenaries", "\"fights\": 3", "\"fights\": 0", "hireling", "fights" },
        { "mercenaries", "\"moneyPercent\": 50, \"dropPercent\": 100", "\"moneyPercent\": 50, \"dropPercent\": 0", "hireling", "dropPercent" },
        { "mercenaries", "\"expPercent\": 50", "\"expPercent\": 101", "hireling", "expPercent" },
        { "mercenaries", "\"deathCost\": 2", "\"deathCost\": 0", "hireling", "deathCost" },
        { "exchanges", "\"id\": \"swap-ore\"", "\"id\": \"\"", "兌換 id", "空" },
        { "exchanges", "\"id\": \"swap-ore\"", $"\"id\": \"{new string('x', 33)}\"", new string('x', 33), "32" },
        { "exchanges", "\"itemId\": \"ore\", ", "", "swap-ore", "itemId" },
        { "exchanges", "\"itemId\": \"ore\"", "\"itemId\": \"phantom\"", "swap-ore", "phantom" },
        { "exchanges", "\"mercenaryId\": \"veteran\"", "\"mercenaryId\": \"ghost\"", "swap-veteran", "ghost" },
        { "exchanges", "\"itemId\": \"ore\", \"quantity\": 1", "\"itemId\": \"ore\", \"quantity\": 0", "swap-ore", "quantity" },
        { "exchanges", "\"itemId\": \"ore\", \"quantity\": 1", "\"itemId\": \"ore\", \"quantity\": 100", "swap-ore", "quantity" },
        { "exchanges", "\"cost\": [{ \"itemId\": \"badge\", \"quantity\": 2 }]", "\"cost\": []", "swap-ore", "cost" },
        { "exchanges", "{ \"itemId\": \"badge\", \"quantity\": 2 }", "{ \"itemId\": \"phantom\", \"quantity\": 2 }", "swap-ore", "phantom" },
        { "exchanges", "{ \"itemId\": \"badge\", \"quantity\": 2 }", "{ \"itemId\": \"badge\", \"quantity\": 0 }", "swap-ore", "quantity" },
        { "exchanges", "{ \"itemId\": \"badge\", \"quantity\": 2 }", "{ \"itemId\": \"badge\", \"quantity\": 100 }", "swap-ore", "quantity" },
        { "exchanges", "{ \"itemId\": \"badge\", \"quantity\": 2 }", "{ \"itemId\": \"badge\", \"quantity\": 2 }, { \"itemId\": \"badge\", \"quantity\": 1 }", "swap-ore", "重複" },
    };

    [Theory]
    [MemberData(nameof(InvalidCompanionFields))]
    public void Companions_RejectInvalidFields(string file, string validField, string invalidField, string offendingId, string expected)
    {
        var message = file == "mercenaries"
            ? CompanionRejection(mercenaries: ReplaceOnce(ValidMercenaryList, validField, invalidField))
            : CompanionRejection(exchanges: ReplaceOnce(ValidExchangeList, validField, invalidField));
        Assert.Contains(offendingId, message);
        Assert.Contains(expected, message);
    }

    // 鑰匙驗證用的地區：1 一般、17 禁區
    private const string KeyWorld = """
        { "startingPlaceId": 1, "startingSubPlace": 1,
          "areas": [{ "id": "central", "name": "中", "placeIds": [1, 17] }],
          "places": [
            { "id": 1, "name": "甲", "subPlaces": ["a", "b", "c", "d"], "recommended": [1, 10], "restricted": false },
            { "id": 17, "name": "禁", "subPlaces": [], "recommended": [60, 80], "restricted": true }] }
        """;

    private const string ValidKey =
        """{ "id": "cave-key", "name": "鑰", "category": "Key", "price": 0, "shopLevel": 1, "shopSellable": false, "key": { "placeId": 17, "seconds": 3600 } }""";

    private static void AssertKeyRejects(string items, string offendingId, string expected)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), KeyWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), $$"""{ "items": [{{items}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
            var message = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName)).Message;
            Assert.Contains(offendingId, message);
            Assert.Contains(expected, message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Items_RejectKeyForUnrestrictedPlace() =>
        AssertKeyRejects($"{ValidKey}, {ValidKey.Replace("cave-key", "plain-key").Replace("\"placeId\": 17", "\"placeId\": 1")}", "plain-key", "禁區");

    [Fact]
    public void Items_RejectSecondKeyForSamePlace() =>
        AssertKeyRejects($"{ValidKey}, {ValidKey.Replace("cave-key", "spare-key")}", "spare-key", "17");

    [Fact]
    public void Items_RejectKeySpecOnNonKeyCategory() =>
        AssertKeyRejects($"{ValidKey}, {ValidOre.Replace("\"price\": 10", "\"key\": { \"placeId\": 17, \"seconds\": 60 }, \"price\": 10")}", "ore", "key");

    // 其餘鑰匙規則：沒有 key、秒數超出 60～604 800、禁區沒有鑰匙
    [Theory]
    [InlineData(", \"key\": { \"placeId\": 17, \"seconds\": 3600 }", "", "key")]
    [InlineData("\"seconds\": 3600", "\"seconds\": 59", "seconds")]
    [InlineData("\"seconds\": 3600", "\"seconds\": 604801", "seconds")]
    [InlineData("\"category\": \"Key\", \"price\": 0, \"shopLevel\": 1, \"shopSellable\": false, \"key\": { \"placeId\": 17, \"seconds\": 3600 }", "\"category\": \"Material\", \"price\": 0, \"shopLevel\": 1, \"shopSellable\": false", "17")]
    public void Items_RejectInvalidKeys(string validField, string invalidField, string expected) =>
        AssertKeyRejects(Replace(ValidKey, validField, invalidField), expected == "17" ? "17" : "cave-key", expected);

    private static string InnRejection(string inn) =>
        WithInn(inn, directory => Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory)).Message);

    private static T WithInn<T>(string inn, Func<string, T> action)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), ValidItems);
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), ValidRecipes);
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName, inn: inn);
            return action(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Inn_LoadsSixRooms()
    {
        var inn = JsonGameContent.Load(ShippedContentDirectory).Inn;

        Assert.Equal(28_800, inn.Seconds);
        Assert.Equal(50, inn.PricePerLevel);
        Assert.Equal(
            [
                new InnRoom("apprentice-loft", "學徒閣樓", BuffKind.Exp, 1.10, 2),
                new InnRoom("training-chamber", "修練廂房", BuffKind.JobExp, 1.10, 2),
                new InnRoom("fighters-bunk", "鬥士通鋪", BuffKind.Attack, 1.05, 1),
                new InnRoom("stargazer-tower", "星見塔房", BuffKind.Magic, 1.05, 1),
                new InnRoom("bulwark-room", "鐵壁客房", BuffKind.Defense, 1.05, 1),
                new InnRoom("quiet-chapel", "靜謐禱室", BuffKind.MagicDefense, 1.05, 1),
            ],
            inn.Rooms);
    }

    [Fact]
    public void Inn_AcceptsValidSample() =>
        Assert.Equal(["loft"], WithInn(ValidInn, directory => JsonGameContent.Load(directory).Inn.Rooms.Select(room => room.Id)));

    [Theory]
    [InlineData("\"multiplier\": 1.10", "\"multiplier\": 1.0", "multiplier")]
    [InlineData("\"multiplier\": 1.10", "\"multiplier\": 3.01", "multiplier")]
    [InlineData("\"multiplier\": 1.10", "\"multiplier\": 1.255", "multiplier")]
    public void Inn_RejectsMultiplierOutOfRange(string valid, string invalid, string expected)
    {
        var message = InnRejection(Replace(ValidInn, valid, invalid));
        Assert.Contains("loft", message);
        Assert.Contains(expected, message);
    }

    [Fact]
    public void Inn_RejectsDuplicateRoomId()
    {
        const string Room = """{ "id": "loft", "name": "閣樓", "kind": "Exp", "multiplier": 1.10, "priceRate": 2 }""";
        var message = InnRejection($$"""{ "seconds": 28800, "pricePerLevel": 50, "rooms": [{{Room}}, {{Room}}] }""");
        Assert.Contains("loft", message);
    }

    [Theory]
    [InlineData("\"priceRate\": 2", "\"priceRate\": 0", "priceRate")]
    [InlineData("\"pricePerLevel\": 50", "\"pricePerLevel\": 0", "pricePerLevel")]
    [InlineData("\"seconds\": 28800", "\"seconds\": 59", "seconds")]
    [InlineData("\"seconds\": 28800", "\"seconds\": 86401", "seconds")]
    [InlineData("\"kind\": \"Exp\"", "\"kind\": \"Luck\"", "kind")]
    public void Inn_RejectsInvalidFields(string valid, string invalid, string expected) =>
        Assert.Contains(expected, InnRejection(Replace(ValidInn, valid, invalid)));

    [Fact]
    public void Inn_RejectsEmptyRooms() =>
        Assert.Contains("rooms", InnRejection("""{ "seconds": 28800, "pricePerLevel": 50, "rooms": [] }"""));

    private static void WriteCompanionContent(
        string directory, string mercenaries = ValidMercenaries, string honourShop = ValidHonourShop,
        string inn = ValidInn, string jobSkills = NoJobSkills)
    {
        File.WriteAllText(Path.Combine(directory, "job-skills.json"), jobSkills);
        File.WriteAllText(Path.Combine(directory, "monster-skills.json"), """{ "monsterSkills": [] }""");
        File.WriteAllText(Path.Combine(directory, "inn.json"), inn);
        File.WriteAllText(Path.Combine(directory, "mercenaries.json"), mercenaries);
        File.WriteAllText(Path.Combine(directory, "honour-shop.json"), honourShop);
    }

    private static void AssertLoadRejects(
        string monstersJson,
        string jobsJson,
        string offendingId,
        string skillsJson = ValidSkills,
        string worldJson = ValidWorld,
        string itemsJson = ValidItems,
        string recipesJson = ValidRecipes,
        string missionsJson = ValidMissions)
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
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), missionsJson);
            WriteCompanionContent(directory.FullName);
            var exception = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName));
            Assert.Contains(offendingId, exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private const string ValidElixir =
        """{ "id": "brew", "name": "密藥", "category": "Consumable", "consumable": "Elixir", "price": 20000, "shopLevel": 6, "shopSellable": true, "elixir": { "kind": "Exp", "multiplier": 1.5, "seconds": 3600 } }""";

    // 密藥必須有 elixir：倍率 1.01～3.00、秒數 60～86 400、kind 是 BuffKind；非密藥不得有 elixir
    [Theory]
    [InlineData("\"multiplier\": 1.5", "\"multiplier\": 1", "multiplier")]
    [InlineData("\"multiplier\": 1.5", "\"multiplier\": 3.01", "multiplier")]
    [InlineData("\"seconds\": 3600", "\"seconds\": 59", "seconds")]
    [InlineData("\"seconds\": 3600", "\"seconds\": 86401", "seconds")]
    [InlineData("\"kind\": \"Exp\"", "\"kind\": \"Luck\"", "kind")]
    [InlineData(", \"elixir\": { \"kind\": \"Exp\", \"multiplier\": 1.5, \"seconds\": 3600 }", "", "elixir")]
    [InlineData("\"consumable\": \"Elixir\"", "\"consumable\": \"StatPotion\", \"bonus\": { \"str\": 1 }", "elixir")]
    public void Items_RejectElixirMultiplierOutOfRange(string validField, string invalidField, string expected)
    {
        var message = ItemRejection($"{ValidItemList}, {Replace(ValidElixir, validField, invalidField)}");
        Assert.Contains("brew", message);
        Assert.Contains(expected, message);
    }

    // 欄位是 DECIMAL(4,2)：小數超過兩位存不進去，載入時就擋
    [Fact]
    public void Items_RejectElixirMultiplierWithMoreThanTwoDecimals()
    {
        var message = ItemRejection($"{ValidItemList}, {Replace(ValidElixir, "\"multiplier\": 1.5", "\"multiplier\": 1.255")}");

        Assert.Contains("brew", message);
        Assert.Contains("小數最多兩位", message);
    }

    [Fact]
    public void Items_LoadElixirSpec()
    {
        var content = JsonGameContent.Load(TestContentDirectory);
        Assert.Equal(new ElixirSpec(BuffKind.Exp, 1.5, 3600), content.GetItem("test-exp-elixir").Elixir);
        Assert.Null(content.GetItem("test-supply").Elixir);
    }

    // 範圍兩端都可載入：倍率 1.01 與 3.00、秒數 60 與 86 400（剛好超出的見 Items_RejectElixirMultiplierOutOfRange）
    [Theory]
    [InlineData(1.01, 60)]
    [InlineData(3.00, 86400)]
    [InlineData(1.01, 86400)]
    [InlineData(3.00, 60)]
    public void Items_AcceptElixirBounds(double multiplier, int seconds)
    {
        var elixir = ValidElixir
            .Replace("\"multiplier\": 1.5", $"\"multiplier\": {multiplier.ToString("0.00", CultureInfo.InvariantCulture)}")
            .Replace("\"seconds\": 3600", $"\"seconds\": {seconds}");
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), ValidMonsters);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), ValidJobs);
            File.WriteAllText(Path.Combine(directory.FullName, "skills.json"), ValidSkills);
            File.WriteAllText(Path.Combine(directory.FullName, "world.json"), ValidWorld);
            File.WriteAllText(Path.Combine(directory.FullName, "items.json"), $$"""{ "items": [{{ValidItemList}}, {{elixir}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "recipes.json"), $$"""{ "recipes": [{{ValidRecipe}}] }""");
            File.WriteAllText(Path.Combine(directory.FullName, "missions.json"), ValidMissions);
            WriteCompanionContent(directory.FullName);
            Assert.Equal(
                new ElixirSpec(BuffKind.Exp, multiplier, seconds), JsonGameContent.Load(directory.FullName).GetItem("brew").Elixir);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
