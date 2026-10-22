using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// API 主機的時鐘停在 2026-01-01T00:00Z（台北 08:00，週期 2025-12-31），下一次每日重置是 01:00Z
public class MissionEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"任務{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string action, string missionId) =>
        client.PostAsJsonAsync($"/api/missions/{action}", new { missionId }, CancellationToken);

    private static List<string?> Ids(JsonElement board, string list) =>
        board.GetProperty(list).EnumerateArray().Select(mission => mission.GetProperty("id").GetString()).ToList();

    [Fact]
    public async Task Missions_GetBoard()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var board = await ReadAsync(await client.GetAsync("/api/missions", CancellationToken));

        Assert.Contains("test-hunt", Ids(board, "available"));
        Assert.Contains("test-daily", Ids(board, "available"));
        Assert.Empty(Ids(board, "active"));
        Assert.Equal(3, board.GetProperty("activeLimit").GetInt32());
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero), board.GetProperty("nextDailyReset").GetDateTimeOffset());
        var hunt = board.GetProperty("available").EnumerateArray().Single(mission => mission.GetProperty("id").GetString() == "test-hunt");
        Assert.Equal("測試平原", hunt.GetProperty("placeName").GetString());
        var kill = hunt.GetProperty("kill");
        Assert.Equal(("測試雪精", "測試平原", 3, 0), (
            kill.GetProperty("monsterName").GetString(), kill.GetProperty("placeName").GetString(),
            kill.GetProperty("count").GetInt32(), kill.GetProperty("progress").GetInt32()));
        Assert.Equal(190, hunt.GetProperty("rewards").GetProperty("exp").GetInt64());
    }

    [Fact]
    public async Task Missions_AcceptThenAbandon()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var accepted = await ReadAsync(await PostAsync(client, "accept", "test-delivery"));

        Assert.Equal(["test-delivery"], Ids(accepted, "active"));
        Assert.DoesNotContain("test-delivery", Ids(accepted, "available"));
        var active = accepted.GetProperty("active")[0];
        Assert.False(active.GetProperty("ready").GetBoolean());
        var deliver = Assert.Single(active.GetProperty("deliver").EnumerateArray());
        Assert.Equal(("test-ore", 3, 0), (deliver.GetProperty("itemId").GetString(), deliver.GetProperty("quantity").GetInt32(), deliver.GetProperty("owned").GetInt32()));

        var abandoned = await ReadAsync(await PostAsync(client, "abandon", "test-delivery"));

        Assert.Empty(Ids(abandoned, "active"));
        Assert.Contains("test-delivery", Ids(abandoned, "available"));
    }

    [Fact]
    public async Task Missions_FourthAcceptReturnsTooMany()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        foreach (var missionId in new[] { "test-hunt", "test-sprite-bounty", "test-delivery" })
        {
            (await PostAsync(client, "accept", missionId)).EnsureSuccessStatusCode();
        }

        var response = await PostAsync(client, "accept", "test-chief-hunt");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["tooMany"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Missions_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/missions", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
