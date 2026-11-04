using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminContentCreateTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_AppendsTheRecordAfterTheExistingOnes()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        monster!["id"] = "test-new-monster";
        monster["name"] = "新怪物";

        var response = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var monsters = await client.GetFromJsonAsync<JsonArray>("/admin/content/monsters", CancellationToken);
        Assert.Equal(7, monsters!.Count);
        Assert.Equal("test-new-monster", monsters[^1]!["id"]!.GetValue<string>());
        Assert.Equal("test-sprite", monsters[0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task Create_ExistingIdReturnsConflictAndLeavesTheDraftUnchanged()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var sword = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/test-sword", CancellationToken);

        var response = await client.PostAsJsonAsync("/admin/content/items", sword, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Create_WithoutIdReturnsBadRequest()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);

        var response = await client.PostAsJsonAsync("/admin/content/items", new JsonObject { ["name"] = "無名" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }
}
