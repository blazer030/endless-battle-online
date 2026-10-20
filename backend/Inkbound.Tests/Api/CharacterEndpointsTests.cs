using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Api;

public class CharacterEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // 名稱在資料庫中唯一，加上隨機後綴避免測試之間互相影響
    private static string UniqueName(string prefix) => $"{prefix}{Random.Shared.Next(100000, 999999)}";

    [Fact]
    public async Task Create_ReturnsCharacterAtStartingStage()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("墨客");

        var created = await client.PostAsJsonAsync("/api/characters", new { name }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.Equal(0, body.GetProperty("experience").GetInt64());
        Assert.Equal(0, body.GetProperty("spiritStones").GetInt64());
        Assert.Equal("test-stage-1", body.GetProperty("currentStageId").GetString());

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(body.ToString(), me.ToString());
    }

    [Fact]
    public async Task GetMe_Returns404_WhenNoCharacter()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_Returns409_WhenAccountAlreadyHasCharacter()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/characters", new { name = UniqueName("初") }, CancellationToken);

        var second = await client.PostAsJsonAsync("/api/characters", new { name = UniqueName("再") }, CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameTaken()
    {
        await using var factory = new InkboundApiFactory(database);
        var firstClient = await factory.CreateAuthenticatedClientAsync();
        var secondClient = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("同名");

        await firstClient.PostAsJsonAsync("/api/characters", new { name }, CancellationToken);
        var duplicate = await secondClient.PostAsJsonAsync("/api/characters", new { name }, CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("墨")]
    [InlineData("一二三四五六七八九十甲乙丙")]
    [InlineData("   ")]
    public async Task Create_Returns400_ForInvalidName(string name)
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/characters", new { name }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_TrimsSurroundingWhitespace()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("劍心");

        var response = await client.PostAsJsonAsync("/api/characters", new { name = $"  {name}  " }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(name, body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Endpoints_Require_Authentication()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/characters", new { name = "無名氏" }, CancellationToken);
        var me = await client.GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
