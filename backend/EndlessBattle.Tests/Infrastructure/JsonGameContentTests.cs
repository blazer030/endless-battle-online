using EndlessBattle.Domain.Content;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class JsonGameContentTests
{
    private const string ValidMonster =
        """{ "id": "sprite", "name": "雪精", "level": 3, "hp": 60, "sp": 10, "at": 14, "mat": 8, "df": 6, "mdf": 4, "agi": 9, "au": 6, "element": 2, "jobExp": 12, "meet": 0 }""";

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
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "empty-stage", "stages": [{ "id": "empty-stage", "name": "空", "monsterIds": [] }] }""", "empty-stage" },
    };

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
        Assert.Equal(
            new MonsterDefinition("test-wolf", "測試寒狼", Level: 5, Hp: 95, Sp: 10, At: 20, Mat: 6, Df: 9, Mdf: 5, Agi: 13, Au: 7, Element: 2, JobExp: 18, Meet: 30),
            content.GetMonster("test-wolf"));
        Assert.Equal(
            new MonsterDefinition("test-sprite", "測試雪精", Level: 3, Hp: 60, Sp: 10, At: 14, Mat: 8, Df: 6, Mdf: 4, Agi: 9, Au: 6, Element: 2, JobExp: 12, Meet: 0),
            content.GetMonster("test-sprite"));
    }

    [Fact]
    public void Load_ShippedContent_DescribesFrostPeakMonsters()
    {
        var content = JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));
        Assert.Equal(
            [
                new MonsterDefinition("snow-sprite", "雪精", Level: 3, Hp: 60, Sp: 10, At: 14, Mat: 8, Df: 6, Mdf: 4, Agi: 9, Au: 6, Element: 2, JobExp: 12, Meet: 0),
                new MonsterDefinition("frost-wolf", "寒狼", Level: 5, Hp: 95, Sp: 10, At: 20, Mat: 6, Df: 9, Mdf: 5, Agi: 13, Au: 7, Element: 2, JobExp: 18, Meet: 30),
            ],
            content.AllMonsters);
    }

    [Theory]
    [MemberData(nameof(InvalidContent))]
    public void Load_Rejects_InvalidContent(string monstersJson, string stagesJson, string offendingId)
    {
        AssertLoadRejects(monstersJson, stagesJson, ValidJobs, offendingId);
    }

    [Theory]
    [MemberData(nameof(InvalidMonsterStats))]
    public void Load_Rejects_MonsterStatsOutOfRange(string validField, string invalidField)
    {
        Assert.Contains(validField, ValidMonster);
        var monstersJson = $$"""{ "monsters": [{{ValidMonster.Replace(validField, invalidField)}}] }""";
        AssertLoadRejects(monstersJson, ValidStages, ValidJobs, "sprite");
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
