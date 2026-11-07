using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminMissionTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Missions_ListIdTitleAndDaily()
    {
        using var directories = new AdminContentDirectories(Path.Combine(BackendPaths.BackendDirectory, "content"));
        await using var factory = new EndlessBattleApiFactory(database);

        var missions = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/missions", CancellationToken);

        Assert.Equal(344, missions.GetArrayLength());
        var first = missions[0];
        Assert.Equal("mission-1", first.GetProperty("id").GetString());
        Assert.Equal("製作漁網", first.GetProperty("title").GetString());
        Assert.False(first.GetProperty("daily").GetBoolean());
        Assert.Equal(3, first.EnumerateObject().Count());
        var daily = missions.EnumerateArray().Single(mission => mission.GetProperty("id").GetString() == "mission-222");
        Assert.True(daily.GetProperty("daily").GetBoolean());
    }

    [Fact]
    public async Task Missions_ReadTheDraftWhenThereIsOne()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        (await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken)).EnsureSuccessStatusCode();
        var path = Path.Combine(directories.Draft, "missions.json");
        await File.WriteAllTextAsync(path, (await File.ReadAllTextAsync(path, CancellationToken)).Replace("測試討伐", "草稿討伐", StringComparison.Ordinal), CancellationToken);

        var missions = await client.GetFromJsonAsync<JsonElement>("/admin/content/missions", CancellationToken);

        Assert.Contains(missions.EnumerateArray(), mission => mission.GetProperty("title").GetString() == "草稿討伐");
    }

    [Fact]
    public async Task Missions_CannotBeWritten()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var body = new JsonObject { ["id"] = "test-kill", ["title"] = "改寫", ["daily"] = false };

        var put = await client.PutAsJsonAsync("/admin/content/missions/test-kill", body, CancellationToken);
        var post = await client.PostAsJsonAsync("/admin/content/missions", body, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }
}
