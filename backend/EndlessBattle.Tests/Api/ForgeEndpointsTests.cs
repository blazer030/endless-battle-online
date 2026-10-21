using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    private async Task<long> GiveAsync(long characterId, string itemId, int quantity = 1, int holes = 0)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context).AddAsync(characterId, TestItems.Entry(0, itemId, quantity) with { Holes = holes }, CancellationToken);
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
}
