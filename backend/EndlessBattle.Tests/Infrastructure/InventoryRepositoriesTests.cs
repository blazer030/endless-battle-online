using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;

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
        return await new InventoryRepository(context).AddAsync(characterId, entry, CancellationToken);
    }

    private async Task<IReadOnlyList<InventoryEntry>> ListInventoryAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context).ListAsync(characterId, CancellationToken);
    }

    private async Task<Loadout> GetLoadoutAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new EquipmentRepository(context).GetAsync(characterId, CancellationToken);
    }

    private async Task SetSlotAsync(long characterId, EquipmentSlot slot, long? inventoryId, int? supplyQuantity = null)
    {
        await using var context = database.CreateDbContext();
        await new EquipmentRepository(context).SetSlotAsync(characterId, slot, inventoryId, supplyQuantity, CancellationToken);
    }

    [Fact]
    public async Task Inventory_RoundTripsEnhancements()
    {
        var characterId = await CreateCharacterAsync();
        var refineBonus = new StatBonus(1, 0, 2, 0, 0, 0, 0, 5, 0, 3, 0);
        var sockets = new[]
        {
            new SocketedStone(1, "stone_fire", new StatBonus(0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0)),
            new SocketedStone(2, "stone_random", new StatBonus(2, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 30)),
        };
        var entry = new InventoryEntry(0, "sword", 1, 3, refineBonus, sockets, 2);

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

        await AddInventoryAsync(characterId, Plain("potion", 5));

        var stored = Assert.Single(await ListInventoryAsync(characterId));
        Assert.Equal(StatBonus.Zero, stored.RefineBonus);
        Assert.Empty(stored.Sockets);
        Assert.Equal(5, stored.Quantity);
    }

    [Fact]
    public async Task Inventory_UpdateEnhancementPersists()
    {
        var characterId = await CreateCharacterAsync();
        var id = await AddInventoryAsync(characterId, Plain("sword"));
        var bonus = new StatBonus(0, 0, 0, 0, 0, 0, 0, 7, 0, 0, 0);
        var sockets = new[] { new SocketedStone(1, "stone_fire", bonus) };

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context).UpdateEnhancementAsync(characterId, id, 2, bonus, sockets, 1, CancellationToken);
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
        var id = await AddInventoryAsync(characterId, Plain("potion", 5));

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context).UpdateQuantityAsync(characterId, id, 9, CancellationToken);
        }

        Assert.Equal(9, Assert.Single(await ListInventoryAsync(characterId)).Quantity);

        await using (var context = database.CreateDbContext())
        {
            await new InventoryRepository(context).RemoveAsync(characterId, id, CancellationToken);
        }

        Assert.Empty(await ListInventoryAsync(characterId));
    }

    [Fact]
    public async Task Inventory_RejectsEntryOfAnotherCharacter()
    {
        var owner = await CreateCharacterAsync();
        var other = await CreateCharacterAsync();
        var id = await AddInventoryAsync(owner, Plain("potion", 5));

        await using var context = database.CreateDbContext();
        var repository = new InventoryRepository(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateQuantityAsync(other, id, 1, CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RemoveAsync(other, id, CancellationToken));
        Assert.Equal(5, Assert.Single(await ListInventoryAsync(owner)).Quantity);
    }

    [Fact]
    public async Task Equipment_SetAndClearSlot()
    {
        var characterId = await CreateCharacterAsync();
        var weaponId = await AddInventoryAsync(characterId, Plain("sword"));
        var supplyId = await AddInventoryAsync(characterId, Plain("potion", 8));

        await SetSlotAsync(characterId, EquipmentSlot.Weapon, weaponId);
        await SetSlotAsync(characterId, EquipmentSlot.Supply, supplyId, 3);

        var loadout = await GetLoadoutAsync(characterId);
        Assert.Equal("sword", loadout.Equipped[EquipmentSlot.Weapon].ItemId);
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
        var id = await AddInventoryAsync(owner, Plain("sword"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => SetSlotAsync(other, EquipmentSlot.Weapon, id));

        Assert.Empty((await GetLoadoutAsync(other)).Equipped);
    }

    [Fact]
    public async Task Depot_RoundTrips()
    {
        var characterId = await CreateCharacterAsync();
        var bonus = new StatBonus(0, 0, 0, 0, 0, 0, 0, 6, 0, 0, 0);
        var sockets = new[] { new SocketedStone(1, "stone_fire", bonus) };
        var entry = new InventoryEntry(0, "sword", 1, 2, bonus, sockets, 1);

        long id;
        await using (var context = database.CreateDbContext())
        {
            id = await new DepotRepository(context).AddAsync(characterId, entry, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new DepotRepository(context);
            var stored = Assert.Single(await repository.ListAsync(characterId, CancellationToken));
            Assert.Equal((id, "sword", 1, 2, 1), (stored.Id, stored.ItemId, stored.Quantity, stored.RefineLevel, stored.Holes));
            Assert.Equal(bonus, stored.RefineBonus);
            Assert.Equal(sockets, stored.Sockets);
            await repository.UpdateQuantityAsync(characterId, id, 4, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new DepotRepository(context);
            Assert.Equal(4, Assert.Single(await repository.ListAsync(characterId, CancellationToken)).Quantity);
            await repository.RemoveAsync(characterId, id, CancellationToken);
            Assert.Empty(await repository.ListAsync(characterId, CancellationToken));
        }
    }
}
