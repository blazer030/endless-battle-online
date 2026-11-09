using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Api.Auth;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class PublicEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PublicSettings_RespondsWithoutToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/public/settings", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["1:男", "2:女"], Options(settings, "sexes"));
        Assert.Equal(["1:地", "2:水", "3:火", "4:木", "5:風", "6:毒"], Options(settings, "elements"));
        Assert.Equal(["1:中央大陸", "2:魔法王國", "3:熱帶雨林", "4:末日王城"], Options(settings, "births"));
    }

    [Fact]
    public async Task PublicSettings_IgnoresExpiredToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var anonymous = await factory.CreateClient().GetStringAsync("/api/public/settings", CancellationToken);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ExpiredToken());

        // 先確認這張 token 真的被拒絕
        var protectedResponse = await client.GetAsync("/api/accounts/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);

        var response = await client.GetAsync("/api/public/settings", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(anonymous, await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task PublicSettings_FireIsStrongAgainstWoodAndWeakAgainstWater()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var settings = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/settings", CancellationToken);

        var fire = settings.GetProperty("elements").EnumerateArray().Single(element => element.GetProperty("id").GetInt32() == 3);
        Assert.Equal("火", fire.GetProperty("name").GetString());
        Assert.Equal(4, fire.GetProperty("strongAgainst").GetInt32());
        Assert.Equal(2, fire.GetProperty("weakAgainst").GetInt32());
    }

    [Fact]
    public async Task PublicSettings_ElementAdvantageFollowsTheCycle()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var settings = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/settings", CancellationToken);

        // 地 → 水 → 火 → 木 → 風 → 毒 → 地：前者剋後者
        Assert.Equal(
            ["1>2<6", "2>3<1", "3>4<2", "4>5<3", "5>6<4", "6>1<5"],
            settings.GetProperty("elements").EnumerateArray().Select(element =>
                $"{element.GetProperty("id").GetInt32()}>{element.GetProperty("strongAgainst").GetInt32()}<{element.GetProperty("weakAgainst").GetInt32()}"));
    }

    private static IEnumerable<string> Options(JsonElement settings, string name) =>
        settings.GetProperty(name).EnumerateArray().Select(option => $"{option.GetProperty("id").GetInt32()}:{option.GetProperty("name").GetString()}");

    // 用測試簽章金鑰簽一張 60 天前發的 token（有效期 30 天，已過期）
    private static string ExpiredToken()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions { SigningKey = "test-only-signing-key-0123456789abcdef0123" });
        return new JwtTokenIssuer(options, new ShiftedTimeProvider(TimeSpan.FromDays(-60))).Issue(1);
    }

    private sealed class ShiftedTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + offset;
    }
}
