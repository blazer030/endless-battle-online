using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

// 新戰士（配點 2/4/2/2）：Base str 12、vit 10 → at 12、df 10。商店在 Task 4，測試直接把道具寫進背包表
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
        return await new InventoryRepository(context, TestGameContent.Instance).AddAsync(characterId, TestItems.Entry(0, itemId, quantity), CancellationToken);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Inventory_WeaponWithSkill_ReturnsWeaponSkill()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-burst-blade");
        await GiveAsync(characterId, "test-sword");

        var inventory = await ReadAsync(await client.GetAsync("/api/inventory", CancellationToken));

        var items = Category(inventory, "Weapon").GetProperty("items").EnumerateArray().ToList();
        var skill = items.Single(item => item.GetProperty("itemId").GetString() == "test-burst-blade").GetProperty("weaponSkill");
        Assert.Equal(("爆氣", 2, 100), (skill.GetProperty("name").GetString(), skill.GetProperty("level").GetInt32(), skill.GetProperty("rate").GetInt32()));
        Assert.Equal(JsonValueKind.Null, items.Single(item => item.GetProperty("itemId").GetString() == "test-sword").GetProperty("weaponSkill").ValueKind);
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

        Assert.Equal(12, inventory.GetProperty("bag").GetArrayLength());
        var weapons = Category(inventory, "Weapon");
        Assert.Equal(15, weapons.GetProperty("capacity").GetInt32());
        var item = Assert.Single(weapons.GetProperty("items").EnumerateArray());
        Assert.Equal(sword, item.GetProperty("inventoryId").GetInt64());
        Assert.Equal("測試鐵劍", item.GetProperty("name").GetString());
        Assert.Equal(8, item.GetProperty("bonus").GetProperty("attack").GetInt32());
        Assert.Equal(10, item.GetProperty("requires").GetProperty("strength").GetInt32());
        Assert.Equal(5, Category(inventory, "Consumable").GetProperty("items")[0].GetProperty("quantity").GetInt32());
        Assert.Empty(inventory.GetProperty("equipment").EnumerateArray());
        Assert.Equal(0, inventory.GetProperty("bonus").GetProperty("attack").GetInt32());
    }

    [Fact]
    public async Task Get_ReturnsRefineStoneLevel_OnlyForRefineStones()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await GiveAsync(characterId, "test-sword");
        await GiveAsync(characterId, "test-refine-stone-2", 3);

        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);

        var stone = Assert.Single(Category(inventory, "RefineStone").GetProperty("items").EnumerateArray());
        Assert.Equal(2, stone.GetProperty("refineStoneLevel").GetInt32());
        var sword = Assert.Single(Category(inventory, "Weapon").GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, sword.GetProperty("refineStoneLevel").ValueKind);
    }

    [Fact]
    public async Task Get_ReturnsStoneNamesWithSockets()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).UpdateEnhancementAsync(
                characterId, sword, 0, StatBonus.Zero, [new SocketedStone(1, "test-stone", StatBonus.Zero with { Attack = 5 })], 1, CancellationToken);
        }

        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);

        var item = Assert.Single(Category(inventory, "Weapon").GetProperty("items").EnumerateArray());
        var socket = Assert.Single(item.GetProperty("sockets").EnumerateArray());
        Assert.Equal("test-stone", socket.GetProperty("stoneId").GetString());
        Assert.Equal("測試攻擊魔石", socket.GetProperty("stoneName").GetString());
    }

    // 測試鐵劍內容：bonus at 8、requires str 10；鑲上的魔石擲出 at 5
    [Fact]
    public async Task Inventory_ItemBonusUsesCompleteNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var sword = await GiveAsync(characterId, "test-sword");
        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).UpdateEnhancementAsync(
                characterId, sword, 0, StatBonus.Zero, [new SocketedStone(1, "test-stone", StatBonus.Zero with { Attack = 5 })], 1, CancellationToken);
        }

        var inventory = await client.GetFromJsonAsync<JsonElement>("/api/inventory", CancellationToken);

        var item = Assert.Single(Category(inventory, "Weapon").GetProperty("items").EnumerateArray());
        Assert.Equal(8, item.GetProperty("bonus").GetProperty("attack").GetInt32());
        Assert.False(item.GetProperty("bonus").TryGetProperty("at", out _));
        Assert.Equal(10, item.GetProperty("requires").GetProperty("strength").GetInt32());
        Assert.False(item.GetProperty("requires").TryGetProperty("str", out _));
        Assert.True(item.GetProperty("refineBonus").TryGetProperty("maxHealthPoints", out _));
        var socket = Assert.Single(item.GetProperty("sockets").EnumerateArray());
        Assert.Equal(5, socket.GetProperty("rolled").GetProperty("attack").GetInt32());
        Assert.True(inventory.GetProperty("bonus").TryGetProperty("magicDefense", out _));
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
        Assert.Equal(20, me.GetProperty("stats").GetProperty("attack").GetInt32());

        await ReadAsync(await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = ring }, CancellationToken));

        me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(62, me.GetProperty("stats").GetProperty("strength").GetInt32());
        Assert.Equal(70, me.GetProperty("stats").GetProperty("attack").GetInt32());
        Assert.Equal(12, me.GetProperty("baseStats").GetProperty("strength").GetInt32());
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
        Assert.Equal(["statTooLow:strength"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    // 測試魅力護符要求魅力 99，新角色的魅力是 10
    [Fact]
    public async Task Equip_StatReasonNamesTheStatInFull()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var charm = await GiveAsync(characterId, "test-charm");

        var response = await client.PostAsJsonAsync("/api/inventory/equip", new { inventoryId = charm }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Contains("statTooLow:charisma", problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
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
        Assert.Equal(13, me.GetProperty("baseStats").GetProperty("strength").GetInt32());
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
        Assert.Equal(350, me.GetProperty("stats").GetProperty("maxHealthPoints").GetInt32());
        Assert.Equal(350, me.GetProperty("healthPoints").GetInt32());
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

    // 測試魔石寶箱的候選只有測試攻擊魔石：回傳抽到的道具名稱與更新後的背包；其餘結果欄位為空值
    [Fact]
    public async Task Use_ReturnsObtainedNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var box = await GiveAsync(characterId, "test-stone-box");

        var result = await ReadAsync(await client.PostAsJsonAsync("/api/inventory/use", new { inventoryId = box }, CancellationToken));

        Assert.Equal(["測試攻擊魔石"], result.GetProperty("obtained").EnumerateArray().Select(name => name.GetString()));
        Assert.Equal(JsonValueKind.Null, result.GetProperty("petName").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("buff").ValueKind);
        Assert.Equal((0L, 0), (result.GetProperty("skillPoints").GetInt64(), result.GetProperty("levelsGained").GetInt32()));
        var stone = Assert.Single(Category(result.GetProperty("inventory"), "Stone").GetProperty("items").EnumerateArray());
        Assert.Equal("test-stone", stone.GetProperty("itemId").GetString());
        Assert.Empty(Category(result.GetProperty("inventory"), "Consumable").GetProperty("items").EnumerateArray());
    }

    // 直接寫入資料庫，模擬內容檔移除了這個道具 id
    private async Task<long> InsertRawInventoryAsync(long characterId, string itemId, string? sockets = null)
    {
        await using var context = database.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO character_inventory (character_id, item_id, quantity, sockets, holes) VALUES ({0}, {1}, 1, IF({2} = '', NULL, CAST({2} AS JSON)), 1)",
            [characterId, itemId, sockets ?? ""], CancellationToken);
        return await context.CharacterInventories.Where(row => row.CharacterId == characterId).MaxAsync(row => row.Id, CancellationToken);
    }

    // 背包、裝備欄（武器與補給）、倉庫都有已移除的道具，劍上鑲著已移除的魔石：各畫面照常 200，那些列不出現
    [Fact]
    public async Task RemovedItem_DoesNotBreakInventoryMeOrAdvance()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        await InsertRawInventoryAsync(characterId, "removed-item");
        var weapon = await InsertRawInventoryAsync(characterId, "removed-item");
        var supply = await InsertRawInventoryAsync(characterId, "removed-item");
        await InsertRawInventoryAsync(characterId, "test-sword", """[{"hole": 1, "stoneId": "removed-stone", "rolled": {"at": 3}}]""");
        await using (var context = database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO character_equipment (character_id, slot, inventory_id, supply_quantity) VALUES ({0}, 'Weapon', {1}, NULL), ({0}, 'Supply', {2}, 3)",
                [characterId, weapon, supply], CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO character_depot (character_id, item_id, quantity) VALUES ({0}, 'removed-item', 1)", [characterId], CancellationToken);
        }

        var inventory = await ReadAsync(await client.GetAsync("/api/inventory", CancellationToken));
        Assert.Empty(inventory.GetProperty("equipment").EnumerateArray());
        var sword = Assert.Single(inventory.GetProperty("bag").EnumerateArray().SelectMany(category => category.GetProperty("items").EnumerateArray()));
        Assert.Equal("test-sword", sword.GetProperty("itemId").GetString());
        Assert.Empty(sword.GetProperty("sockets").EnumerateArray());
        await ReadAsync(await client.GetAsync("/api/characters/me", CancellationToken));
        factory.Clock.Advance(TimeSpan.FromSeconds(60));
        await ReadAsync(await client.PostAsync("/api/adventure/advance", null, CancellationToken));
        var depot = await ReadAsync(await client.GetAsync("/api/depot", CancellationToken));
        Assert.Empty(depot.GetProperty("entries").EnumerateArray());
    }

    // test-sword 原有 1 孔、test-axe 原有 0 孔：精鍊、鑲嵌、多打的孔都算強化過
    [Fact]
    public async Task Inventory_MarksEnhancedEquipment()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var (client, characterId) = await CreateCharacterAsync(factory);
        var fresh = await GiveAsync(characterId, "test-sword");
        var refined = await GiveAsync(characterId, "test-sword");
        var socketed = await GiveAsync(characterId, "test-sword");
        var drilled = await GiveAsync(characterId, "test-axe");
        await using (var context = database.CreateDbContext())
        {
            var repository = new InventoryRepository(context, TestGameContent.Instance);
            await repository.UpdateEnhancementAsync(characterId, fresh, 0, StatBonus.Zero, [], 1, CancellationToken);
            await repository.UpdateEnhancementAsync(characterId, refined, 1, StatBonus.Zero with { Attack = 2 }, [], 1, CancellationToken);
            await repository.UpdateEnhancementAsync(
                characterId, socketed, 0, StatBonus.Zero, [new SocketedStone(1, "test-stone", StatBonus.Zero with { Attack = 5 })], 1, CancellationToken);
            await repository.UpdateEnhancementAsync(characterId, drilled, 0, StatBonus.Zero, [], 1, CancellationToken);
        }

        var inventory = await ReadAsync(await client.GetAsync("/api/inventory", CancellationToken));

        var enhanced = Category(inventory, "Weapon").GetProperty("items").EnumerateArray()
            .ToDictionary(item => item.GetProperty("inventoryId").GetInt64(), item => item.GetProperty("enhanced").GetBoolean());
        Assert.Equal(
            new Dictionary<long, bool> { [fresh] = false, [refined] = true, [socketed] = true, [drilled] = true },
            enhanced);
    }
}
