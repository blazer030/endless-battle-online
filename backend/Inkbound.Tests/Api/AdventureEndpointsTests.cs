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
        var created = await client.PostAsJsonAsync("/api/characters", new { name = $"行者{Random.Shared.Next(100000, 999999)}" }, CancellationToken);
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
        var experienceGained = body.GetProperty("experienceGained").GetInt64();
        Assert.True(experienceGained > 0);
        Assert.Equal(experienceGained, body.GetProperty("character").GetProperty("experience").GetInt64());

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(experienceGained, me.GetProperty("experience").GetInt64());
    }

    [Fact]
    public async Task Advance_ReturnsNothing_WhenClockIsBehindLastSettlement()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(-5));

        var body = await AdvanceAsync(client);

        Assert.Empty(body.GetProperty("events").EnumerateArray());
        Assert.Equal(0, body.GetProperty("experienceGained").GetInt64());
    }

    [Fact]
    public async Task Advance_CapsSimulationAtFiveMinutes()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromHours(1));

        var capped = await AdvanceAsync(client);
        Assert.All(capped.GetProperty("events").EnumerateArray(), battleEvent => Assert.True(battleEvent.GetProperty("atSeconds").GetDouble() <= 300));

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
