using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminItemTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OfficialContent => Path.Combine(BackendPaths.BackendDirectory, "content");

    [Fact]
    public async Task Items_UseCompleteFieldNames()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var item = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/items/item-1823", CancellationToken);

        Assert.Equal("Weapon", item.GetProperty("category").GetString());
        Assert.Equal(650, item.GetProperty("bonus").GetProperty("attack").GetInt32());
        Assert.Equal(25, item.GetProperty("bonus").GetProperty("strength").GetInt32());
        Assert.Equal(590, item.GetProperty("requires").GetProperty("strength").GetInt32());
        Assert.Equal("Fire", item.GetProperty("element").GetString());
        Assert.Equal(12, item.GetProperty("weaponSkill").GetProperty("activationRate").GetInt32());
        Assert.True(item.GetProperty("bound").GetBoolean());
        Assert.False(item.TryGetProperty("key", out _));
    }

    [Theory]
    [InlineData("item-2255", "elixir", "kind", "Exp")]
    [InlineData("item-1338", "key", "placeId", "17")]
    [InlineData("item-1863", "refine", "kind", "1")]
    public async Task Items_ExposeTheSectionOfTheirCategory(string id, string section, string field, string expected)
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var item = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>($"/admin/content/items/{id}", CancellationToken);

        Assert.Equal(expected, item.GetProperty(section).GetProperty(field).ToString());
    }

    [Fact]
    public async Task Items_RejectUnknownCategory()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var item = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/test-sword", CancellationToken);
        item!["category"] = "Vehicle";

        var response = await client.PutAsJsonAsync("/admin/content/items/test-sword", item, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("category", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Items_SaveWritesOnlyThatItem()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var official = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "items.json"), CancellationToken);
        var item = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/item-1823", CancellationToken);
        item!["bonus"]!["attack"] = 700;

        var response = await client.PutAsJsonAsync("/admin/content/items/item-1823", item, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        var draft = await File.ReadAllLinesAsync(Path.Combine(directories.Draft, "items.json"), CancellationToken);
        var changed = Assert.Single(draft.Where((line, index) => line != official[index]));
        Assert.Contains("\"at\": 700", changed);
    }

    [Fact]
    public async Task Items_SectionOfAnotherCategoryIsSavedAndReported()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var item = await client.GetFromJsonAsync<JsonObject>("/admin/content/items/test-sword", CancellationToken);
        item!["key"] = new JsonObject { ["placeId"] = 1, ["seconds"] = 60 };

        var response = await client.PutAsJsonAsync("/admin/content/items/test-sword", item, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.False(status.GetProperty("valid").GetBoolean());
    }
}
