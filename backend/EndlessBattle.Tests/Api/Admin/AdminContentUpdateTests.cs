using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Application.Content;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminContentUpdateTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OfficialContent => Path.Combine(BackendPaths.BackendDirectory, "content");

    [Fact]
    public async Task Draft_FirstEditCopiesContentAndLeavesOfficialUntouched()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var host = directories.Configure(factory);
        var client = host.CreateClient();
        var officialItems = await File.ReadAllTextAsync(Path.Combine(directories.Official, "items.json"), CancellationToken);
        var cloth = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/item-1", CancellationToken);
        Assert.Equal(6, cloth!["price"]!.GetValue<int>());
        cloth["price"] = 9;

        var response = await client.PutAsJsonAsync("/admin/content/items/item-1", cloth, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        Assert.Equal(
            Directory.GetFiles(directories.Official, "*.json").Select(Path.GetFileName).Order(),
            Directory.GetFiles(directories.Draft, "*.json").Select(Path.GetFileName).Where(name => name != ".base.json").Order());
        Assert.Equal(officialItems, await File.ReadAllTextAsync(Path.Combine(directories.Official, "items.json"), CancellationToken));
        var changed = DifferentLines(officialItems, await File.ReadAllTextAsync(Path.Combine(directories.Draft, "items.json"), CancellationToken));
        var line = Assert.Single(changed);
        Assert.Contains("\"id\": \"item-1\"", line);
        Assert.Contains("\"price\": 9", line);
        Assert.Equal(6, host.Services.GetRequiredService<IGameContent>().GetItem("item-1").Price);
        var reread = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/item-1", CancellationToken);
        Assert.Equal(9, reread!["price"]!.GetValue<int>());
    }

    [Fact]
    public async Task Update_IdDifferentFromPathReturnsBadRequest()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", new JsonObject { ["id"] = "test-wolf" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Update_MissingRecordReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);

        var response = await client.PutAsJsonAsync("/admin/content/monsters/no-such", new JsonObject { ["id"] = "no-such" }, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }

    // 逐行比對，回傳草稿與正式內容不同的那幾行（草稿那一側）
    private static IReadOnlyList<string> DifferentLines(string official, string draft)
    {
        var before = official.Split('\n');
        var after = draft.Split('\n');
        Assert.Equal(before.Length, after.Length);
        return after.Where((line, index) => line != before[index]).ToList();
    }
}
