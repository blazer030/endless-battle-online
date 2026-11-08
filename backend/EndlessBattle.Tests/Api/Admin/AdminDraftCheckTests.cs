using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminDraftCheckTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Draft_NoDraftReportsNotExisting()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var status = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/draft", CancellationToken);

        Assert.False(status.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task Draft_ValidEditReportsValid()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        monster!["level"] = 3;

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.True(status.GetProperty("exists").GetBoolean());
        Assert.True(status.GetProperty("valid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, status.GetProperty("error").ValueKind);
    }

    [Fact]
    public async Task Draft_InvalidWriteIsSavedAndReportsTheLoaderError()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        monster!["placeId"] = 9;

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.False(status.GetProperty("valid").GetBoolean());
        Assert.Equal("怪物 test-sprite 引用了不存在的地區：9", status.GetProperty("error").GetString());
        var saved = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        Assert.Equal(9, saved!["placeId"]!.GetValue<int>());
        var draft = await client.GetFromJsonAsync<JsonElement>("/admin/draft", CancellationToken);
        Assert.Equal("怪物 test-sprite 引用了不存在的地區：9", draft.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Draft_CreateReportsTheDraftStatus()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        monster!["id"] = "test-copy";

        var response = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);

        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.True(status.GetProperty("valid").GetBoolean());
    }
}
