using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminWorldTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OfficialContent => Path.Combine(BackendPaths.BackendDirectory, "content");

    [Fact]
    public async Task Places_UseCompleteFieldNames()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var place = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/places/1", CancellationToken);

        Assert.Equal(1, place.GetProperty("id").GetInt32());
        Assert.Contains(place.GetProperty("subPlaceNames").EnumerateArray(), name => name.GetString() == "綠風草地");
        Assert.Equal(1, place.GetProperty("recommendedLevel").GetProperty("minimum").GetInt32());
        Assert.Equal(10, place.GetProperty("recommendedLevel").GetProperty("maximum").GetInt32());
        Assert.False(place.TryGetProperty("subPlaces", out _));
    }

    [Fact]
    public async Task Areas_ListPlaceIds()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var areas = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/areas", CancellationToken);

        Assert.Equal(5, areas.GetArrayLength());
        Assert.True(areas[0].GetProperty("placeIds").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Places_SavingKeepsAreasAndTheStartingPlace()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var official = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "world.json"), CancellationToken);
        var place = await client.GetFromJsonAsync<JsonObject>("/admin/content/places/1", CancellationToken);
        place!["recommendedLevel"]!["maximum"] = 12;

        var response = await client.PutAsJsonAsync("/admin/content/places/1", place, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        var draft = await File.ReadAllLinesAsync(Path.Combine(directories.Draft, "world.json"), CancellationToken);
        var changed = Assert.Single(draft.Where((line, index) => line != official[index]));
        Assert.Contains("\"recommended\": [1, 12]", changed);
    }

    [Fact]
    public async Task Places_NumericIdInPathMustMatchTheBody()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var place = await client.GetFromJsonAsync<JsonObject>("/admin/content/places/1", CancellationToken);

        var response = await client.PutAsJsonAsync("/admin/content/places/2", place, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Places_RejectNullSubPlaceName()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var place = await client.GetFromJsonAsync<JsonObject>("/admin/content/places/1", CancellationToken);
        place!["subPlaceNames"] = new JsonArray("甲地", null, "丙地", "丁地");

        var response = await client.PutAsJsonAsync("/admin/content/places/1", place, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("subPlaceNames[1]", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Areas_RejectStringPlaceId()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var area = await client.GetFromJsonAsync<JsonObject>("/admin/content/areas/central", CancellationToken);
        area!["placeIds"] = new JsonArray("1");

        var response = await client.PutAsJsonAsync("/admin/content/areas/central", area, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("placeIds[0]", error.GetProperty("field").GetString());
    }
}
