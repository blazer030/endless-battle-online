using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class AdventureEndpointsTests(MySqlDatabaseFixture database)
{
    private const int StaminaMax = 50;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
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
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var body = await AdvanceAsync(client);

        var events = body.GetProperty("events").EnumerateArray().ToList();
        Assert.Equal("encounter", events[0].GetProperty("type").GetString());
        Assert.Contains(events, battleEvent => battleEvent.GetProperty("type").GetString() == "round");
        Assert.Contains(events, battleEvent => battleEvent.GetProperty("type").GetString() == "attack");
        Assert.True(body.GetProperty("eventsCoverAll").GetBoolean());
        var summary = body.GetProperty("summary");
        Assert.True(summary.GetProperty("fights").GetInt32() >= 1);
        Assert.Equal(
            events.Count(battleEvent => battleEvent.GetProperty("type").GetString() == "encounter"),
            summary.GetProperty("fights").GetInt32());

        var character = body.GetProperty("character");
        Assert.Equal(StaminaMax - summary.GetProperty("fights").GetInt32(), character.GetProperty("actNum").GetInt32());
        Assert.Equal(StaminaMax, character.GetProperty("actNumMax").GetInt32());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(character.ToString(), me.ToString());
    }

    [Fact]
    public async Task Advance_UsesCharacterStats()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var strong = await factory.CreateAuthenticatedClientAsync();
        var weak = await factory.CreateAuthenticatedClientAsync();
        (await strong.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"力士{Random.Shared.Next(100000, 999999)}", str: 7, smart: 1, life: 1, agi: 1))).EnsureSuccessStatusCode();
        (await weak.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"命士{Random.Shared.Next(100000, 999999)}", str: 1, smart: 1, life: 7, agi: 1))).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var strongBody = await AdvanceAsync(strong);
        var weakBody = await AdvanceAsync(weak);

        Assert.True(MaxPlayerDamage(strongBody) > MaxPlayerDamage(weakBody));
    }

    private static int MaxPlayerDamage(JsonElement body) => body.GetProperty("events").EnumerateArray()
        .Where(battleEvent => battleEvent.GetProperty("type").GetString() == "attack"
            && string.Equals(battleEvent.GetProperty("attacker").GetString(), "player", StringComparison.OrdinalIgnoreCase))
        .Max(battleEvent => battleEvent.GetProperty("damage").GetInt32());

    [Fact]
    public async Task Advance_ReturnsNothing_WhenClockIsBehindLastSettlement()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(-5));

        var body = await AdvanceAsync(client);

        Assert.Empty(body.GetProperty("events").EnumerateArray());
        Assert.Equal(0, body.GetProperty("summary").GetProperty("fights").GetInt32());
        Assert.Equal(0, body.GetProperty("summary").GetProperty("exp").GetInt64());
        Assert.Equal(StaminaMax, body.GetProperty("character").GetProperty("actNum").GetInt32());
    }

    // 離線 6 小時：上萬場一次算完；事件只含最後 30 秒開始的場次，結算到最後一場打完，馬上再推進是空的
    [Fact]
    public async Task Advance_SixHours_ReturnsSummaryQuickly()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromHours(6));

        var stopwatch = Stopwatch.StartNew();
        var body = await AdvanceAsync(client);
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"推進花了 {stopwatch.ElapsedMilliseconds} ms");
        const double totalSeconds = 6 * 3600;
        var events = body.GetProperty("events").EnumerateArray().ToList();
        Assert.NotEmpty(events);
        Assert.All(events, battleEvent => Assert.True(battleEvent.GetProperty("atSeconds").GetDouble() >= totalSeconds - 30));
        Assert.False(body.GetProperty("eventsCoverAll").GetBoolean());
        var summary = body.GetProperty("summary");
        Assert.True(summary.GetProperty("fights").GetInt32() > 1000);
        Assert.True(summary.GetProperty("levelsGained").GetInt32() > 0);
        Assert.True(body.GetProperty("character").GetProperty("level").GetInt32() > 1);

        var immediatelyAfter = await AdvanceAsync(client);
        Assert.Empty(immediatelyAfter.GetProperty("events").EnumerateArray());
    }

    [Fact]
    public async Task Advance_RecordsJobProficiency()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(60));

        var body = await AdvanceAsync(client);

        var jobExp = body.GetProperty("summary").GetProperty("jobExp").GetInt64();
        Assert.True(jobExp > 0);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        var proficiency = me.GetProperty("jobProficiency").EnumerateArray().Single();
        Assert.Equal("warrior", proficiency.GetProperty("jobId").GetString());
        Assert.Equal(jobExp, proficiency.GetProperty("proficiency").GetInt64());
        Assert.Equal(jobExp, proficiency.GetProperty("skillPoints").GetInt64());
    }

    [Fact]
    public async Task Advance_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/adventure/advance", null, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesPolymorphicEvents()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var document = await factory.CreateClient().GetStringAsync("/openapi/v1.json", CancellationToken);

        Assert.Contains("/api/adventure/advance", document);
        Assert.Contains("\"encounter\"", document);
        Assert.Contains("\"attack\"", document);
    }
}
