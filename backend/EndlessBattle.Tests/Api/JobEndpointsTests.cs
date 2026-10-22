using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

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

    private async Task<(HttpClient Client, long CharacterId)> CreateClientWithCharacterIdAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = $"轉職{Random.Shared.Next(100000, 999999)}";
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        await using var context = database.CreateDbContext();
        var characterId = await context.Characters.Where(character => character.Name == name).Select(character => character.Id).SingleAsync(CancellationToken);
        return (client, characterId);
    }

    private async Task<long> GiveAsync(long characterId, string itemId, int quantity = 1)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context, TestGameContent.Instance).AddAsync(characterId, TestItems.Entry(0, itemId, quantity), CancellationToken);
    }

    private static async Task<JsonElement> InventoryAsync(HttpClient client) =>
        await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);

    private static JsonElement Items(JsonElement inventory, string category) =>
        inventory.GetProperty("bag").EnumerateArray().Single(bag => bag.GetProperty("category").GetString() == category).GetProperty("items");

    // 裝上 16 把劍中的 1 把：背包武器 15 格全滿，轉職要卸下的劍放不回去
    [Fact]
    public async Task JobChange_BagFullLeavesEquipmentAndJob()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateClientWithCharacterIdAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        for (var index = 0; index < 15; index++)
        {
            await GiveAsync(characterId, "test-sword");
        }

        (await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = sword }, CancellationToken)).EnsureSuccessStatusCode();

        var response = await ChangeAsync(client, "thief");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["bagFull"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
        var equipped = Assert.Single((await InventoryAsync(client)).GetProperty("equipment").EnumerateArray());
        Assert.Equal(sword, equipped.GetProperty("item").GetProperty("inventoryId").GetInt64());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal("warrior", me.GetProperty("jobId").GetString());
    }

    // 補給欄裝 4 個（從 10 個那格分出）：轉職後併回原格成為 10 個，武器回到背包
    [Fact]
    public async Task Change_UnequipsWeaponAndSupplyIntoBag()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateClientWithCharacterIdAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        var supply = await GiveAsync(characterId, "test-supply", 10);
        (await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = sword }, CancellationToken)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = supply, quantity = 4 }, CancellationToken)).EnsureSuccessStatusCode();

        (await ChangeAsync(client, "thief")).EnsureSuccessStatusCode();

        var inventory = await InventoryAsync(client);
        Assert.Empty(inventory.GetProperty("equipment").EnumerateArray());
        Assert.Equal(sword, Assert.Single(Items(inventory, "Weapon").EnumerateArray()).GetProperty("inventoryId").GetInt64());
        var supplies = Assert.Single(Items(inventory, "Consumable").EnumerateArray());
        Assert.Equal((supply, 10), (supplies.GetProperty("inventoryId").GetInt64(), supplies.GetProperty("quantity").GetInt32()));
    }

    // 兩個角色同時轉職（各自清掉被動技能欄、重寫技能欄）：彼此的鎖不能互相卡住，10 輪全部 200
    [Fact]
    public async Task JobChange_TwoCharactersAtOnce_BothSucceed()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var players = new[] { await CreateClientWithCharacterIdAsync(factory), await CreateClientWithCharacterIdAsync(factory) };

        for (var round = 0; round < 10; round++)
        {
            var current = round % 2 == 0 ? "warrior" : "thief";
            foreach (var (_, characterId) in players)
            {
                await using var context = database.CreateDbContext();
                await new SkillLoadoutRepository(context).ReplaceAsync(
                    characterId, [new SkillSlot(1, current == "warrior" ? "burst" : "combo", 50), new SkillSlot(4, "iron-skin", 30)], CancellationToken);
            }

            var target = current == "warrior" ? "thief" : "warrior";
            var responses = await Task.WhenAll(players.Select(player => ChangeAsync(player.Client, target)));

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        }
    }
}
