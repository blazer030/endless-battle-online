using EndlessBattle.Application.Common;
using EndlessBattle.Application.Honour;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-exchange-ore（test-ore ×1，花 test-medal-a ×2）、test-exchange-mercenary（test-honour-mercenary，花 test-medal-a ×1 ＋ test-medal-b ×1）
public class HonourServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryInventoryRepository inventory;
    private readonly InMemoryMercenaryRepository mercenaries;
    private readonly HonourService service;

    public HonourServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        inventory = new InMemoryInventoryRepository(log);
        mercenaries = new InMemoryMercenaryRepository(log);
        var equipment = new InMemoryEquipmentRepository(inventory);
        var unitOfWork = new InMemoryUnitOfWork(characters, inventory, mercenaries);
        service = new HonourService(characters, inventory, equipment, mercenaries, Content, new FakeClock(Now), unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> AddCharacterAsync() =>
        await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch), CancellationToken);

    private Task<long> GiveAsync(Character character, string itemId, int quantity) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity), CancellationToken);

    private Task<IReadOnlyList<InventoryEntry>> BagAsync(Character character) => inventory.ListAsync(character.Id, CancellationToken);

    private async Task<long> VersionAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version;

    [Fact]
    public async Task Get_ListsOffersAndMedalsInContentOrder()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-medal-a", 2);

        var overview = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(["test-exchange-ore", "test-exchange-mercenary"], overview.Offers.Select(offer => offer.Exchange.Id));
        Assert.Equal([true, false], overview.Offers.Select(offer => offer.Affordable));
        Assert.Equal(["test-medal-a", "test-medal-b"], overview.Medals.Keys);
        Assert.Equal([2, 0], overview.Medals.Values);
    }

    private HonourService RealService() => new(
        characters,
        inventory,
        new InMemoryEquipmentRepository(inventory),
        mercenaries,
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content")),
        new FakeClock(Now),
        new InMemoryUnitOfWork(characters, inventory, mercenaries));

    // 規格 honour「Listing with medals in the bag」的 affordability 表（正式內容）
    [Fact]
    public async Task Get_RealAffordability()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "item-1304", 14);
        await GiveAsync(character, "item-1305", 6);

        var overview = await RealService().GetAsync(AccountId, CancellationToken);

        Assert.Equal([14, 6, 0, 0, 0], overview.Medals.Values);
        var affordable = overview.Offers.ToDictionary(offer => offer.Exchange.Id, offer => offer.Affordable);
        Assert.Equal(
            [true, true, false, false],
            new[] { "honour-9", "honour-12", "honour-1", "honour-142" }.Select(id => affordable[id]));
    }

    // 規格 honour「Weapon requirement not met」：需求比對裸點（力 300）；沒有需求的不列
    [Fact]
    public async Task Get_RequirementMetUsesBaseStats()
    {
        await characters.AddAsync(
            TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with
            {
                Base = new BaseStats(Strength: 300, Intelligence: 12, Life: 12, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10),
            },
            CancellationToken);

        var overview = await RealService().GetAsync(AccountId, CancellationToken);

        var offers = overview.Offers.ToDictionary(offer => offer.Exchange.Id);
        Assert.Equal([new HonourRequirement(BaseStat.Strength, 435, false)], offers["honour-11"].Requirements);
        Assert.Equal([new HonourRequirement(BaseStat.Strength, 145, true)], offers["honour-9"].Requirements);
        Assert.Empty(offers["honour-133"].Requirements);
        Assert.Empty(offers["honour-1"].Requirements);
    }

    // 規格 honour「Hired mercenary name」
    [Fact]
    public async Task Get_HiredMercenaryName()
    {
        var character = await AddCharacterAsync();
        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).HiredMercenaryName);

        await mercenaries.AddAsync(character.Id, new MercenaryContract("test-honour-mercenary", "葛雷", 5, null, null, Now), CancellationToken);

        Assert.Equal("葛雷", (await service.GetAsync(AccountId, CancellationToken)).HiredMercenaryName);
    }

    // 規格 honour「Exchanging for an item」：大陸 14 換巨石槌（honour-9，花大陸 2）
    [Fact]
    public async Task Exchange_FromTheRealContent()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "item-1304", 14);

        var overview = await RealService().ExchangeAsync(AccountId, "honour-9", CancellationToken);

        var bag = await BagAsync(character);
        Assert.Equal(12, Assert.Single(bag, entry => entry.ItemId == "item-1304").Quantity);
        Assert.Equal(1, Assert.Single(bag, entry => entry.ItemId == "item-1384").Quantity);
        Assert.Equal(12, overview.Medals["item-1304"]);
    }

    // 規格 honour「Exchanging for a mercenary」：大陸、王國各 13 換 C級傭兵（honour-1）
    [Fact]
    public async Task Exchange_RealMercenaryStartsResting()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "item-1304", 13);
        await GiveAsync(character, "item-1305", 13);

        await RealService().ExchangeAsync(AccountId, "honour-1", CancellationToken);

        Assert.Empty(await BagAsync(character));
        var contract = await mercenaries.FindAsync(character.Id, CancellationToken);
        Assert.Equal(("mercenary-4", "C級傭兵", false), (contract!.MercenaryId, contract.Name, contract.Fighting));
    }

    [Fact]
    public async Task Exchange_PaysMedalsAndAddsItem()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-medal-a", 3);
        log.Clear();

        var overview = await service.ExchangeAsync(AccountId, "test-exchange-ore", CancellationToken);

        var bag = await BagAsync(character);
        Assert.Equal(1, Assert.Single(bag, entry => entry.ItemId == "test-medal-a").Quantity);
        Assert.Equal(1, Assert.Single(bag, entry => entry.ItemId == "test-ore").Quantity);
        Assert.Equal(1, overview.Medals["test-medal-a"]);
        Assert.Equal(1, await VersionAsync());
    }

    // 角色版本鎖最先寫，之後扣勳章、最後新增傭兵合約
    [Fact]
    public async Task Exchange_WritesCharacterVersionFirst()
    {
        var character = await AddCharacterAsync();
        var medalA = await GiveAsync(character, "test-medal-a", 1);
        var medalB = await GiveAsync(character, "test-medal-b", 2);
        log.Clear();

        await service.ExchangeAsync(AccountId, "test-exchange-mercenary", CancellationToken);

        Assert.Equal(["update", $"quantity:{medalB}", $"remove:{medalA}", "mercenary:add"], log);
        var contract = await mercenaries.FindAsync(character.Id, CancellationToken);
        Assert.Equal(new MercenaryContract("test-honour-mercenary", "測試榮譽傭兵", 5, null, null, Now), contract);
        Assert.Equal(1, Assert.Single(await BagAsync(character)).Quantity);
    }

    [Fact]
    public async Task Exchange_MissingMedalsChangesNothing()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-medal-a", 1);
        log.Clear();

        var error = await Assert.ThrowsAsync<InputValidationException>(
            () => service.ExchangeAsync(AccountId, "test-exchange-mercenary", CancellationToken));

        Assert.Equal(["medalsMissing"], error.Reasons);
        Assert.Equal(1, Assert.Single(await BagAsync(character)).Quantity);
        Assert.Null(await mercenaries.FindAsync(character.Id, CancellationToken));
        Assert.Equal(0, await VersionAsync());
        Assert.Empty(log);
    }

    [Fact]
    public async Task Exchange_RejectsUnknownAndSecondMercenary()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-medal-a", 2);
        await GiveAsync(character, "test-medal-b", 2);
        await service.ExchangeAsync(AccountId, "test-exchange-mercenary", CancellationToken);
        log.Clear();

        var unknown = await Assert.ThrowsAsync<InputValidationException>(() => service.ExchangeAsync(AccountId, "ghost", CancellationToken));
        var again = await Assert.ThrowsAsync<InputValidationException>(
            () => service.ExchangeAsync(AccountId, "test-exchange-mercenary", CancellationToken));

        Assert.Equal(["honourUnknown"], unknown.Reasons);
        Assert.Equal(["mercenaryHired"], again.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Exchange_ConflictsWhenCharacterVersionIsStale()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-medal-a", 2);
        log.Clear();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.ExchangeAsync(AccountId, "test-exchange-ore", CancellationToken));

        Assert.Equal(2, Assert.Single(await BagAsync(character)).Quantity);
        Assert.Empty(log);
    }
}
