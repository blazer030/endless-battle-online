using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// 新戰士：Lv1、str 12、warrior 熟練度 0
public class JobEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"轉職{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<HttpResponseMessage> ChangeAsync(HttpClient client, string jobId) =>
        client.PostAsJsonAsync("/api/jobs/change", new { jobId }, CancellationToken);

    [Fact]
    public async Task Jobs_ListMarksCurrentAndEligibility()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var jobs = (await client.GetFromJsonAsync<JsonElement>("/api/jobs", CancellationToken)).EnumerateArray().ToList();

        JsonElement Find(string id) => jobs.First(job => job.GetProperty("id").GetString() == id);
        Assert.True(Find("warrior").GetProperty("current").GetBoolean());
        Assert.False(Find("thief").GetProperty("current").GetBoolean());
        Assert.True(Find("thief").GetProperty("eligible").GetBoolean());
        var berserker = Find("berserker");
        Assert.False(berserker.GetProperty("eligible").GetBoolean());
        Assert.Equal("測試戰士", berserker.GetProperty("parentName").GetString());
        Assert.Equal(60, berserker.GetProperty("statRequirements")[0].GetProperty("minimum").GetInt32());
        Assert.Equal(3, berserker.GetProperty("growth").GetProperty("str")[0].GetInt32());
        Assert.Equal(
            ["levelTooLow", "parentProficiencyTooLow", "statTooLow:str"],
            berserker.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Change_Returns400_WhenIneligible()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await ChangeAsync(client, "berserker");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Contains("levelTooLow", problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Change_SwitchesJobAndKeepsLevel()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var before = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);

        var response = await ChangeAsync(client, "thief");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("thief", body.GetProperty("jobId").GetString());
        Assert.Equal("盜賊", body.GetProperty("jobName").GetString());
        foreach (var field in new[] { "level", "exp", "money" })
        {
            Assert.Equal(before.GetProperty(field).GetInt64(), body.GetProperty(field).GetInt64());
        }

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal("thief", me.GetProperty("jobId").GetString());
    }

    // 推進 20 秒耗掉行動力，再過 400 秒：GET /me 顯示回復後的值（+12），轉職回應也要一樣
    [Fact]
    public async Task Change_ReturnsSameActNumAsMe()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(20));
        var advanced = await (await client.PostAsync("/api/adventure/advance", null, CancellationToken)).Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var spent = advanced.GetProperty("character").GetProperty("actNum").GetInt32();
        factory.Clock.Advance(TimeSpan.FromSeconds(400));

        var response = await ChangeAsync(client, "thief");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(Math.Min(spent + 12, 50), me.GetProperty("actNum").GetInt32());
        Assert.Equal(me.GetProperty("actNum").GetInt32(), body.GetProperty("actNum").GetInt32());
    }

    [Fact]
    public async Task Change_Returns400_ForSameJob()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await ChangeAsync(client, "warrior");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["sameJob"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Jobs_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/jobs", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ChangeAsync(client, "thief")).StatusCode);
    }

    [Fact]
    public async Task Change_Returns400_WhenJobIdBlank()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await ChangeAsync(client, " ")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/jobs/change", new { }, CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Jobs_RequiresAuthentication()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/jobs", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ChangeAsync(client, "thief")).StatusCode);
    }
}
