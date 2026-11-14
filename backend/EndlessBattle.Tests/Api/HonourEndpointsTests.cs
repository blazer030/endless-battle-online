using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

// TestContent：test-exchange-ore（測試礦石 ×1，花 測試勳章甲 ×2）、test-exchange-mercenary（測試榮譽傭兵，花 甲 ×1 ＋ 乙 ×1）
public class HonourEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"勳章{Random.Shared.Next(100000, 999999)}"));
        created.EnsureSuccessStatusCode();
        return client;
    }

    // 直接寫入勳章：取得勳章要靠任務或掉落，這裡跳過
    private static async Task<long> GiveAsync(EndlessBattleApiFactory factory, HttpClient client, string itemId, int quantity)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        return await scope.ServiceProvider.GetRequiredService<IInventoryRepository>()
            .AddAsync(character!.Id, TestItems.Entry(0, itemId, quantity), CancellationToken);
    }

    private static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static Task<HttpResponseMessage> ExchangeAsync(HttpClient client, string exchangeId) =>
        client.PostAsJsonAsync("/api/honour/exchange", new { exchangeId }, CancellationToken);

    [Fact]
    public async Task Honour_GetListsMedalsAndOffers()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GiveAsync(factory, client, "test-medal-a", 2);

        var shop = await ReadOkAsync(await client.GetAsync("/api/honour", CancellationToken));

        var medals = shop.GetProperty("medals").EnumerateArray().ToList();
        Assert.Equal(
            [("test-medal-a", "測試勳章甲", 2), ("test-medal-b", "測試勳章乙", 0)],
            medals.Select(medal => (medal.GetProperty("itemId").GetString(), medal.GetProperty("name").GetString(), medal.GetProperty("owned").GetInt32())));
        var exchanges = shop.GetProperty("exchanges").EnumerateArray().ToList();
        Assert.Equal(2, exchanges.Count);

        var ore = exchanges[0];
        Assert.Equal(("test-exchange-ore", "item", "test-ore", "測試礦石", 1, true), (
            ore.GetProperty("id").GetString(), ore.GetProperty("kind").GetString(), ore.GetProperty("rewardId").GetString(),
            ore.GetProperty("rewardName").GetString(), ore.GetProperty("quantity").GetInt32(), ore.GetProperty("affordable").GetBoolean()));
        var cost = Assert.Single(ore.GetProperty("cost").EnumerateArray());
        Assert.Equal(("test-medal-a", "測試勳章甲", 2, 2), (
            cost.GetProperty("itemId").GetString(), cost.GetProperty("name").GetString(), cost.GetProperty("quantity").GetInt32(), cost.GetProperty("owned").GetInt32()));

        var mercenary = exchanges[1];
        Assert.Equal(("mercenary", "test-honour-mercenary", "測試榮譽傭兵", false), (
            mercenary.GetProperty("kind").GetString(), mercenary.GetProperty("rewardId").GetString(),
            mercenary.GetProperty("rewardName").GetString(), mercenary.GetProperty("affordable").GetBoolean()));
    }

    // 規格 honour「Honour listing」：勳章只算背包，存進倉庫的不算
    [Fact]
    public async Task Honour_CountsMedalsInTheBagOnly()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var medals = await GiveAsync(factory, client, "test-medal-a", 3);
        await ReadOkAsync(await client.PostAsJsonAsync("/api/depot/store", new { inventoryId = medals, quantity = 2 }, CancellationToken));

        var shop = await ReadOkAsync(await client.GetAsync("/api/honour", CancellationToken));

        Assert.Equal(1, shop.GetProperty("medals")[0].GetProperty("owned").GetInt32());
    }

    [Fact]
    public async Task Honour_ExchangeReturnsShop()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GiveAsync(factory, client, "test-medal-a", 1);
        await GiveAsync(factory, client, "test-medal-b", 1);

        var shop = await ReadOkAsync(await ExchangeAsync(client, "test-exchange-mercenary"));

        Assert.All(shop.GetProperty("medals").EnumerateArray(), medal => Assert.Equal(0, medal.GetProperty("owned").GetInt32()));
        var mercenaries = await ReadOkAsync(await client.GetAsync("/api/mercenaries", CancellationToken));
        var contract = mercenaries.GetProperty("contract");
        Assert.Equal(("test-honour-mercenary", 5, false), (
            contract.GetProperty("mercenaryId").GetString(), contract.GetProperty("remaining").GetInt32(), contract.GetProperty("fighting").GetBoolean()));
    }

    // 規格 honour「Exchange details in the listing」的接線：道具與傭兵恰好一個有值、兌換回應也帶詳情與僱用中的名字
    [Fact]
    public async Task Honour_ListsDetailsAndHiredName()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GiveAsync(factory, client, "test-medal-a", 1);
        await GiveAsync(factory, client, "test-medal-b", 1);

        var listed = await ReadOkAsync(await client.GetAsync("/api/honour", CancellationToken));

        Assert.Equal(JsonValueKind.Null, listed.GetProperty("hiredMercenaryName").ValueKind);
        var ore = listed.GetProperty("exchanges")[0];
        Assert.Equal(JsonValueKind.Null, ore.GetProperty("mercenary").ValueKind);
        var item = ore.GetProperty("item");
        Assert.Equal(("Material", "None", 0, false), (
            item.GetProperty("category").GetString(), item.GetProperty("consumable").GetString(),
            item.GetProperty("requirements").GetArrayLength(), item.GetProperty("bound").GetBoolean()));
        var mercenary = listed.GetProperty("exchanges")[1];
        Assert.Equal(JsonValueKind.Null, mercenary.GetProperty("item").ValueKind);
        var contract = mercenary.GetProperty("mercenary");
        Assert.Equal((5, 100, 100, 100, 1), (
            contract.GetProperty("fights").GetInt32(), contract.GetProperty("moneyPercent").GetInt32(), contract.GetProperty("dropPercent").GetInt32(),
            contract.GetProperty("experiencePercent").GetInt32(), contract.GetProperty("deathCost").GetInt32()));

        var exchanged = await ReadOkAsync(await ExchangeAsync(client, "test-exchange-mercenary"));

        Assert.Equal("測試榮譽傭兵", exchanged.GetProperty("hiredMercenaryName").GetString());
        Assert.Equal(JsonValueKind.Object, exchanged.GetProperty("exchanges")[0].GetProperty("item").ValueKind);
    }

    [Fact]
    public async Task Honour_MissingMedalsReturnsReason()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await ExchangeAsync(client, "test-exchange-ore");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["medalsMissing"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/honour/exchange", new { }, CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Honour_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/honour", CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/honour/exchange", new { exchangeId = "test-exchange-ore" }, CancellationToken)).StatusCode);
    }
}
