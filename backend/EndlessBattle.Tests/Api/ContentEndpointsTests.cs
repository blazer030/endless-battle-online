using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class ContentEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetContent_ReturnsMonsterAndStageNames_WithoutLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/content", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(CancellationToken);
        Assert.DoesNotContain("\"maxHealth\"", text);

        var content = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal(
            ["test-sprite:測試雪精", "test-wolf:測試寒狼"],
            content.GetProperty("monsters").EnumerateArray().Select(item => $"{item.GetProperty("id").GetString()}:{item.GetProperty("name").GetString()}"));
        Assert.Equal(
            ["test-stage-1:測試關卡・一"],
            content.GetProperty("stages").EnumerateArray().Select(item => $"{item.GetProperty("id").GetString()}:{item.GetProperty("name").GetString()}"));
    }

    [Fact]
    public async Task GetContent_IncludesJobsAndEnums()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var content = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/content", CancellationToken);

        var jobs = content.GetProperty("jobs").EnumerateArray().ToList();
        Assert.Equal(["warrior", "sorcerer", "berserker"], jobs.Select(job => job.GetProperty("id").GetString()));
        var berserker = jobs[2];
        Assert.Equal(2, berserker.GetProperty("tier").GetInt32());
        Assert.Equal("warrior", berserker.GetProperty("parentId").GetString());
        Assert.Equal(30, berserker.GetProperty("unlockLevel").GetInt32());
        var requirement = Assert.Single(berserker.GetProperty("statRequirements").EnumerateArray());
        Assert.Equal("str", requirement.GetProperty("stat").GetString());
        Assert.Equal(60, requirement.GetProperty("minimum").GetInt32());

        var elements = content.GetProperty("elements").EnumerateArray().ToList();
        Assert.Equal(6, elements.Count);
        Assert.Equal("3", elements[2].GetProperty("id").GetString());
        Assert.Equal("火", elements[2].GetProperty("name").GetString());
        Assert.Equal(4, content.GetProperty("births").GetArrayLength());
        Assert.Equal(2, content.GetProperty("sexes").GetArrayLength());
    }
}
