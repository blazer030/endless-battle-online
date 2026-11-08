using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

// test-sword 1 孔、精鍊上限 4；test-refine-stone Lv1；test-refine-stone-2 Lv2；test-stone 固定 at +5。
// API 主機的亂數是固定種子，成敗不寫死，只驗證兩種結果各自應有的狀態
public class ForgeEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, long CharacterId)> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = $"鍛造{Random.Shared.Next(100000, 999999)}";
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        await using var context = database.CreateDbContext();
        var characterId = await context.Characters.Where(character => character.Name == name).Select(character => character.Id).SingleAsync(CancellationToken);
        return (client, characterId);
    }

    [Fact]
    public async Task Recipes_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/forge/recipes", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<long> GiveAsync(long characterId, string itemId, int quantity = 1, int holes = 0)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context, TestGameContent.Instance).AddAsync(characterId, TestItems.Entry(0, itemId, quantity) with { Holes = holes }, CancellationToken);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static async Task<IEnumerable<string?>> ReasonsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        return problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()).ToList();
    }

    private static JsonElement Items(JsonElement inventory, string category) =>
        inventory.GetProperty("bag").EnumerateArray().Single(bag => bag.GetProperty("category").GetString() == category).GetProperty("items");

    [Fact]
    public async Task Refine_ConsumesStoneAndReportsOutcome()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword", holes: 1);
        var stone = await GiveAsync(characterId, "test-refine-stone");
        var wrongLevel = await GiveAsync(characterId, "test-refine-stone-2");

        Assert.Equal(
            ["wrongStoneLevel"],
            await ReasonsAsync(await client.PostAsJsonAsync("/api/forge/refine", new { inventoryId = sword, stoneInventoryId = wrongLevel }, CancellationToken)));
        var notFound = await client.PostAsJsonAsync("/api/forge/refine", new { inventoryId = 999999999, stoneInventoryId = stone }, CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);

        var result = await ReadAsync(await client.PostAsJsonAsync(
            "/api/forge/refine", new { inventoryId = sword, stoneInventoryId = stone, useSafeStone = false }, CancellationToken));

        var success = result.GetProperty("success").GetBoolean();
        var inventory = result.GetProperty("inventory");
        Assert.Equal(["test-refine-stone-2"], Items(inventory, "RefineStone").EnumerateArray().Select(item => item.GetProperty("itemId").GetString()));
        var weapon = Assert.Single(Items(inventory, "Weapon").EnumerateArray());
        Assert.Equal(success ? 1 : 0, weapon.GetProperty("refineLevel").GetInt32());
        Assert.Equal(success, weapon.GetProperty("refineBonus").GetProperty("at").GetInt32() > 0);
    }

    [Fact]
    public async Task Socket_InsertsStoneAndRejectsOccupiedHole()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword", holes: 1);
        var stone = await GiveAsync(characterId, "test-stone", 2);

        var result = await ReadAsync(await client.PostAsJsonAsync(
            "/api/forge/socket", new { inventoryId = sword, hole = 1, stoneInventoryId = stone }, CancellationToken));

        Assert.True(result.GetProperty("success").GetBoolean());
        var inventory = result.GetProperty("inventory");
        var socket = Assert.Single(Assert.Single(Items(inventory, "Weapon").EnumerateArray()).GetProperty("sockets").EnumerateArray());
        Assert.Equal(1, socket.GetProperty("hole").GetInt32());
        Assert.Equal("test-stone", socket.GetProperty("stoneId").GetString());
        Assert.Equal(5, socket.GetProperty("rolled").GetProperty("at").GetInt32());
        Assert.Equal(1, Items(inventory, "Stone")[0].GetProperty("quantity").GetInt32());

        Assert.Equal(
            ["holeOccupied"],
            await ReasonsAsync(await client.PostAsJsonAsync("/api/forge/socket", new { inventoryId = sword, hole = 1, stoneInventoryId = stone }, CancellationToken)));
    }

    // TestContent 只有一條配方：test-ore ×3 → test-sword ×1，成功率 70
    [Fact]
    public async Task Recipes_EndpointListsAll()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var recipes = (await client.GetFromJsonAsync<JsonElement>("/api/forge/recipes", CancellationToken)).EnumerateArray().ToList();

        Assert.Equal(["test-forge-sword", "test-forge-locked"], recipes.Select(entry => entry.GetProperty("id").GetString()));
        var recipe = recipes[0];
        Assert.Equal("test-forge-sword", recipe.GetProperty("id").GetString());
        Assert.Equal("測試鐵劍", recipe.GetProperty("resultName").GetString());
        Assert.Equal(1, recipe.GetProperty("resultQuantity").GetInt32());
        Assert.Equal(70, recipe.GetProperty("successRate").GetInt32());
        Assert.Equal(1000, recipe.GetProperty("costPerAttempt").GetInt64());
        var material = Assert.Single(recipe.GetProperty("materials").EnumerateArray());
        Assert.Equal(("test-ore", "測試礦石", 3), (material.GetProperty("itemId").GetString(), material.GetProperty("name").GetString(), material.GetProperty("quantity").GetInt32()));
    }

    [Fact]
    public async Task Recipes_ReportRequiredMissionState()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);

        async Task<JsonElement> ListAsync()
        {
            var recipes = await client.GetFromJsonAsync<JsonElement>("/api/forge/recipes", CancellationToken);
            return recipes.EnumerateArray().Single(recipe => recipe.GetProperty("id").GetString() == "test-forge-locked").GetProperty("requiredMission");
        }

        var locked = await ListAsync();
        Assert.Equal(("test-hunt", "測試討伐", false), (
            locked.GetProperty("id").GetString(), locked.GetProperty("title").GetString(), locked.GetProperty("completed").GetBoolean()));

        await SeedMissionAsync(characterId, MissionState.Active);
        Assert.False((await ListAsync()).GetProperty("completed").GetBoolean());

        await using (var context = database.CreateDbContext())
        {
            await new MissionRepository(context).CompleteAsync(characterId, "test-hunt", DateTimeOffset.UtcNow, CancellationToken);
        }

        Assert.True((await ListAsync()).GetProperty("completed").GetBoolean());
        var open = (await client.GetFromJsonAsync<JsonElement>("/api/forge/recipes", CancellationToken)).EnumerateArray()
            .Single(recipe => recipe.GetProperty("id").GetString() == "test-forge-sword");
        Assert.Equal(JsonValueKind.Null, open.GetProperty("requiredMission").ValueKind);
    }

    private async Task SeedMissionAsync(long characterId, MissionState state)
    {
        await using var context = database.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        await new MissionRepository(context).AddAsync(
            characterId,
            new MissionRecord("test-hunt", state, [3], null, now, state == MissionState.Completed ? now : null),
            CancellationToken);
    }

    private async Task AssertLockedAsync(HttpClient client)
    {
        Assert.Equal(
            ["missionIncomplete"],
            await ReasonsAsync(await client.PostAsJsonAsync("/api/forge/synthesize", new { recipeId = "test-forge-locked", attempts = 1 }, CancellationToken)));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(2000, me.GetProperty("money").GetInt64());
        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);
        Assert.Equal(1, Items(inventory, "Material").EnumerateArray().Single().GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Synthesize_RequiredMissionNotAccepted_Fails()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-ore");

        await AssertLockedAsync(client);
    }

    [Fact]
    public async Task Synthesize_RequiredMissionActive_Fails()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-ore");
        await SeedMissionAsync(characterId, MissionState.Active);

        await AssertLockedAsync(client);
    }

    [Fact]
    public async Task Synthesize_RequiredMissionCompleted_Runs()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-ore");
        await SeedMissionAsync(characterId, MissionState.Completed);

        var result = await ReadAsync(await client.PostAsJsonAsync(
            "/api/forge/synthesize", new { recipeId = "test-forge-locked", attempts = 1 }, CancellationToken));

        Assert.Equal(1, result.GetProperty("successes").GetInt32());
    }

    // 新角色 2000 金錢：合成 2 次，材料全扣、成功幾次就多幾把劍、扣幾個 1000
    [Fact]
    public async Task Synthesize_EndpointReturnsCounts()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-ore", 6);

        Assert.Equal(
            ["invalidQuantity"],
            await ReasonsAsync(await client.PostAsJsonAsync("/api/forge/synthesize", new { recipeId = "test-forge-sword", attempts = 0 }, CancellationToken)));
        var unknown = await client.PostAsJsonAsync("/api/forge/synthesize", new { recipeId = "nope", attempts = 1 }, CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var result = await ReadAsync(await client.PostAsJsonAsync(
            "/api/forge/synthesize", new { recipeId = "test-forge-sword", attempts = 2 }, CancellationToken));

        var successes = result.GetProperty("successes").GetInt32();
        Assert.InRange(successes, 0, 2);
        Assert.Equal(2, result.GetProperty("attempts").GetInt32());
        Assert.Equal(1000L * successes, result.GetProperty("cost").GetInt64());
        var inventory = result.GetProperty("inventory");
        Assert.Empty(Items(inventory, "Material").EnumerateArray());
        Assert.Equal(successes, Items(inventory, "Weapon").GetArrayLength());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(2000 - 1000L * successes, me.GetProperty("money").GetInt64());
    }

    // 鋼鐵 Lv2 + 秘法 Lv2 → 活力 Lv1；同種類 400 cannotMerge
    [Fact]
    public async Task MergeCross_Route()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var iron = await GiveAsync(characterId, "test-refine-stone-2", 2);
        var arcane = await GiveAsync(characterId, "test-arcane-refine-stone-2", 1);

        Assert.Equal(
            ["cannotMerge"],
            await ReasonsAsync(await client.PostAsJsonAsync(
                "/api/forge/merge-cross", new { firstInventoryId = iron, secondInventoryId = iron, quantityPairs = 1 }, CancellationToken)));

        var result = await ReadAsync(await client.PostAsJsonAsync(
            "/api/forge/merge-cross", new { firstInventoryId = iron, secondInventoryId = arcane, quantityPairs = 1 }, CancellationToken));

        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(
            [("test-refine-stone-2", 1), ("test-vigor-refine-stone", 1)],
            Items(result.GetProperty("inventory"), "RefineStone").EnumerateArray()
                .Select(item => (item.GetProperty("itemId").GetString(), item.GetProperty("quantity").GetInt32())));
    }
}
