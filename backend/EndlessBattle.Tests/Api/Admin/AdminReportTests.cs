using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminReportTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static int LevelAt(JsonElement report, int placeId) =>
        report.GetProperty("adventures").EnumerateArray()
            .First(row => row.GetProperty("placeId").GetInt32() == placeId)
            .GetProperty("level").GetInt32();

    [Fact]
    public async Task Report_DraftIsComputedFromTheDraftContent()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var cave = await client.GetFromJsonAsync<JsonObject>("/admin/content/places/2", CancellationToken);
        cave!["recommendedLevel"] = new JsonObject { ["minimum"] = 7, ["maximum"] = 15 };
        (await client.PutAsJsonAsync("/admin/content/places/2", cave, CancellationToken)).EnsureSuccessStatusCode();

        var official = await client.PostAsJsonAsync("/admin/reports", new { source = "official" }, CancellationToken);
        var draft = await client.PostAsJsonAsync("/admin/reports", new { source = "draft", skillLevel = "first" }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, official.StatusCode);
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        var officialReport = await official.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var draftReport = await draft.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(6, LevelAt(officialReport, 2));
        Assert.Equal(7, LevelAt(draftReport, 2));
        Assert.Equal("First", draftReport.GetProperty("assumptions").GetProperty("skillLevel").GetString());
        Assert.Equal("Max", officialReport.GetProperty("assumptions").GetProperty("skillLevel").GetString());
    }

    [Fact]
    public async Task Report_DraftWithoutADraftReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await directories.CreateClient(factory).PostAsJsonAsync("/admin/reports", new { source = "draft" }, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Report_InvalidDraftReturnsConflictWithTheLoaderError()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        monster!["placeId"] = 9;
        (await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/admin/reports", new { source = "draft" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("怪物 test-sprite 引用了不存在的地區：9", body.GetProperty("error").GetString());
    }
}
