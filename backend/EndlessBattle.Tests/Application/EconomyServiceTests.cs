using EndlessBattle.Application.Common;
using EndlessBattle.Application.Economy;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-sword（價 200、商店賣、1 孔）、test-supply（價 20、商店賣）、test-axe（價 200、不賣）、test-bound-sword（綁定、價 400）。
// 預設角色金錢 2000、Lv 1
public class EconomyServiceTests
{
    private const long AccountId = 7;
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemoryDepotRepository depot = new();
    private readonly InventoryService inventoryService;
    private readonly ShopService shop;
    private readonly BankService bank;
    private readonly DepotService depotService;

    public EconomyServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        inventoryService = new InventoryService(characters, inventory, equipment, Content);
        shop = new ShopService(characters, inventory, equipment, Content, inventoryService);
        bank = new BankService(characters);
        depotService = new DepotService(characters, inventory, equipment, depot, Content, inventoryService);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    private Task<long> GiveAsync(Character character, string itemId, int quantity = 1) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity) with { Holes = Content.GetItem(itemId).Holes }, CancellationToken);

    private async Task<Character> ReloadAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    private static IEnumerable<(string ItemId, int Quantity)> BagOf(InventoryView view, ItemCategory category) =>
        view.Bag.Single(bag => bag.Category == category).Items.Select(item => (item.Entry.ItemId, item.Entry.Quantity));

    private static async Task<IReadOnlyList<string>> ReasonsAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<InputValidationException>(action)).Reasons;

    [Fact]
    public async Task Buy_DeductsMoneyAndAddsToBag()
    {
        await AddCharacterAsync();

        var view = await shop.BuyAsync(AccountId, "test-sword", 1, CancellationToken);

        Assert.Equal(1800, (await ReloadAsync()).Money);
        Assert.Equal([("test-sword", 1)], BagOf(view, ItemCategory.Weapon));
        Assert.Equal(1, view.Bag.Single(bag => bag.Category == ItemCategory.Weapon).Items.Single().Entry.Holes);
    }

    [Fact]
    public async Task Buy_StacksConsumables()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-supply", 90);

        var view = await shop.BuyAsync(AccountId, "test-supply", 20, CancellationToken);

        Assert.Equal(1600, (await ReloadAsync()).Money);
        Assert.Equal([("test-supply", 99), ("test-supply", 11)], BagOf(view, ItemCategory.Consumable));
    }

    [Fact]
    public async Task Buy_RejectsWhenBagFull()
    {
        var character = await AddCharacterAsync();
        for (var index = 0; index < 15; index++)
        {
            await GiveAsync(character, "test-axe");
        }

        Assert.Equal(["bagFull"], await ReasonsAsync(() => shop.BuyAsync(AccountId, "test-sword", 1, CancellationToken)));
        Assert.Equal(2000, (await ReloadAsync()).Money);
    }

    [Fact]
    public async Task Buy_RejectsNotForSaleAndPoorAndBadQuantity()
    {
        await AddCharacterAsync(character => character with { Money = 100 });

        Assert.Equal(["notForSale"], await ReasonsAsync(() => shop.BuyAsync(AccountId, "test-axe", 1, CancellationToken)));
        Assert.Equal(["notForSale"], await ReasonsAsync(() => shop.BuyAsync(AccountId, "no-such-item", 1, CancellationToken)));
        Assert.Equal(["notEnoughMoney"], await ReasonsAsync(() => shop.BuyAsync(AccountId, "test-sword", 1, CancellationToken)));
        Assert.Equal(["invalidQuantity"], await ReasonsAsync(() => shop.BuyAsync(AccountId, "test-supply", 100, CancellationToken)));
    }

    [Fact]
    public async Task Buy_ConflictWritesNothing()
    {
        await AddCharacterAsync();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => shop.BuyAsync(AccountId, "test-sword", 1, CancellationToken));

        Assert.Empty(await inventory.ListAsync((await ReloadAsync()).Id, CancellationToken));
        Assert.Equal(2000, (await ReloadAsync()).Money);
    }

    [Fact]
    public void List_RejectsLevelOutOfRange() =>
        Assert.Equal(["invalidLevel"], Assert.Throws<InputValidationException>(() => shop.List(ItemCategory.Weapon, 8)).Reasons);

    // 精鍊 +2、鑲嵌、打洞都不影響賣價：floor(200 × 0.5) = 100，列刪除
    [Fact]
    public async Task Sell_IgnoresEnhancements()
    {
        var character = await AddCharacterAsync();
        var id = await inventory.AddAsync(
            character.Id,
            TestItems.Entry(0, "test-sword") with
            {
                RefineLevel = 2,
                RefineBonus = StatBonus.Zero with { At = 3 },
                Holes = 1,
                Sockets = [new SocketedStone(1, "test-stone", StatBonus.Zero with { At = 5 })],
            },
            CancellationToken);

        var view = await shop.SellAsync(AccountId, id, 1, CancellationToken);

        Assert.Equal(2100, (await ReloadAsync()).Money);
        Assert.Empty(BagOf(view, ItemCategory.Weapon));
        Assert.Empty(await inventory.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Sell_AllowsBound()
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, "test-bound-sword");

        await shop.SellAsync(AccountId, id, 1, CancellationToken);

        Assert.Equal(2200, (await ReloadAsync()).Money);
    }

    [Fact]
    public async Task Sell_RejectsEquipped()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await inventoryService.EquipAsync(AccountId, sword, null, CancellationToken);

        Assert.Equal(["equipped"], await ReasonsAsync(() => shop.SellAsync(AccountId, sword, 1, CancellationToken)));
        Assert.Single(await inventory.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Sell_PartialStack()
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, "test-supply", 10);

        var view = await shop.SellAsync(AccountId, id, 3, CancellationToken);

        Assert.Equal(2030, (await ReloadAsync()).Money);
        Assert.Equal([("test-supply", 7)], BagOf(view, ItemCategory.Consumable));
        Assert.Equal(["invalidQuantity"], await ReasonsAsync(() => shop.SellAsync(AccountId, id, 8, CancellationToken)));
    }

    [Fact]
    public async Task Sell_RejectsMissingItem()
    {
        await AddCharacterAsync();

        Assert.Equal(["itemNotFound"], await ReasonsAsync(() => shop.SellAsync(AccountId, 999, 1, CancellationToken)));
    }

    [Fact]
    public async Task Sell_ConflictWritesNothing()
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, "test-sword");
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => shop.SellAsync(AccountId, id, 1, CancellationToken));

        Assert.Single(await inventory.ListAsync(character.Id, CancellationToken));
        Assert.Equal(2000, (await ReloadAsync()).Money);
    }

    [Fact]
    public async Task Bank_RequiresLevel15()
    {
        await AddCharacterAsync();

        Assert.Equal(["levelTooLow"], await ReasonsAsync(() => bank.DepositAsync(AccountId, 100, CancellationToken)));
        Assert.Equal(2000, (await ReloadAsync()).Money);
    }

    [Fact]
    public async Task Bank_DepositsAtLevel15_AndWithdrawsAnytime()
    {
        await AddCharacterAsync(character => character with { Level = 15 });

        var deposited = await bank.DepositAsync(AccountId, 1500, CancellationToken);
        Assert.Equal((500L, 1500L), (deposited.Money, deposited.Bank));

        // 等級掉回 15 以下不影響領出
        await characters.UpdateAsync(deposited with { Level = 1 }, CancellationToken);
        var withdrawn = await bank.WithdrawAsync(AccountId, 1000, CancellationToken);

        Assert.Equal((1500L, 500L), (withdrawn.Money, withdrawn.Bank));
    }

    [Fact]
    public async Task Bank_RejectsOverdraft()
    {
        await AddCharacterAsync(character => character with { Level = 15, Bank = 100 });

        Assert.Equal(["notEnoughMoney"], await ReasonsAsync(() => bank.DepositAsync(AccountId, 2001, CancellationToken)));
        Assert.Equal(["notEnoughBank"], await ReasonsAsync(() => bank.WithdrawAsync(AccountId, 101, CancellationToken)));
        Assert.Equal(["invalidAmount"], await ReasonsAsync(() => bank.WithdrawAsync(AccountId, 0, CancellationToken)));
    }

    // 精鍊與鑲嵌存進倉庫、再取回都保留
    [Fact]
    public async Task Depot_StoresAndTakes()
    {
        var character = await AddCharacterAsync();
        var id = await inventory.AddAsync(
            character.Id,
            TestItems.Entry(0, "test-sword") with { RefineLevel = 3, RefineBonus = StatBonus.Zero with { At = 4 }, Holes = 1 },
            CancellationToken);

        var stored = await depotService.StoreAsync(AccountId, id, 1, CancellationToken);

        Assert.Empty(BagOf(stored.Inventory, ItemCategory.Weapon));
        var inDepot = Assert.Single(stored.Depot.Entries).Entry;
        Assert.Equal((3, 4, 1), (inDepot.RefineLevel, inDepot.RefineBonus.At, inDepot.Holes));
        Assert.Equal(20, stored.Depot.Capacity);

        var taken = await depotService.TakeAsync(AccountId, inDepot.Id, 1, CancellationToken);

        Assert.Empty(taken.Depot.Entries);
        var back = taken.Inventory.Bag.Single(bag => bag.Category == ItemCategory.Weapon).Items.Single().Entry;
        Assert.Equal((3, 4, 1), (back.RefineLevel, back.RefineBonus.At, back.Holes));
    }

    [Fact]
    public async Task Depot_StackLimit254()
    {
        var character = await AddCharacterAsync();
        var first = await GiveAsync(character, "test-supply", 99);
        await depotService.StoreAsync(AccountId, first, 99, CancellationToken);
        var second = await GiveAsync(character, "test-supply", 99);
        var merged = await depotService.StoreAsync(AccountId, second, 99, CancellationToken);
        Assert.Equal(198, Assert.Single(merged.Depot.Entries).Entry.Quantity);
        var third = await GiveAsync(character, "test-supply", 99);

        Assert.Equal(["stackLimit"], await ReasonsAsync(() => depotService.StoreAsync(AccountId, third, 99, CancellationToken)));

        var partial = await depotService.StoreAsync(AccountId, third, 56, CancellationToken);
        Assert.Equal(254, Assert.Single(partial.Depot.Entries).Entry.Quantity);
        Assert.Equal([("test-supply", 43)], BagOf(partial.Inventory, ItemCategory.Consumable));
    }

    [Fact]
    public async Task Depot_RejectsWhenFull()
    {
        var character = await AddCharacterAsync();
        for (var index = 0; index < 20; index++)
        {
            await depot.AddAsync(character.Id, TestItems.Entry(0, "test-axe"), CancellationToken);
        }

        var sword = await GiveAsync(character, "test-sword");

        Assert.Equal(["depotFull"], await ReasonsAsync(() => depotService.StoreAsync(AccountId, sword, 1, CancellationToken)));
    }

    [Fact]
    public async Task Depot_CapacityIncludesBonus()
    {
        var character = await AddCharacterAsync(character => character with { DepotBonus = 5 });
        for (var index = 0; index < 20; index++)
        {
            await depot.AddAsync(character.Id, TestItems.Entry(0, "test-axe"), CancellationToken);
        }

        var sword = await GiveAsync(character, "test-sword");
        var stored = await depotService.StoreAsync(AccountId, sword, 1, CancellationToken);

        Assert.Equal((21, 25), (stored.Depot.Entries.Count, stored.Depot.Capacity));
    }

    [Fact]
    public async Task Take_RejectsWhenBagFull()
    {
        var character = await AddCharacterAsync();
        for (var index = 0; index < 15; index++)
        {
            await GiveAsync(character, "test-axe");
        }

        var stored = await depot.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);

        Assert.Equal(["bagFull"], await ReasonsAsync(() => depotService.TakeAsync(AccountId, stored, 1, CancellationToken)));
        Assert.Single(await depot.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Depot_AllowsBound()
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, "test-bound-sword");

        var stored = await depotService.StoreAsync(AccountId, id, 1, CancellationToken);

        Assert.Equal("test-bound-sword", Assert.Single(stored.Depot.Entries).Entry.ItemId);
    }

    [Fact]
    public async Task Store_RejectsEquipped()
    {
        var character = await AddCharacterAsync();
        var sword = await GiveAsync(character, "test-sword");
        await inventoryService.EquipAsync(AccountId, sword, null, CancellationToken);

        Assert.Equal(["equipped"], await ReasonsAsync(() => depotService.StoreAsync(AccountId, sword, 1, CancellationToken)));
    }

    [Fact]
    public async Task Store_ConflictWritesNothing()
    {
        var character = await AddCharacterAsync();
        var id = await GiveAsync(character, "test-sword");
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => depotService.StoreAsync(AccountId, id, 1, CancellationToken));

        Assert.Empty(await depot.ListAsync(character.Id, CancellationToken));
        Assert.Single(await inventory.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Take_ConflictWritesNothing()
    {
        var character = await AddCharacterAsync();
        var stored = await depot.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => depotService.TakeAsync(AccountId, stored, 1, CancellationToken));

        Assert.Single(await depot.ListAsync(character.Id, CancellationToken));
        Assert.Empty(await inventory.ListAsync(character.Id, CancellationToken));
    }
}
