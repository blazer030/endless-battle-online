using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// TestContent 的種子冠軍：測試冠軍 Lv5、HP 120、SP 20、at 20
public class ChampionEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Champion_GetAndChallenge()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"冠軍{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();

        var champion = await ReadOkAsync(await client.GetAsync("/api/champion", CancellationToken));

        Assert.Equal(("測試冠軍", 5, 120, 120, 20, false, 0, 500L), (
            champion.GetProperty("name").GetString(), champion.GetProperty("level").GetInt32(), champion.GetProperty("hp").GetInt32(),
            champion.GetProperty("hpMax").GetInt32(), champion.GetProperty("at").GetInt32(), champion.GetProperty("isPlayer").GetBoolean(),
            champion.GetProperty("defenses").GetInt32(), champion.GetProperty("fee").GetInt64()));
        Assert.Equal(JsonValueKind.Null, champion.GetProperty("lockedUntil").ValueKind);
        Assert.Empty(champion.GetProperty("reasons").EnumerateArray());

        var challenge = await ReadOkAsync(await client.PostAsync("/api/champion/challenge", null, CancellationToken));

        var events = challenge.GetProperty("events").EnumerateArray().ToList();
        Assert.NotEmpty(events);
        Assert.Equal("round", events[0].GetProperty("type").GetString());
        Assert.Equal(1500, challenge.GetProperty("character").GetProperty("money").GetInt64());
        var won = challenge.GetProperty("won").GetBoolean();
        Assert.Equal(won, challenge.GetProperty("champion").GetProperty("isPlayer").GetBoolean());
        Assert.Equal(won ? 0 : 1, challenge.GetProperty("champion").GetProperty("defenses").GetInt32());
    }

    [Fact]
    public async Task Champion_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/champion", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/champion/challenge", null, CancellationToken)).StatusCode);
    }
}
