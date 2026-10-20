using Inkbound.Infrastructure.Content;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Infrastructure;

public class JsonGameContentTests
{
    private const string ValidMonster =
        """{ "id": "sprite", "name": "雪精", "stats": { "maxHealth": 40, "attack": 6, "defense": 2, "critChance": 0, "critMultiplier": 1.5, "attacksPerSecond": 0.8 }, "experienceReward": 5, "spiritStoneReward": 2 }""";

    public static TheoryData<string, string, string> InvalidContent => new()
    {
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["ghost-monster"] }] }""", "ghost-monster" },
        { $$"""{ "monsters": [{{ValidMonster}}, {{ValidMonster}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "missing-stage", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "missing-stage" },
        { $$"""{ "monsters": [{{ValidMonster.Replace("\"attacksPerSecond\": 0.8", "\"attacksPerSecond\": 0")}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster.Replace("\"maxHealth\": 40", "\"maxHealth\": 0")}}] }""", """{ "startingStageId": "s1", "stages": [{ "id": "s1", "name": "一", "monsterIds": ["sprite"] }] }""", "sprite" },
        { $$"""{ "monsters": [{{ValidMonster}}] }""", """{ "startingStageId": "empty-stage", "stages": [{ "id": "empty-stage", "name": "空", "monsterIds": [] }] }""", "empty-stage" },
    };

    [Fact]
    public void Load_ShippedContent_IsValid()
    {
        var content = JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));
        var startingStage = content.GetStage(content.StartingStageId);
        Assert.NotEmpty(startingStage.MonsterIds);
        Assert.All(startingStage.MonsterIds, monsterId => Assert.Equal(monsterId, content.GetMonster(monsterId).Id));
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
        var directory = Directory.CreateTempSubdirectory("inkbound-content-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "monsters.json"), monstersJson);
            File.WriteAllText(Path.Combine(directory.FullName, "stages.json"), stagesJson);
            var exception = Assert.Throws<GameContentException>(() => JsonGameContent.Load(directory.FullName));
            Assert.Contains(offendingId, exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetMonster_UnknownId_Throws()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        var exception = Assert.Throws<GameContentException>(() => content.GetMonster("nobody"));
        Assert.Contains("nobody", exception.Message);
    }
}
