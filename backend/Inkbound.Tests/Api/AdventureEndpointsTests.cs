using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Api;

public class AdventureEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(InkboundApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody($"行者{Random.Shared.Next(100000, 999999)}"), CancellationToken);
        created.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> AdvanceAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/adventure/advance", null, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Advance_AfterTenSeconds_ReturnsEventsAndPersistsGains()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var body = await AdvanceAsync(client);

        var events = body.GetProperty("events").EnumerateArray().ToList();
        Assert.Equal("encounter", events[0].GetProperty("type").GetString());
        Assert.Contains(events, battleEvent => battleEvent.GetProperty("type").GetString() == "reward");
        var expGained = body.GetProperty("expGained").GetInt64();
        Assert.True(expGained > 0);
        Assert.Equal(expGained, body.GetProperty("character").GetProperty("exp").GetInt64());

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(expGained, me.GetProperty("exp").GetInt64());
    }

    [Fact]
    public async Task Advance_UsesCharacterStats()
    {
        await using var factory = new InkboundApiFactory(database);
        var strong = await factory.CreateAuthenticatedClientAsync();
        var weak = await factory.CreateAuthenticatedClientAsync();
        (await strong.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"力士{Random.Shared.Next(100000, 999999)}", str: 7, smart: 1, life: 1, agi: 1))).EnsureSuccessStatusCode();
        (await weak.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"命士{Random.Shared.Next(100000, 999999)}", str: 1, smart: 1, life: 7, agi: 1))).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var strongBody = await AdvanceAsync(strong);
        var weakBody = await AdvanceAsync(weak);

        Assert.True(MaxPlayerDamage(strongBody) > MaxPlayerDamage(weakBody));
        foreach (var (client, body) in new[] { (strong, strongBody), (weak, weakBody) })
        {
            var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
            Assert.Equal(body.GetProperty("expGained").GetInt64(), me.GetProperty("exp").GetInt64());
        }
    }

    private static int MaxPlayerDamage(JsonElement body) => body.GetProperty("events").EnumerateArray()
        .Where(battleEvent => battleEvent.GetProperty("type").GetString() == "attack"
            && string.Equals(battleEvent.GetProperty("attacker").GetString(), "player", StringComparison.OrdinalIgnoreCase))
        .Max(battleEvent => battleEvent.GetProperty("damage").GetInt32());

    [Fact]
    public async Task Advance_ReturnsNothing_WhenClockIsBehindLastSettlement()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(-5));

        var body = await AdvanceAsync(client);

        Assert.Empty(body.GetProperty("events").EnumerateArray());
        Assert.Equal(0, body.GetProperty("expGained").GetInt64());
        Assert.Equal(0, body.GetProperty("moneyGained").GetInt64());
        Assert.Equal(0, body.GetProperty("moneyGained").GetInt64());
    }

    [Fact]
    public async Task Advance_CapsSimulationAtFiveMinutes()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromHours(1));

        var capped = await AdvanceAsync(client);
        Assert.All(capped.GetProperty("events").EnumerateArray(), battleEvent => Assert.True(battleEvent.GetProperty("atSeconds").GetDouble() <= 360));

        var immediatelyAfter = await AdvanceAsync(client);
        Assert.Empty(immediatelyAfter.GetProperty("events").EnumerateArray());
    }

    [Fact]
    public async Task Advance_Returns404_WhenNoCharacter()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/adventure/advance", null, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesPolymorphicEvents()
    {
        await using var factory = new InkboundApiFactory(database);

        var document = await factory.CreateClient().GetStringAsync("/openapi/v1.json", CancellationToken);

        Assert.Contains("/api/adventure/advance", document);
        Assert.Contains("\"encounter\"", document);
        Assert.Contains("\"attack\"", document);
    }
}
