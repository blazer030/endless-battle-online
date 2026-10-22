using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// 新戰士 2000 錢；TestContent 的 test-mercenary 100 錢、3 次，地區 1 測試平原
public class MercenaryEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"僱主{Random.Shared.Next(100000, 999999)}"));
        created.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Mercenaries_HireDeployAndAdvance()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var catalogue = await ReadOkAsync(await client.GetAsync("/api/mercenaries", CancellationToken));
        Assert.Equal(JsonValueKind.Null, catalogue.GetProperty("contract").ValueKind);
        var honour = catalogue.GetProperty("catalogue").EnumerateArray().Single(entry => entry.GetProperty("id").GetString() == "test-honour-mercenary");
        Assert.Equal(JsonValueKind.Null, honour.GetProperty("price").ValueKind);

        var hired = await ReadOkAsync(await client.PostAsJsonAsync("/api/mercenaries/hire", new { mercenaryId = "test-mercenary" }, CancellationToken));
        Assert.Equal(1900, hired.GetProperty("character").GetProperty("money").GetInt64());
        var contract = hired.GetProperty("mercenaries").GetProperty("contract");
        Assert.Equal(("測試傭兵", 3, false), (contract.GetProperty("name").GetString(), contract.GetProperty("remaining").GetInt32(), contract.GetProperty("fighting").GetBoolean()));

        var deployed = (await ReadOkAsync(await client.PostAsJsonAsync("/api/mercenaries/deploy", new { placeId = 1 }, CancellationToken))).GetProperty("contract");
        Assert.Equal(("測試平原", true), (deployed.GetProperty("placeName").GetString(), deployed.GetProperty("fighting").GetBoolean()));
        Assert.Equal(factory.Clock.UtcNow.AddSeconds(100), deployed.GetProperty("nextFightAt").GetDateTimeOffset());

        factory.Clock.Advance(TimeSpan.FromSeconds(250));
        var advanced = await ReadOkAsync(await client.PostAsync("/api/adventure/advance", null, CancellationToken));

        var report = advanced.GetProperty("mercenary");
        Assert.True(report.GetProperty("fights").GetInt32() >= 1);
        Assert.Equal("測試傭兵", report.GetProperty("name").GetString());
        var recent = report.GetProperty("recent").EnumerateArray().ToList();
        Assert.Equal(report.GetProperty("fights").GetInt32(), recent.Count);
        Assert.All(recent, fight => Assert.Contains(fight.GetProperty("monsterName").GetString(), new[] { "測試雪精", "測試寒狼" }));
    }

    [Fact]
    public async Task Mercenaries_SecondHireReturnsReason()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await ReadOkAsync(await client.PostAsJsonAsync("/api/mercenaries/hire", new { mercenaryId = "test-mercenary" }, CancellationToken));

        var again = await client.PostAsJsonAsync("/api/mercenaries/hire", new { mercenaryId = "test-mercenary" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["mercenaryHired"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Mercenaries_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/mercenaries", CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/mercenaries/hire", new { mercenaryId = "test-mercenary" }, CancellationToken)).StatusCode);
    }
}
