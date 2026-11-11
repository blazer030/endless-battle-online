using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Rest;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent 的 inn.json：8 小時、每級 50；學徒閣樓（經驗 ×1.10）與修練廂房價格倍率 2，其餘 1。
// 預設角色 Lv 1、金錢 2000、HpMax 350。擲骰一律 0.0：測試雪精每場 2.8 秒、勝場經驗 116，學徒閣樓 → 128（見 AdventureServiceTests 上方）
public class InnServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 3, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemoryJobProficiencyRepository proficiencies = new();
    private readonly InMemoryLodgingRepository lodgings;
    private readonly FakeClock clock = new(Now);
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly AdventureService adventure;
    private readonly InnService service;

    public InnServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        lodgings = new InMemoryLodgingRepository(log);
        var pets = new InMemoryPetRepository();
        var missions = new InMemoryMissionRepository();
        var mercenaries = new InMemoryMercenaryRepository();
        var buffs = new InMemoryBuffRepository();
        unitOfWork = new InMemoryUnitOfWork(characters, proficiencies, inventory, equipment, missions, pets, mercenaries, buffs, lodgings);
        adventure = new AdventureService(
            characters, proficiencies, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(), inventory, equipment,
            missions, pets, mercenaries, new InMemoryAreaPassRepository(), buffs, lodgings, Content, clock, new ZeroRandomFactory(), unitOfWork);
        service = new InnService(characters, equipment, lodgings, Content, adventure, unitOfWork, clock);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, Now);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    private async Task<Character> ReloadAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    private static async Task<IReadOnlyList<string>> ReasonsAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<InputValidationException>(action)).Reasons;

    // 上次結算就是現在，不必推進：扣 1 × 50 × 2 = 100，HP、SP 回滿，從現在起住 8 小時
    [Fact]
    public async Task Book_SpendsHealsAndSavesLodging()
    {
        var character = await AddCharacterAsync(character => character with { Hp = 1, Sp = 0 });
        var stats = DerivedStats.From(character.Base, EquipmentBonus.Zero);

        var result = await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);

        var expected = new Lodging("apprentice-loft", Now, Now.AddHours(8));
        Assert.Equal(100, result.Cost);
        Assert.Equal(expected, result.Lodging);
        Assert.Equal((1900L, stats.MaxHealthPoints, stats.MaxManaPoints), (result.Character.Money, result.Character.Hp, result.Character.Sp));
        Assert.Equal(result.Character, await ReloadAsync());
        Assert.Equal(expected, await lodgings.FindAsync(character.Id, CancellationToken));
        Assert.Equal(["update", "lodging"], log);
    }

    [Fact]
    public async Task Book_UnknownRoomWritesNothing()
    {
        await AddCharacterAsync(character => character with { LastSettledAt = Now.AddSeconds(-60) });

        Assert.Equal(["unknownRoom"], await ReasonsAsync(() => service.BookAsync(AccountId, "royal-suite", CancellationToken)));
        Assert.Empty(log);
    }

    // 錢不夠時連推進都不做
    [Fact]
    public async Task Book_NotEnoughMoneyWritesNothing()
    {
        await AddCharacterAsync(character => character with { Money = 99, LastSettledAt = Now.AddSeconds(-60) });

        Assert.Equal(["notEnoughMoney"], await ReasonsAsync(() => service.BookAsync(AccountId, "apprentice-loft", CancellationToken)));
        Assert.Empty(log);
    }

    // 上次結算後 60 秒才入住、90 秒推進：0～58.8 開打的 22 場在入住之前，經驗不變；61.6～89.6 的 11 場才加成
    [Fact]
    public async Task Book_DoesNotBoostFightsBeforeBooking()
    {
        await AddCharacterAsync(character => character with { NextExp = 1_000_000_000 });
        clock.Advance(TimeSpan.FromSeconds(60));

        await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(30));
        await adventure.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal((22 * 116) + (11 * 128), (await ReloadAsync()).Exp);
    }

    // Lv 101、陣亡、錢剛好 101 × 50 × 2 = 10100：結算時先自動住宿花掉 round(10 × 101 × 2.1) = 2121，剩 7979 不夠 → 整筆回滾
    [Fact]
    public async Task Book_RechecksMoneyAfterSettling()
    {
        var character = await AddCharacterAsync(character => character with { Level = 101, Hp = 0, Money = 10100, NextExp = 1_000_000_000 });
        var previous = new Lodging("fighters-bunk", Now.AddHours(-1), Now.AddHours(7));
        await lodgings.SaveAsync(character.Id, previous, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["notEnoughMoney"], await ReasonsAsync(() => service.BookAsync(AccountId, "apprentice-loft", CancellationToken)));
        Assert.Equal(character, await ReloadAsync());
        Assert.Equal(previous, await lodgings.FindAsync(character.Id, CancellationToken));
        Assert.Equal("rollback", unitOfWork.Log[^1]);
    }

    // 經驗 900／1000、上次結算後 2 秒：結算打 1 場拿 116 經驗升到 Lv 2，價格從 1 × 50 × 2 = 100 漲成 2 × 50 × 2 = 200
    [Fact]
    public async Task Book_ChargesThePriceForTheLevelReachedWhileSettling()
    {
        await AddCharacterAsync(character => character with { Exp = 900 });
        clock.Advance(TimeSpan.FromSeconds(2));

        var result = await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);

        Assert.Equal(2, result.Character.Level);
        Assert.Equal(200, result.Cost);
        Assert.Equal(1800, result.Character.Money);
    }

    [Fact]
    public async Task Book_ReplacesPreviousRoom()
    {
        var character = await AddCharacterAsync();
        await lodgings.SaveAsync(character.Id, new Lodging("apprentice-loft", Now.AddHours(-1), Now.AddHours(7)), CancellationToken);

        await service.BookAsync(AccountId, "training-chamber", CancellationToken);

        Assert.Equal(new Lodging("training-chamber", Now, Now.AddHours(8)), await lodgings.FindAsync(character.Id, CancellationToken));
    }

    // Lv 3：學徒閣樓與修練廂房 3 × 50 × 2 = 300，其餘 150
    [Fact]
    public async Task Get_ReturnsPricesForLevel()
    {
        var character = await AddCharacterAsync(character => character with { Level = 3 });
        var lodging = new Lodging("bulwark-room", Now.AddHours(-1), Now.AddHours(7));
        await lodgings.SaveAsync(character.Id, lodging, CancellationToken);

        var view = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(Content.Inn, view.Inn);
        Assert.Equal([300L, 300, 150, 150, 150, 150], view.Inn.Rooms.Select(room => Inn.Price(view.Inn, room, view.Level)));
        Assert.Equal(lodging, view.Lodging);
    }

    [Fact]
    public async Task Get_IgnoresExpiredLodging()
    {
        var character = await AddCharacterAsync();
        await lodgings.SaveAsync(character.Id, new Lodging("bulwark-room", Now.AddHours(-8), Now), CancellationToken);

        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).Lodging);
    }

    [Fact]
    public async Task Get_IgnoresLodgingForRemovedRoom()
    {
        var character = await AddCharacterAsync();
        await lodgings.SaveAsync(character.Id, new Lodging("removed-room", Now.AddHours(-1), Now.AddHours(7)), CancellationToken);

        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).Lodging);
    }

    private sealed class ZeroRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }
}
