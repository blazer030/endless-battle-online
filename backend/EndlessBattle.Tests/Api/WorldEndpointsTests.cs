using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

// 新角色 Lv1、位於 1 測試平原・甲地
public class WorldEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"世界{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, int placeId, int? subPlace) =>
        client.PutAsJsonAsync("/api/world/location", new { placeId, subPlace }, CancellationToken);

    // 直接寫入鑰匙：取得鑰匙要靠勳章所，這裡跳過
    private static async Task GiveKeysAsync(EndlessBattleApiFactory factory, HttpClient client, int quantity)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        await scope.ServiceProvider.GetRequiredService<IInventoryRepository>()
            .AddAsync(character!.Id, TestItems.Entry(0, "test-key", quantity), CancellationToken);
    }

    private static async Task<JsonElement> PlaceAsync(HttpClient client, int placeId) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/world", CancellationToken)).EnumerateArray()
            .SelectMany(area => area.GetProperty("places").EnumerateArray())
            .Single(place => place.GetProperty("id").GetInt32() == placeId);

    [Fact]
    public async Task World_ListsAreasWithCurrentAndRecommendation()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var areas = (await client.GetFromJsonAsync<JsonElement>("/api/world", CancellationToken)).EnumerateArray().ToList();

        Assert.Equal(["central", "restricted"], areas.Select(area => area.GetProperty("id").GetString()));
        Assert.Equal("測試大陸", areas[0].GetProperty("name").GetString());
        var places = areas[0].GetProperty("places").EnumerateArray().ToList();
        Assert.Equal([1, 2, 20], places.Select(place => place.GetProperty("id").GetInt32()));
        var plain = places[0];
        Assert.Equal("測試平原", plain.GetProperty("name").GetString());
        Assert.True(plain.GetProperty("current").GetBoolean());
        Assert.Equal("Recommended", plain.GetProperty("recommendation").GetString());
        Assert.Equal(1, plain.GetProperty("recommended").GetProperty("min").GetInt32());
        Assert.Equal(10, plain.GetProperty("recommended").GetProperty("max").GetInt32());
        Assert.Equal(1, plain.GetProperty("subPlaces")[0].GetProperty("index").GetInt32());
        Assert.Equal("甲地", plain.GetProperty("subPlaces")[0].GetProperty("name").GetString());
        Assert.False(places[1].GetProperty("current").GetBoolean());
        Assert.Equal("TooHard", places[1].GetProperty("recommendation").GetString());
        Assert.Empty(places[2].GetProperty("subPlaces").EnumerateArray());
        var restricted = areas[1].GetProperty("places")[0];
        Assert.True(restricted.GetProperty("restricted").GetBoolean());
        Assert.Equal("None", restricted.GetProperty("recommendation").GetString());
    }

    [Fact]
    public async Task Move_ChangesLocation_AndMeAgrees()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(20));

        var response = await MoveAsync(client, 2, 3);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(2, body.GetProperty("placeId").GetInt32());
        Assert.Equal("測試洞窟", body.GetProperty("placeName").GetString());
        Assert.Equal(3, body.GetProperty("subPlace").GetInt32());
        Assert.Equal("三洞", body.GetProperty("subPlaceName").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        foreach (var field in new[] { "placeId", "subPlace", "exp", "actNum", "hp" })
        {
            Assert.Equal(body.GetProperty(field).GetInt64(), me.GetProperty(field).GetInt64());
        }

        var world = await client.GetFromJsonAsync<JsonElement>("/api/world", CancellationToken);
        Assert.Equal(
            [2],
            world.EnumerateArray().SelectMany(area => area.GetProperty("places").EnumerateArray())
                .Where(place => place.GetProperty("current").GetBoolean())
                .Select(place => place.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Move_Returns400_WithReasons()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await MoveAsync(client, 17, 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["restricted"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(1, me.GetProperty("placeId").GetInt32());
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync("/api/world/location", new { }, CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task World_ShowsKeysAndPass()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GiveKeysAsync(factory, client, 2);

        var restricted = await PlaceAsync(client, 17);
        Assert.Equal((2, JsonValueKind.Null), (restricted.GetProperty("keys").GetInt32(), restricted.GetProperty("passExpiresAt").ValueKind));
        var plain = await PlaceAsync(client, 2);
        Assert.Equal((0, JsonValueKind.Null), (plain.GetProperty("keys").GetInt32(), plain.GetProperty("passExpiresAt").ValueKind));

        (await MoveAsync(client, 17, 1)).EnsureSuccessStatusCode();

        restricted = await PlaceAsync(client, 17);
        Assert.Equal(1, restricted.GetProperty("keys").GetInt32());
        Assert.Equal(factory.Clock.UtcNow.AddSeconds(3600), restricted.GetProperty("passExpiresAt").GetDateTimeOffset());
    }

    // 用鑰匙進入禁區；之後的推進回應帶著通行證到期時刻
    [Fact]
    public async Task World_EnterRestrictedWithKey()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GiveKeysAsync(factory, client, 1);

        var response = await MoveAsync(client, 17, 1);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal((17, "測試禁地"), (body.GetProperty("placeId").GetInt32(), body.GetProperty("placeName").GetString()));
        var expiresAt = factory.Clock.UtcNow.AddSeconds(3600);
        factory.Clock.Advance(TimeSpan.FromSeconds(10));
        var advance = await (await client.PostAsync("/api/adventure/advance", null, CancellationToken)).Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(expiresAt, advance.GetProperty("areaPassExpiresAt").GetDateTimeOffset());
        Assert.Equal(0, (await PlaceAsync(client, 17)).GetProperty("keys").GetInt32());
    }

    [Fact]
    public async Task World_RequiresAuthentication()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/world", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MoveAsync(client, 2, 1)).StatusCode);
    }

    [Fact]
    public async Task World_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/world", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await MoveAsync(client, 2, 1)).StatusCode);
    }
}
