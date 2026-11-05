using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminContentReadTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("items", 31, "test-sword")]
    [InlineData("monsters", 6, "test-sprite")]
    [InlineData("jobs", 7, "warrior")]
    [InlineData("skills", 8, "burst")]
    [InlineData("areas", 2, "central")]
    [InlineData("places", 4, "1")]
    public async Task List_ReturnsEveryRecordOfTheKind(string kind, int count, string firstId)
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var records = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>($"/admin/content/{kind}", CancellationToken);

        Assert.Equal(count, records.GetArrayLength());
        Assert.Equal(firstId, records[0].GetProperty("id").ToString());
    }

    [Fact]
    public async Task List_UnsupportedKindReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await directories.CreateClient(factory).GetAsync("/admin/content/recipes", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsTheRecordWithAllFields()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var record = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/places/2", CancellationToken);

        Assert.Equal(2, record.GetProperty("id").GetInt32());
        Assert.True(record.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Get_MissingRecordReturnsNotFound()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await directories.CreateClient(factory).GetAsync("/admin/content/items/no-such-item", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
