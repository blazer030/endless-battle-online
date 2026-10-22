using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

// 新戰士：Lv 1、金錢 2000（以 /me 為準）。test-sword 價 200、商店賣；test-axe 不賣
public class EconomyEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, long CharacterId)> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = $"商店{Random.Shared.Next(100000, 999999)}";
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

    private async Task SetLevelAsync(long characterId, int level)
    {
        await using var context = database.CreateDbContext();
        await context.Characters.Where(character => character.Id == characterId)
            .ExecuteUpdateAsync(update => update.SetProperty(character => character.Level, level), CancellationToken);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static async Task<long> MoneyAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken)).GetProperty("money").GetInt64();

    private static async Task<IReadOnlyList<string>> ReasonsOf400Async(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        return problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()!).ToList();
    }

    private static JsonElement Category(JsonElement inventory, string category) =>
        inventory.GetProperty("bag").EnumerateArray().Single(bag => bag.GetProperty("category").GetString() == category);

    [Fact]
    public async Task Buy_OnlyShopSellableByLevel()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var level1 = await client.GetFromJsonAsync<JsonElement>("/api/shop?category=Weapon&level=1", CancellationToken);
        var item = Assert.Single(level1.EnumerateArray());
        Assert.Equal("test-sword", item.GetProperty("id").GetString());
        Assert.Equal(200, item.GetProperty("price").GetInt64());
        Assert.Equal(8, item.GetProperty("bonus").GetProperty("at").GetInt32());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/shop?category=Weapon&level=2", CancellationToken)).EnumerateArray());

        var notForSale = await client.PostAsJsonAsync("/api/shop/buy", new { itemId = "test-axe", quantity = 1 }, CancellationToken);
        Assert.Equal(["notForSale"], await ReasonsOf400Async(notForSale));
        var badLevel = await client.GetAsync("/api/shop?category=Weapon&level=8", CancellationToken);
        Assert.Equal(["invalidLevel"], await ReasonsOf400Async(badLevel));
    }

    [Fact]
    public async Task Buy_DeductsMoneyAndAddsToBag()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var inventory = await ReadAsync(await client.PostAsJsonAsync("/api/shop/buy", new { itemId = "test-sword", quantity = 1 }, CancellationToken));

        Assert.Equal("test-sword", Category(inventory, "Weapon").GetProperty("items")[0].GetProperty("itemId").GetString());
        Assert.Equal(1800, await MoneyAsync(client));
    }

    [Fact]
    public async Task Sell_AddsMoneyAndRemovesRow()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");

        var inventory = await ReadAsync(await client.PostAsJsonAsync("/api/shop/sell", new { inventoryId = sword, quantity = 1 }, CancellationToken));

        Assert.Empty(Category(inventory, "Weapon").GetProperty("items").EnumerateArray());
        Assert.Equal(2100, await MoneyAsync(client));
    }

    [Fact]
    public async Task Bank_RequiresLevel15_ThenDepositsAndWithdraws()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);

        var early = await client.PostAsJsonAsync("/api/bank/deposit", new { amount = 100 }, CancellationToken);
        Assert.Equal(["levelTooLow"], await ReasonsOf400Async(early));

        var before = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(0, before.GetProperty("bank").GetInt64());

        await SetLevelAsync(characterId, 15);
        var deposited = await ReadAsync(await client.PostAsJsonAsync("/api/bank/deposit", new { amount = 500 }, CancellationToken));
        Assert.Equal(1500, deposited.GetProperty("money").GetInt64());
        Assert.Equal(500, deposited.GetProperty("bank").GetInt64());
        var withdrawn = await ReadAsync(await client.PostAsJsonAsync("/api/bank/withdraw", new { amount = 200 }, CancellationToken));
        Assert.Equal(1700, withdrawn.GetProperty("money").GetInt64());
        Assert.Equal(300, withdrawn.GetProperty("bank").GetInt64());

        var overdraft = await client.PostAsJsonAsync("/api/bank/withdraw", new { amount = 301 }, CancellationToken);
        Assert.Equal(["notEnoughBank"], await ReasonsOf400Async(overdraft));
    }

    [Fact]
    public async Task Depot_StoresAndTakes()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");

        var stored = await ReadAsync(await client.PostAsJsonAsync("/api/depot/store", new { inventoryId = sword, quantity = 1 }, CancellationToken));

        Assert.Empty(Category(stored.GetProperty("inventory"), "Weapon").GetProperty("items").EnumerateArray());
        var entry = Assert.Single(stored.GetProperty("depot").GetProperty("entries").EnumerateArray());
        Assert.Equal(20, stored.GetProperty("depot").GetProperty("capacity").GetInt32());

        var listed = await client.GetFromJsonAsync<JsonElement>("/api/depot", CancellationToken);
        Assert.Single(listed.GetProperty("entries").EnumerateArray());

        var taken = await ReadAsync(await client.PostAsJsonAsync(
            "/api/depot/take", new { depotId = entry.GetProperty("inventoryId").GetInt64(), quantity = 1 }, CancellationToken));

        Assert.Empty(taken.GetProperty("depot").GetProperty("entries").EnumerateArray());
        Assert.Single(Category(taken.GetProperty("inventory"), "Weapon").GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Depot_PreservesEnhancementsThroughStoreAndTake()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).UpdateEnhancementAsync(
                characterId, sword, 2, StatBonus.Zero with { At = 3 }, [new SocketedStone(1, "test-stone", StatBonus.Zero with { At = 5 })], 1, CancellationToken);
        }

        var stored = await ReadAsync(await client.PostAsJsonAsync("/api/depot/store", new { inventoryId = sword, quantity = 1 }, CancellationToken));
        var depotId = stored.GetProperty("depot").GetProperty("entries")[0].GetProperty("inventoryId").GetInt64();
        await ReadAsync(await client.PostAsJsonAsync("/api/depot/take", new { depotId, quantity = 1 }, CancellationToken));

        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);
        var item = Assert.Single(Category(inventory, "Weapon").GetProperty("items").EnumerateArray());
        Assert.Equal(2, item.GetProperty("refineLevel").GetInt32());
        Assert.Equal(3, item.GetProperty("refineBonus").GetProperty("at").GetInt32());
        Assert.Equal(1, item.GetProperty("holes").GetInt32());
        var socket = Assert.Single(item.GetProperty("sockets").EnumerateArray());
        Assert.Equal("test-stone", socket.GetProperty("stoneId").GetString());
        Assert.Equal(5, socket.GetProperty("rolled").GetProperty("at").GetInt32());
    }

    [Fact]
    public async Task Take_Returns400_WhenDepotItemMissing()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var response = await client.PostAsJsonAsync("/api/depot/take", new { depotId = 999999, quantity = 1 }, CancellationToken);

        Assert.Equal(["itemNotFound"], await ReasonsOf400Async(response));
    }

    [Theory]
    [InlineData("GET", "/api/shop?category=Weapon&level=1")]
    [InlineData("POST", "/api/shop/buy")]
    [InlineData("POST", "/api/bank/deposit")]
    [InlineData("GET", "/api/depot")]
    public async Task Economy_RequiresAuthentication(string method, string path)
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/depot")]
    [InlineData("POST", "/api/bank/withdraw")]
    [InlineData("POST", "/api/shop/sell")]
    public async Task Economy_Returns404_WithoutCharacter(string method, string path)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { amount = 1, inventoryId = 1, quantity = 1 });
        }

        var response = await client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
