using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

// 新戰士：Lv 1、金錢 2000。TestContent 的 inn.json：8 小時、每級 50，學徒閣樓價格倍率 2
public class InnEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, long CharacterId)> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = $"旅館{Random.Shared.Next(100000, 999999)}";
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        await using var context = database.CreateDbContext();
        var characterId = await context.Characters.Where(character => character.Name == name).Select(character => character.Id).SingleAsync(CancellationToken);
        return (client, characterId);
    }

    private async Task SetMoneyAsync(long characterId, long money)
    {
        await using var context = database.CreateDbContext();
        await context.Characters.Where(character => character.Id == characterId)
            .ExecuteUpdateAsync(update => update.SetProperty(character => character.Money, money), CancellationToken);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static async Task<IReadOnlyList<string>> ReasonsOf400Async(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        return problem.TryGetProperty("reasons", out var reasons) ? reasons.EnumerateArray().Select(reason => reason.GetString()!).ToList() : [];
    }

    [Fact]
    public async Task GetInn_ListsRooms()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var inn = await ReadAsync(await client.GetAsync("/api/inn", CancellationToken));

        var rooms = inn.GetProperty("rooms").EnumerateArray().ToList();
        Assert.Equal(6, rooms.Count);
        var loft = rooms[0];
        Assert.Equal("apprentice-loft", loft.GetProperty("id").GetString());
        Assert.Equal("學徒閣樓", loft.GetProperty("name").GetString());
        Assert.Equal("Experience", loft.GetProperty("kind").GetString());
        Assert.Equal(1.10, loft.GetProperty("multiplier").GetDouble());
        Assert.Equal(28800, loft.GetProperty("seconds").GetInt32());
        Assert.Equal(100, loft.GetProperty("price").GetInt64());
        Assert.Equal(50, rooms[2].GetProperty("price").GetInt64());
        Assert.Equal(JsonValueKind.Null, inn.GetProperty("lodging").ValueKind);
    }

    // 規格 Buff kinds use complete names：內容檔仍寫 Exp／JobExp
    [Fact]
    public async Task Inn_BuffKindsUseCompleteNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var inn = await ReadAsync(await client.GetAsync("/api/inn", CancellationToken));

        var kinds = inn.GetProperty("rooms").EnumerateArray().Select(room => room.GetProperty("kind").GetString()).ToList();
        Assert.Equal(["Experience", "JobExperience", "Attack", "Magic", "Defense", "MagicDefense"], kinds);
    }

    [Fact]
    public async Task Book_Returns400WithReasons()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);

        var unknown = await client.PostAsJsonAsync("/api/inn/book", new { roomId = "royal-suite" }, CancellationToken);
        Assert.Equal(["unknownRoom"], await ReasonsOf400Async(unknown));

        var missing = await client.PostAsJsonAsync("/api/inn/book", new { }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        await SetMoneyAsync(characterId, 99);
        var poor = await client.PostAsJsonAsync("/api/inn/book", new { roomId = "apprentice-loft" }, CancellationToken);
        Assert.Equal(["notEnoughMoney"], await ReasonsOf400Async(poor));
    }

    [Fact]
    public async Task Book_ReturnsLodging()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var booked = await ReadAsync(await client.PostAsJsonAsync("/api/inn/book", new { roomId = "apprentice-loft" }, CancellationToken));

        Assert.Equal(100, booked.GetProperty("cost").GetInt64());
        Assert.Equal(1900, booked.GetProperty("character").GetProperty("money").GetInt64());
        Assert.Equal("apprentice-loft", booked.GetProperty("lodging").GetProperty("roomId").GetString());

        var inn = await ReadAsync(await client.GetAsync("/api/inn", CancellationToken));
        Assert.Equal("apprentice-loft", inn.GetProperty("lodging").GetProperty("roomId").GetString());
        Assert.Equal(
            booked.GetProperty("lodging").GetProperty("endsAt").GetDateTimeOffset(),
            inn.GetProperty("lodging").GetProperty("endsAt").GetDateTimeOffset());
    }

    // 規格 Lodging start and end：住宿帶出開始與結束時刻（8 小時）
    [Fact]
    public async Task Inn_LodgingCarriesStartAndEnd()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);
        var now = factory.Clock.UtcNow;

        var booked = await ReadAsync(await client.PostAsJsonAsync("/api/inn/book", new { roomId = "apprentice-loft" }, CancellationToken));
        var inn = await ReadAsync(await client.GetAsync("/api/inn", CancellationToken));

        foreach (var lodging in new[] { booked.GetProperty("lodging"), inn.GetProperty("lodging") })
        {
            Assert.Equal(now, lodging.GetProperty("startsAt").GetDateTimeOffset());
            Assert.Equal(now.AddHours(8), lodging.GetProperty("endsAt").GetDateTimeOffset());
        }
    }

    [Theory]
    [InlineData("GET", "/api/inn")]
    [InlineData("POST", "/api/inn/book")]
    public async Task Inn_RequiresAuthentication(string method, string path)
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/inn")]
    [InlineData("POST", "/api/inn/book")]
    public async Task Inn_Returns404_WithoutCharacter(string method, string path)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { roomId = "apprentice-loft" });
        }

        var response = await client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
