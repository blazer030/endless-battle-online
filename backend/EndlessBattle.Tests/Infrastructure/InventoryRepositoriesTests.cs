using System.Text.Json.Nodes;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class InventoryRepositoriesTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"背包{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    private static InventoryEntry Plain(string itemId, int quantity = 1) =>
        new(0, itemId, quantity, 0, StatBonus.Zero, [], 0);

    private async Task<long> AddInventoryAsync(long characterId, InventoryEntry entry)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context, TestGameContent.Instance).AddAsync(characterId, entry, CancellationToken);
    }

    private async Task<IReadOnlyList<InventoryEntry>> ListInventoryAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context, TestGameContent.Instance).ListAsync(characterId, CancellationToken);
    }

    private async Task<Loadout> GetLoadoutAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new EquipmentRepository(context, TestGameContent.Instance).GetAsync(characterId, CancellationToken);
    }

    private async Task SetSlotAsync(long characterId, EquipmentSlot slot, long? inventoryId, int? supplyQuantity = null)
    {
        await using var context = database.CreateDbContext();
        await new EquipmentRepository(context, TestGameContent.Instance).SetSlotAsync(characterId, slot, inventoryId, supplyQuantity, CancellationToken);
    }

    private async Task<string?> ReadRawColumnAsync(string column, long id)
    {
        await using var context = database.CreateDbContext();
        var query = column == "sockets"
            ? context.Database.SqlQueryRaw<string?>("SELECT sockets AS Value FROM character_inventory WHERE id = {0}", id)
            : context.Database.SqlQueryRaw<string?>("SELECT refine_bonus AS Value FROM character_inventory WHERE id = {0}", id);
        return Assert.Single(await query.ToListAsync(CancellationToken));
    }

    [Fact]
    public async Task Inventory_StoresEnhancementsAsCamelCaseJson()
    {
        var characterId = await CreateCharacterAsync();
        var entry = new InventoryEntry(
            0, "test-sword", 1, 1, new StatBonus(1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, MaxHealthPoints: 30),
            [new SocketedStone(1, "test-stone", StatBonus.Zero), new SocketedStone(2, "test-stone", StatBonus.Zero)], 2);

        var id = await AddInventoryAsync(characterId, entry);

        var sockets = await ReadRawColumnAsync("sockets", id);
        var refineBonus = await ReadRawColumnAsync("refine_bonus", id);
        Assert.Contains("\"stoneId\"", sockets);
        Assert.Contains("\"rolled\"", sockets);
        Assert.DoesNotContain("\"StoneId\"", sockets);
        Assert.Contains("\"hpMax\"", refineBonus);
        Assert.DoesNotContain("\"HpMax\"", refineBonus);
    }

    [Fact]
    public async Task Inventory_StoresNullWhenNoEnhancements()
    {
        var characterId = await CreateCharacterAsync();

        var id = await AddInventoryAsync(characterId, Plain("test-supply", 3));

        Assert.Null(await ReadRawColumnAsync("sockets", id));
        Assert.Null(await ReadRawColumnAsync("refine_bonus", id));
    }

    [Fact]
    public async Task Equipment_SetSlot_SwapsInventoryId()
    {
        var characterId = await CreateCharacterAsync();
        var first = await AddInventoryAsync(characterId, Plain("test-sword"));
        var second = await AddInventoryAsync(characterId, Plain("test-axe"));

        await SetSlotAsync(characterId, EquipmentSlot.Weapon, first);
        await SetSlotAsync(characterId, EquipmentSlot.Weapon, second);

        var loadout = await GetLoadoutAsync(characterId);
        Assert.Equal(second, loadout.Equipped[EquipmentSlot.Weapon].Id);
        await using var context = database.CreateDbContext();
        Assert.Equal(1, await context.CharacterEquipments.CountAsync(row => row.CharacterId == characterId, CancellationToken));
    }

    [Fact]
    public async Task Inventory_RoundTripsEnhancements()
    {
        var characterId = await CreateCharacterAsync();
        var refineBonus = new StatBonus(1, 0, 2, 0, 0, 0, 0, 5, 0, 3, 0);
        var sockets = new[]
        {
            new SocketedStone(1, "test-stone", new StatBonus(0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0)),
            new SocketedStone(2, "test-stone", new StatBonus(2, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 30)),
        };
        var entry = new InventoryEntry(0, "test-sword", 1, 3, refineBonus, sockets, 2);

        var id = await AddInventoryAsync(characterId, entry);

        var stored = Assert.Single(await ListInventoryAsync(characterId));
        Assert.Equal(id, stored.Id);
        Assert.Equal((entry.ItemId, entry.Quantity, entry.RefineLevel, entry.Holes), (stored.ItemId, stored.Quantity, stored.RefineLevel, stored.Holes));
        Assert.Equal(refineBonus, stored.RefineBonus);
        Assert.Equal(sockets, stored.Sockets);
    }

    [Fact]
    public async Task Inventory_PlainEntryRoundTripsAsEmptyEnhancements()
    {
        var characterId = await CreateCharacterAsync();

        await AddInventoryAsync(characterId, Plain("test-supply", 5));

        var stored = Assert.Single(await ListInventoryAsync(characterId));
        Assert.Equal(StatBonus.Zero, stored.RefineBonus);
        Assert.Empty(stored.Sockets);
        Assert.Equal(5, stored.Quantity);
    }

    [Fact]
    public async Task Inventory_UpdateEnhancementPersists()
    {
        var characterId = await CreateCharacterAsync();
        var id = await AddInventoryAsync(characterId, Plain("test-sword"));
        var bonus = new StatBonus(0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0);
        var sockets = new[] { new SocketedStone(1, "test-stone", bonus) };

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).UpdateEnhancementAsync(characterId, id, 2, bonus, sockets, 1, CancellationToken);
        }

        var stored = Assert.Single(await ListInventoryAsync(characterId));
        Assert.Equal((2, 1), (stored.RefineLevel, stored.Holes));
        Assert.Equal(bonus, stored.RefineBonus);
        Assert.Equal(sockets, stored.Sockets);
    }

    [Fact]
    public async Task Inventory_UpdateQuantityAndRemove()
    {
        var characterId = await CreateCharacterAsync();
        var id = await AddInventoryAsync(characterId, Plain("test-supply", 5));

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).UpdateQuantityAsync(characterId, id, 9, CancellationToken);
        }

        Assert.Equal(9, Assert.Single(await ListInventoryAsync(characterId)).Quantity);

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context, TestGameContent.Instance).RemoveAsync(characterId, id, CancellationToken);
        }

        Assert.Empty(await ListInventoryAsync(characterId));
    }

    [Fact]
    public async Task Inventory_RejectsEntryOfAnotherCharacter()
    {
        var owner = await CreateCharacterAsync();
        var other = await CreateCharacterAsync();
        var id = await AddInventoryAsync(owner, Plain("test-supply", 5));

        await using var context = database.CreateDbContext();
        var repository = new InventoryRepository(context, TestGameContent.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateQuantityAsync(other, id, 1, CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RemoveAsync(other, id, CancellationToken));
        Assert.Equal(5, Assert.Single(await ListInventoryAsync(owner)).Quantity);
    }

    [Fact]
    public async Task Equipment_SetAndClearSlot()
    {
        var characterId = await CreateCharacterAsync();
        var weaponId = await AddInventoryAsync(characterId, Plain("test-sword"));
        var supplyId = await AddInventoryAsync(characterId, Plain("test-supply", 8));

        await SetSlotAsync(characterId, EquipmentSlot.Weapon, weaponId);
        await SetSlotAsync(characterId, EquipmentSlot.Supply, supplyId, 3);

        var loadout = await GetLoadoutAsync(characterId);
        Assert.Equal("test-sword", loadout.Equipped[EquipmentSlot.Weapon].ItemId);
        Assert.Equal(supplyId, loadout.Equipped[EquipmentSlot.Supply].Id);
        Assert.Equal(3, loadout.SupplyQuantity);

        await SetSlotAsync(characterId, EquipmentSlot.Weapon, null);
        await SetSlotAsync(characterId, EquipmentSlot.Supply, supplyId, 5);

        loadout = await GetLoadoutAsync(characterId);
        Assert.False(loadout.Equipped.ContainsKey(EquipmentSlot.Weapon));
        Assert.Equal(5, loadout.SupplyQuantity);

        await SetSlotAsync(characterId, EquipmentSlot.Supply, null);

        loadout = await GetLoadoutAsync(characterId);
        Assert.Empty(loadout.Equipped);
        Assert.Equal(0, loadout.SupplyQuantity);
    }

    [Fact]
    public async Task Equipment_RejectsInventoryOfAnotherCharacter()
    {
        var owner = await CreateCharacterAsync();
        var other = await CreateCharacterAsync();
        var id = await AddInventoryAsync(owner, Plain("test-sword"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => SetSlotAsync(other, EquipmentSlot.Weapon, id));

        Assert.Empty((await GetLoadoutAsync(other)).Equipped);
    }

    [Fact]
    public async Task Depot_RoundTrips()
    {
        var characterId = await CreateCharacterAsync();
        var bonus = new StatBonus(0, 0, 0, 0, 0, 0, 0, 6, 0, 0, 0);
        var sockets = new[] { new SocketedStone(1, "test-stone", bonus) };
        var entry = new InventoryEntry(0, "test-sword", 1, 2, bonus, sockets, 1);

        long id;
        await using (var context = database.CreateDbContext())
        {
            id = await new DepotRepository(context, TestGameContent.Instance).AddAsync(characterId, entry, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new DepotRepository(context, TestGameContent.Instance);
            var stored = Assert.Single(await repository.ListAsync(characterId, CancellationToken));
            Assert.Equal((id, "test-sword", 1, 2, 1), (stored.Id, stored.ItemId, stored.Quantity, stored.RefineLevel, stored.Holes));
            Assert.Equal(bonus, stored.RefineBonus);
            Assert.Equal(sockets, stored.Sockets);
            await repository.UpdateQuantityAsync(characterId, id, 4, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new DepotRepository(context, TestGameContent.Instance);
            Assert.Equal(4, Assert.Single(await repository.ListAsync(characterId, CancellationToken)).Quantity);
            await repository.RemoveAsync(characterId, id, CancellationToken);
            Assert.Empty(await repository.ListAsync(characterId, CancellationToken));
        }
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

    [Fact]
    public async Task List_SkipsRowsWhoseItemIsNotInContent()
    {
        var characterId = await CreateCharacterAsync();
        var sword = await AddInventoryAsync(characterId, Plain("test-sword"));
        var removed = await InsertRawInventoryAsync(characterId, "removed-item");
        await using (var context = database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO character_equipment (character_id, slot, inventory_id) VALUES ({0}, 'Weapon', {1})", [characterId, removed], CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO character_depot (character_id, item_id, quantity) VALUES ({0}, 'removed-item', 1), ({0}, 'test-supply', 2)",
                [characterId], CancellationToken);
        }

        Assert.Equal([sword], (await ListInventoryAsync(characterId)).Select(entry => entry.Id));
        Assert.Empty((await GetLoadoutAsync(characterId)).Equipped);
        await using (var context = database.CreateDbContext())
        {
            var depot = await new DepotRepository(context, TestGameContent.Instance).ListAsync(characterId, CancellationToken);
            Assert.Equal(["test-supply"], depot.Select(entry => entry.ItemId));
            // 資料庫保留那幾列，內容加回來就恢復
            Assert.Equal(2, await context.CharacterInventories.CountAsync(row => row.CharacterId == characterId, CancellationToken));
            Assert.Equal(2, await context.CharacterDepots.CountAsync(row => row.CharacterId == characterId, CancellationToken));
        }
    }

    [Fact]
    public async Task List_DropsSocketsWithUnknownStone()
    {
        var characterId = await CreateCharacterAsync();
        const string sockets = """[{"hole": 1, "stoneId": "removed-stone", "rolled": {"at": 3}}, {"hole": 2, "stoneId": "test-stone", "rolled": {"at": 5}}]""";
        var id = await InsertRawInventoryAsync(characterId, "test-sword", sockets);
        await SetSlotAsync(characterId, EquipmentSlot.Weapon, id);

        var listed = Assert.Single(await ListInventoryAsync(characterId));
        Assert.Equal(["test-stone"], listed.Sockets.Select(socket => socket.StoneId));
        var equipped = (await GetLoadoutAsync(characterId)).Equipped[EquipmentSlot.Weapon];
        Assert.Equal(["test-stone"], equipped.Sockets.Select(socket => socket.StoneId));
    }

    private const string LegacyRefineBonus =
        "{\"str\":2,\"smart\":0,\"life\":0,\"vit\":0,\"agi\":0,\"au\":0,\"be\":0,\"at\":3,\"mat\":0,\"df\":0,\"mdf\":0,\"hpMax\":0}";

    private const string LegacySockets =
        "[{\"hole\":1,\"stoneId\":\"test-stone\",\"rolled\":{\"str\":0,\"smart\":0,\"life\":0,\"vit\":0,\"agi\":0,\"au\":0,\"be\":0,\"at\":5,\"mat\":0,\"df\":0,\"mdf\":0,\"hpMax\":30}}]";

    [Fact]
    public async Task EnhancementJson_ReadsStoredLegacyKeys()
    {
        var characterId = await CreateCharacterAsync();
        var id = await AddInventoryAsync(characterId, Plain("test-sword"));
        await using (var context = database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE character_inventory SET refine_bonus = {0}, sockets = {1} WHERE id = {2}",
                [LegacyRefineBonus, LegacySockets, id],
                CancellationToken);
        }

        var entry = Assert.Single(await ListInventoryAsync(characterId));

        Assert.Equal(new StatBonus(2, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0), entry.RefineBonus);
        var socket = Assert.Single(entry.Sockets);
        Assert.Equal(1, socket.Hole);
        Assert.Equal("test-stone", socket.StoneId);
        Assert.Equal(new StatBonus(0, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, MaxHealthPoints: 30), socket.Rolled);
    }

    [Fact]
    public async Task EnhancementJson_WritesTheSameKeys()
    {
        var characterId = await CreateCharacterAsync();
        var entry = new InventoryEntry(
            0, "test-sword", 1, 1, new StatBonus(2, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0),
            [new SocketedStone(1, "test-stone", new StatBonus(0, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, MaxHealthPoints: 30))], 1);

        var id = await AddInventoryAsync(characterId, entry);

        // MySQL 的 JSON 欄位會重排鍵與空白，比對解析後的內容
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(LegacyRefineBonus), JsonNode.Parse(await ReadRawColumnAsync("refine_bonus", id) ?? "null")));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(LegacySockets), JsonNode.Parse(await ReadRawColumnAsync("sockets", id) ?? "null")));
    }
}
