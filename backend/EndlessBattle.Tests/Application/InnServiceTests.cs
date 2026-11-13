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
        service = new InnService(characters, lodgings, Content, unitOfWork, clock);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, Now);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    // 直接放一筆住宿（不截斷、不清理）
    private Task SeedStayAsync(long characterId, Lodging stay) =>
        lodgings.SaveBookingAsync(characterId, stay, ended: null, DateTimeOffset.MinValue, CancellationToken);

    private async Task<Character> ReloadAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    private static async Task<IReadOnlyList<string>> ReasonsAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<InputValidationException>(action)).Reasons;

    // 規格 inn：Booking deducts money and starts the stay——扣 1 × 50 × 2 = 100，HP、SP 不變，從現在起住 8 小時
    [Fact]
    public async Task Book_KeepsHealthAndMana()
    {
        var character = await AddCharacterAsync(character => character with { HealthPoints = 1, ManaPoints = 0 });

        var result = await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);

        var expected = new Lodging("apprentice-loft", Now, Now.AddHours(8));
        Assert.Equal(100, result.Cost);
        Assert.Equal(expected, result.Lodging);
        Assert.Equal((1900L, 1, 0), (result.Character.Money, result.Character.HealthPoints, result.Character.ManaPoints));
        Assert.Equal(result.Character, await ReloadAsync());
        Assert.Equal(expected, await lodgings.FindActiveAsync(character.Id, Now, CancellationToken));
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
        await AddCharacterAsync(character => character with { NextLevelExperience = 1_000_000_000 });
        clock.Advance(TimeSpan.FromSeconds(60));

        await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(30));
        await adventure.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal((22 * 116) + (11 * 128), (await ReloadAsync()).Experience);
    }

    // 規格 inn：Booking does not settle adventure time——上次結算 1 小時前，入住後仍是那時，也不打任何一場
    [Fact]
    public async Task Book_DoesNotSettleAdventureTime()
    {
        var character = await AddCharacterAsync(character => character with { LastSettledAt = Now.AddHours(-1) });

        await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);

        var reloaded = await ReloadAsync();
        Assert.Equal((Now.AddHours(-1), character.Experience), (reloaded.LastSettledAt, reloaded.Experience));
        Assert.Equal(["update", "lodging"], log);
    }

    // 價格用存檔的等級：經驗 900／1000、上次結算後 2 秒，不先結算升級，仍收 1 × 50 × 2 = 100
    [Fact]
    public async Task Book_ChargesTheStoredLevel()
    {
        await AddCharacterAsync(character => character with { Experience = 900 });
        clock.Advance(TimeSpan.FromSeconds(2));

        var result = await service.BookAsync(AccountId, "apprentice-loft", CancellationToken);

        Assert.Equal((1, 100L, 1900L), (result.Character.Level, result.Cost, result.Character.Money));
    }

    // 規格 inn：Switching rooms——舊房間截到入住時刻、新房間從現在起 8 小時，兩筆都在
    [Fact]
    public async Task Book_EndsThePreviousRoom()
    {
        var character = await AddCharacterAsync();
        await SeedStayAsync(character.Id, new Lodging("apprentice-loft", Now.AddHours(-1), Now.AddHours(7)));

        await service.BookAsync(AccountId, "training-chamber", CancellationToken);

        Assert.Equal(
            [new Lodging("apprentice-loft", Now.AddHours(-1), Now), new Lodging("training-chamber", Now, Now.AddHours(8))],
            await lodgings.ListEndingAfterAsync(character.Id, Now.AddDays(-1), CancellationToken));
    }

    // Lv 3：學徒閣樓與修練廂房 3 × 50 × 2 = 300，其餘 150
    [Fact]
    public async Task Get_ReturnsPricesForLevel()
    {
        var character = await AddCharacterAsync(character => character with { Level = 3 });
        var lodging = new Lodging("bulwark-room", Now.AddHours(-1), Now.AddHours(7));
        await SeedStayAsync(character.Id, lodging);

        var view = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(Content.Inn, view.Inn);
        Assert.Equal([300L, 300, 150, 150, 150, 150], view.Inn.Rooms.Select(room => Inn.Price(view.Inn, room, view.Level)));
        Assert.Equal(lodging, view.Lodging);
    }

    [Fact]
    public async Task Get_IgnoresExpiredLodging()
    {
        var character = await AddCharacterAsync();
        await SeedStayAsync(character.Id, new Lodging("bulwark-room", Now.AddHours(-8), Now));

        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).Lodging);
    }

    [Fact]
    public async Task Get_IgnoresLodgingForRemovedRoom()
    {
        var character = await AddCharacterAsync();
        await SeedStayAsync(character.Id, new Lodging("removed-room", Now.AddHours(-1), Now.AddHours(7)));

        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).Lodging);
    }

    private sealed class ZeroRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }
}
