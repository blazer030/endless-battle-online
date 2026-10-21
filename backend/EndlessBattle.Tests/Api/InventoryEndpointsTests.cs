using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

// 新戰士（配點 2/2/2/2）：Base str 12、vit 10 → at 12、df 10。商店在 Task 4，測試直接把道具寫進背包表
public class InventoryEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<(HttpClient Client, long CharacterId)> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = $"背包{Random.Shared.Next(100000, 999999)}";
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        await using var context = database.CreateDbContext();
        var characterId = await context.Characters.Where(character => character.Name == name).Select(character => character.Id).SingleAsync(CancellationToken);
        return (client, characterId);
    }

    private async Task<long> GiveAsync(long characterId, string itemId, int quantity = 1)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context).AddAsync(characterId, TestItems.Entry(0, itemId, quantity), CancellationToken);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static JsonElement Category(JsonElement inventory, string category) =>
        inventory.GetProperty("bag").EnumerateArray().Single(bag => bag.GetProperty("category").GetString() == category);

    [Fact]
    public async Task Get_ShowsBagAndLoadout()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        await GiveAsync(characterId, "test-supply", 5);

        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);

        Assert.Equal(10, inventory.GetProperty("bag").GetArrayLength());
        var weapons = Category(inventory, "Weapon");
        Assert.Equal(15, weapons.GetProperty("capacity").GetInt32());
        var item = Assert.Single(weapons.GetProperty("items").EnumerateArray());
        Assert.Equal(sword, item.GetProperty("inventoryId").GetInt64());
        Assert.Equal("測試鐵劍", item.GetProperty("name").GetString());
        Assert.Equal(8, item.GetProperty("bonus").GetProperty("at").GetInt32());
        Assert.Equal(10, item.GetProperty("requires").GetProperty("str").GetInt32());
        Assert.Equal(5, Category(inventory, "Consumable").GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Empty(inventory.GetProperty("equipment").EnumerateArray());
        Assert.Equal(0, inventory.GetProperty("bonus").GetProperty("at").GetInt32());
    }

    // 鐵劍 at 8 → at 20；怪力戒指 str +50 → 有效 str 62、at 8 + 62 = 70；裸點仍是 12
    [Fact]
    public async Task Equip_ChangesEffectiveStatsInMe()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        var ring = await GiveAsync(characterId, "test-power-ring");

        var equipped = await ReadAsync(await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = sword }, CancellationToken));
        var slot = Assert.Single(equipped.GetProperty("equipment").EnumerateArray());
        Assert.Equal("Weapon", slot.GetProperty("slot").GetString());
        Assert.Empty(Category(equipped, "Weapon").GetProperty("items").EnumerateArray());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(20, me.GetProperty("stats").GetProperty("at").GetInt32());

        await ReadAsync(await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = ring }, CancellationToken));

        me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(62, me.GetProperty("stats").GetProperty("str").GetInt32());
        Assert.Equal(70, me.GetProperty("stats").GetProperty("at").GetInt32());
        Assert.Equal(12, me.GetProperty("baseStats").GetProperty("str").GetInt32());
    }

    [Fact]
    public async Task Equip_Returns400_WithStatReason()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var heavy = await GiveAsync(characterId, "test-heavy-sword");

        var response = await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = heavy }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["statTooLow:str"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    // 補給欄裝 3、卸下後疊回背包；用掉一瓶力量藥水 → 裸點 str 13
    [Fact]
    public async Task Unequip_And_Use_RoundTrip()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var supply = await GiveAsync(characterId, "test-supply", 10);
        var potion = await GiveAsync(characterId, "test-str-potion");

        var equipped = await ReadAsync(await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = supply, quantity = 3 }, CancellationToken));
        Assert.Equal(3, equipped.GetProperty("equipment")[0].GetProperty("quantity").GetInt32());

        var unequipped = await ReadAsync(await client.PostAsJsonAsync("/api/inventory/unequip", new { slot = "Supply" }, CancellationToken));
        Assert.Empty(unequipped.GetProperty("equipment").EnumerateArray());
        Assert.Equal(10, Category(unequipped, "Consumable").GetProperty("items")[0].GetProperty("quantity").GetInt32());

        await ReadAsync(await client.PostAsJsonAsync("/api/inventory/use", new { inventoryId = potion }, CancellationToken));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(13, me.GetProperty("baseStats").GetProperty("str").GetInt32());
    }

    // 生命護符 life +20 → hpMax 32×25+50 = 850；在身上時 hp 850，卸下後上限回到 350，hp 不能超過
    [Fact]
    public async Task Me_ClampsHpAfterUnequip()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var amulet = await GiveAsync(characterId, "test-life-amulet");
        await ReadAsync(await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = amulet }, CancellationToken));
        await using (var context = database.CreateDbContext())
        {
            await context.Characters.Where(character => character.Id == characterId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(character => character.Hp, 850), CancellationToken);
        }

        await ReadAsync(await client.PostAsJsonAsync("/api/inventory/unequip", new { slot = "Accessory" }, CancellationToken));

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(350, me.GetProperty("stats").GetProperty("hpMax").GetInt32());
        Assert.Equal(350, me.GetProperty("hp").GetInt32());
    }

    [Fact]
    public async Task Inventory_RequiresAuthentication()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/inventory", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_Returns404_WithoutCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/inventory", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unequip_Returns400_ForUnknownSlot()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, _) = await CreateCharacterAsync(factory);

        var response = await client.PostAsJsonAsync("/api/inventory/unequip", new { slot = "Tail" }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
