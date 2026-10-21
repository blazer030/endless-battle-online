using EndlessBattle.Domain.Content;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class JsonGameContentTests
{
    private const string ValidMonster =
        """{ "id": "sprite", "name": "雪精", "stats": { "maxHealth": 40, "attack": 6, "defense": 2, "critChance": 0, "critMultiplier": 1.5, "attacksPerSecond": 0.8 }, "experienceReward": 5, "spiritStoneReward": 2 }""";

    private const string ValidMonsters = $$"""{ "monsters": [{{ValidMonster}}] }""";

    private const string ValidStages = """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""";

    private const string ValidGrowth =
        """{ "str": [2, 3], "smart": [0, 1], "life": [1, 2], "vit": [1, 2], "agi": [1, 2], "au": [0, 1], "be": [0, 1] }""";

    private const string ValidWarrior =
        $$"""{ "id": "warrior", "name": "戰士", "tier": 1, "parentId": null, "unlockLevel": 1, "proficiencyRequired": 0, "statRequirements": [], "growth": {{ValidGrowth}} }""";

    private const string ValidJobs = $$"""{ "jobs": [{{ValidWarrior}}] }""";

    public static TheoryData<string, string, string> InvalidContent => new()
    {
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["ghost-monster"] }] }""", "ghost-monster" },
        { $$"""{ "monsters": [{{ValidMonster}}, {{ValidMonster}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "missing-stage", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "missing-stage" },
        { $$"""{ "monsters": [{{ValidMonster.Replace("\"attacksPerSecond\": 0.8", "\"attacksPerSecond\": 0")}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster.Replace("\"maxHealth\": 40", "\"maxHealth\": 0")}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "empty-stage", "stages": [{ "id": "empty-stage", "name": "空", "monsterIds": [] }] }""", "empty-stage" },
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
        var content = JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));
        var startingStage = content.GetStage(content.StartingStageId);
        Assert.NotEmpty(startingStage.MonsterIds);
        Assert.All(startingStage.MonsterIds, monsterId => Assert.Equal(monsterId, content.GetMonster(monsterId).Id));
        Assert.Equal(12, content.AllJobs.Count);
        Assert.All(
            content.AllJobs.Where(job => job.Tier == 2),
            job => Assert.Equal(1, content.GetJob(job.ParentId!).Tier));
    }

    [Fact]
    public void Load_TestContent_ExposesJobs()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal(3, content.AllJobs.Count);
        var berserker = content.GetJob("berserker");
        Assert.Equal("warrior", berserker.ParentId);
        Assert.Equal(new StatRequirement("str", 60), berserker.StatRequirements[0]);
        Assert.Equal(new StatRange(3, 4), berserker.Growth.Str);
    }

    [Fact]
    public void Load_TestContent_ExposesStagesAndMonsters()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        Assert.Equal("test-stage-1", content.StartingStageId);
        Assert.Equal(["test-sprite", "test-wolf"], content.GetStage("test-stage-1").MonsterIds);
        Assert.Equal(60, content.GetMonster("test-wolf").Stats.MaxHealth);
        Assert.Equal("測試寒狼", content.GetMonster("test-wolf").Name);
    }

    [Theory]
    [MemberData(nameof(InvalidContent))]
    public void Load_Rejects_InvalidContent(string monstersJson, string stagesJson, string offendingId)
    {
        AssertLoadRejects(monstersJson, stagesJson, ValidJobs, offendingId);
    }

    [Theory]
    [MemberData(nameof(InvalidJobs))]
    public void Load_Rejects_InvalidJobs(string jobsJson, string offendingId)
    {
        AssertLoadRejects(ValidMonsters, ValidStages, jobsJson, offendingId);
    }

    [Fact]
    public void GetMonster_UnknownId_Throws()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        var exception = Assert.Throws<GameContentException>(() => content.GetMonster("nobody"));
        Assert.Contains("nobody", exception.Message);
    }

    private static void AssertLoadRejects(string monstersJson, string stagesJson, string jobsJson, string offendingId)
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), monstersJson);
            File.WriteAllText(Path.Combine(directory.FullName, "stages.json"), stagesJson);
            File.WriteAllText(Path.Combine(directory.FullName, "jobs.json"), jobsJson);
            var exception = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName));
            Assert.Contains(offendingId, exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
