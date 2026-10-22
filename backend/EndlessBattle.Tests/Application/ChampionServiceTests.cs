using EndlessBattle.Application.Champion;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent 的種子冠軍：測試冠軍 Lv5、HP 120、SP 20、at 20、mat 5、df 8、mdf 5、agi 10、au 6
public class ChampionServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryChampionRepository champions;
    private readonly InMemoryInventoryRepository inventory;
    private readonly InMemoryEquipmentRepository equipment;
    private readonly ChampionService service;

    public ChampionServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        champions = new InMemoryChampionRepository(log);
        inventory = new InMemoryInventoryRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        var unitOfWork = new InMemoryUnitOfWork(characters, champions, inventory);
        service = new ChampionService(characters, champions, equipment, new InMemorySkillLoadoutRepository(), new InMemorySkillBookRepository(), Content,
            new FakeClock(Now), new FixedSeedRandomFactory(3), unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "挑戰者", 1, 1, Now);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    private async Task<Character> StoredAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    [Fact]
    public async Task Get_ShowsSeedWhenNoSeat()
    {
        var character = await AddCharacterAsync();

        var view = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(Content.SeedChampion, view.Seat);
        Assert.Empty(view.Reasons);
        Assert.Null(await champions.FindAsync(character.Id, CancellationToken));
        Assert.Empty(log);
    }

    [Fact]
    public async Task Get_ShowsSavedSeatAndReasons()
    {
        var character = await AddCharacterAsync(character => character with { Hp = 0 });
        var locked = Content.SeedChampion with { Name = "前任的我", IsPlayer = true, LockedUntil = Now.AddDays(1) };
        await champions.SaveAsync(character.Id, locked, CancellationToken);

        var view = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(locked, view.Seat);
        Assert.Equal(["championLocked", "noHp"], view.Reasons);
    }

    // 角色版本鎖（扣費、戰後 HP／SP）最先寫，之後才存席位
    [Fact]
    public async Task Challenge_WritesCharacterVersionFirst()
    {
        await AddCharacterAsync();

        var result = await service.ChallengeAsync(AccountId, CancellationToken);

        Assert.Equal(["update", "champion:save"], log);
        var stored = await StoredAsync();
        Assert.Equal(1500, stored.Money);
        Assert.Equal(1, stored.Version);
        Assert.NotEmpty(result.Events);
        Assert.Equal(new RoundEvent(0, 1), result.Events[0]);
        Assert.Equal(result.View.Seat, await champions.FindAsync(stored.Id, CancellationToken));
    }

    [Fact]
    public async Task Challenge_RejectsWithReasonsAndWritesNothing()
    {
        var character = await AddCharacterAsync(character => character with { Money = 400 });

        var error = await Assert.ThrowsAsync<InputValidationException>(() => service.ChallengeAsync(AccountId, CancellationToken));

        Assert.Equal(["notEnoughMoney"], error.Reasons);
        Assert.Empty(log);
        Assert.Equal(400, (await StoredAsync()).Money);
        Assert.Null(await champions.FindAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Challenge_ConflictPersistsNothing()
    {
        var character = await AddCharacterAsync();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.ChallengeAsync(AccountId, CancellationToken));

        Assert.Empty(log);
        Assert.Equal(character, await StoredAsync());
        Assert.Null(await champions.FindAsync(character.Id, CancellationToken));
    }

    // 第一次輸掉後冠軍留著傷；回滿 HP 再挑戰，開戰時冠軍是上次剩下的 HP
    [Fact]
    public async Task Challenge_SecondChallengeFacesWoundedChampion()
    {
        await AddCharacterAsync(character => character with { Hp = 30 });
        var first = await service.ChallengeAsync(AccountId, CancellationToken);
        Assert.False(first.Won);
        var wounded = first.View.Seat;
        Assert.InRange(wounded.Hp, 1, wounded.HpMax - 1);
        Assert.Equal(1, wounded.Defenses);
        await characters.UpdateAsync(await StoredAsync() with { Hp = 30 }, CancellationToken);

        var second = await service.ChallengeAsync(AccountId, CancellationToken);

        var opening = second.Events.OfType<AttackEvent>().First(attack => attack.Attacker == CombatSide.Player);
        Assert.Equal(wounded.Hp, opening.TargetHpAfter + opening.Damage);
        Assert.Equal(2, second.View.Seat.Defenses);
        Assert.Equal(1000, (await StoredAsync()).Money);
    }

    // 勝：席位換成含裝備的快照，鎖 7 天，畫面上立刻顯示 championLocked
    [Fact]
    public async Task Challenge_WinTakesTheSeatAndLocksIt()
    {
        var character = await AddCharacterAsync(character => character with
        {
            Base = new BaseStats(Str: 300, Smart: 12, Life: 100, Vit: 200, Agi: 100, Au: 50, Be: 100),
            Hp = 2000,
        });
        var sword = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);

        var result = await service.ChallengeAsync(AccountId, CancellationToken);

        Assert.True(result.Won);
        Assert.Equal(("挑戰者", true, 0, 308, Now.AddDays(7)), (result.View.Seat.Name, result.View.Seat.IsPlayer, result.View.Seat.Defenses,
            result.View.Seat.At, result.View.Seat.LockedUntil));
        Assert.Equal(["championLocked"], result.View.Reasons);
        Assert.Equal(1500, (await StoredAsync()).Money);
    }

    // 補給欄有補給品、HP 低也不喝，補給數量不變
    [Fact]
    public async Task Challenge_LeavesSupplyUntouched()
    {
        var character = await AddCharacterAsync(character => character with { Hp = 5 });
        var supply = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-supply", 3), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Supply, supply, 3, CancellationToken);
        log.Clear();

        var result = await service.ChallengeAsync(AccountId, CancellationToken);

        Assert.Empty(result.Events.OfType<PotionEvent>());
        Assert.Equal(["update", "champion:save"], log);
        Assert.Equal(3, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
    }
}
