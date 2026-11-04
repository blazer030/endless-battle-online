using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminDraftPublishTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task EditSpriteAsync(HttpClient client, Action<JsonObject> change)
    {
        var monster = await client.GetFromJsonAsync<JsonObject>("/admin/content/monsters/test-sprite", CancellationToken);
        change(monster!);
        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Publish_ValidDraftWritesBackAndRemovesTheDraft()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        await EditSpriteAsync(client, monster => monster["level"] = 3);
        var draftMonsters = await File.ReadAllTextAsync(Path.Combine(directories.Draft, "monsters.json"), CancellationToken);

        var response = await client.PostAsync("/admin/draft/publish", null, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.True(body.GetProperty("restartRequired").GetBoolean());
        Assert.Equal(draftMonsters, await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken));
        Assert.False(Directory.Exists(directories.Draft));
        var status = await client.GetFromJsonAsync<JsonElement>("/admin/draft", CancellationToken);
        Assert.False(status.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task Publish_InvalidDraftIsRefusedWithTheLoaderError()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var officialMonsters = await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken);
        await EditSpriteAsync(client, monster => monster["placeId"] = 9);

        var response = await client.PostAsync("/admin/draft/publish", null, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("invalidDraft", body.GetProperty("reason").GetString());
        Assert.Equal("怪物 test-sprite 引用了不存在的地區：9", body.GetProperty("error").GetString());
        Assert.Equal(officialMonsters, await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken));
        Assert.True(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Publish_OfficialContentChangedAfterTheDraftIsRefused()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        await EditSpriteAsync(client, monster => monster["level"] = 3);
        var officialItemsPath = Path.Combine(directories.Official, "items.json");
        var changedItems = await File.ReadAllTextAsync(officialItemsPath, CancellationToken) + "\n";
        await File.WriteAllTextAsync(officialItemsPath, changedItems, CancellationToken);
        var officialMonsters = await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken);

        var response = await client.PostAsync("/admin/draft/publish", null, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("officialChanged", body.GetProperty("reason").GetString());
        Assert.Equal(["items.json"], body.GetProperty("changedFiles").EnumerateArray().Select(file => file.GetString()));
        Assert.Equal(changedItems, await File.ReadAllTextAsync(officialItemsPath, CancellationToken));
        Assert.Equal(officialMonsters, await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken));
        Assert.True(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Publish_WithoutDraftReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await directories.CreateClient(factory).PostAsync("/admin/draft/publish", null, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Discard_RemovesTheDraftAndLeavesOfficialUntouched()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var officialMonsters = await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken);
        await EditSpriteAsync(client, monster => monster["level"] = 3);

        var response = await client.DeleteAsync("/admin/draft", CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
        Assert.Equal(officialMonsters, await File.ReadAllTextAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken));
    }

    [Fact]
    public async Task Discard_WithoutDraftReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await directories.CreateClient(factory).DeleteAsync("/admin/draft", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
