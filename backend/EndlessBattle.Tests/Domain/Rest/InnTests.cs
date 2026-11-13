using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Rest;

public class InnTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly InnRoom Loft = new("apprentice-loft", "學徒閣樓", BuffKind.Experience, 1.10, 2);
    private static readonly InnRoom Bunk = new("fighters-bunk", "鬥士通鋪", BuffKind.Attack, 1.05, 1);
    private static readonly InnDefinition Definition = new([Loft, Bunk], 28_800, 50);

    // Life 38 → HpMax = round(38×25) + 50 = 1000；Smart 12、Be 10 → SpMax = round(22×3) + 25 = 91
    private static Character Hero(int level = 6, long money = 1000) =>
        TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with
        {
            Level = level,
            HealthPoints = 30,
            ManaPoints = 3,
            Money = money,
            Base = new BaseStats(Strength: 12, Intelligence: 12, Life: 38, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10),
        };

    private static CharacterBuff Elixir(BuffKind kind, double multiplier) =>
        new(kind, multiplier, Now, Now.AddHours(1));

    private static CharacterBuff Stay(BuffKind kind, double multiplier) =>
        new(kind, multiplier, Now, Now.AddHours(8));

    [Fact]
    public void Price_IsLevelTimesPerLevelTimesRate()
    {
        Assert.Equal(600, Inn.Price(Definition, Loft, 6));
        Assert.Equal(300, Inn.Price(Definition, Bunk, 6));
    }

    // 規格 inn：Room price 的 Lv 120 列
    [Fact]
    public void Price_ForLevel120()
    {
        Assert.Equal(12_000, Inn.Price(Definition, Loft, 120));
    }

    [Fact]
    public void Price_DoesNotOverflowInt()
    {
        Assert.Equal(3_000_000_000L, Inn.Price(new InnDefinition([Loft], 60, 50_000_000), Loft, 30));
    }

    [Fact]
    public void CanBook_RejectsWhenMoneyShort()
    {
        Assert.Equal(["notEnoughMoney"], Inn.CanBook(Hero(money: 599), 600));
        Assert.Empty(Inn.CanBook(Hero(money: 600), 600));
    }

    private static DateTimeOffset At(int hour) => new(2026, 10, 29, hour, 0, 0, TimeSpan.Zero);

    // 規格 inn：Booking deducts money and starts the stay（Lv 6、1,000 金、HP 20 → 400 金、HP 20、08:00–16:00）
    [Fact]
    public void Book_SpendsMoneyOnly()
    {
        var hero = Hero() with { HealthPoints = 20, ManaPoints = 3 };

        var booking = Inn.Book(hero, Definition, Loft, active: null, At(8));

        Assert.Equal(600, booking.Cost);
        Assert.Equal(hero with { Money = 400 }, booking.Character);
        Assert.Equal(new Lodging("apprentice-loft", At(8), At(16)), booking.Booked);
        Assert.Null(booking.Ended);
    }

    // 規格 inn：Switching rooms（鬥士通鋪 06:00–14:00，10:00 訂學徒閣樓）
    [Fact]
    public void Book_EndsTheActiveStayNow()
    {
        var booking = Inn.Book(Hero(), Definition, Loft, new Lodging("fighters-bunk", At(6), At(14)), At(10));

        Assert.Equal(new Lodging("fighters-bunk", At(6), At(10)), booking.Ended);
        Assert.Equal(new Lodging("apprentice-loft", At(10), At(18)), booking.Booked);
    }

    // 規格 inn：Booking the same room again restarts it
    [Fact]
    public void Book_SameRoomRestarts()
    {
        var booking = Inn.Book(Hero(), Definition, Bunk, new Lodging("fighters-bunk", At(6), At(14)), At(10));

        Assert.Equal(new Lodging("fighters-bunk", At(6), At(10)), booking.Ended);
        Assert.Equal(new Lodging("fighters-bunk", At(10), At(18)), booking.Booked);
    }

    // 已經到期的那筆不用截斷
    [Fact]
    public void Book_ExpiredStayIsNotEnded()
    {
        var booking = Inn.Book(Hero(), Definition, Loft, new Lodging("fighters-bunk", At(1), At(9)), At(10));

        Assert.Null(booking.Ended);
    }

    // 規格 inn：Pruning at booking time（離線補算上限 86,400 秒）
    [Fact]
    public void PruneBefore_IsOneDayEarlier()
    {
        Assert.Equal(At(10).AddDays(-1), Inn.PruneBefore(At(10)));
    }

    [Fact]
    public void ToBuff_NullWhenRoomMissing()
    {
        Assert.Null(Inn.ToBuff(new Lodging("gone-room", Now, Now.AddHours(8)), Definition));
        Assert.Null(Inn.ToBuff(null, Definition));
    }

    // 規格 inn：Room removed from the content——內容沒有的房型不算加成
    [Fact]
    public void ToBuffs_SkipsRemovedRooms()
    {
        IReadOnlyList<Lodging> stays = [new("gone-room", Now, Now.AddHours(1)), new("fighters-bunk", Now.AddHours(1), Now.AddHours(9))];

        Assert.Equal([new CharacterBuff(BuffKind.Attack, 1.05, Now.AddHours(1), Now.AddHours(9))], Inn.ToBuffs(stays, Definition));
    }

    [Fact]
    public void ToBuff_UsesRoomKindAndMultiplier()
    {
        var lodging = new Lodging("fighters-bunk", Now, Now.AddHours(8));

        Assert.Equal(new CharacterBuff(BuffKind.Attack, 1.05, Now, Now.AddHours(8)), Inn.ToBuff(lodging, Definition));
    }

    [Fact]
    public void Combined_MultipliesElixirAndLodging()
    {
        var multiplier = Buffs.Multiplier([Elixir(BuffKind.Experience, 1.5)], BuffKind.Experience, Now, [Stay(BuffKind.Experience, 1.1)]);

        Assert.Equal(1.65, multiplier, 10);
    }

    // 規格範例：經驗密藥 ×1.05 與經驗住宿 ×1.10 → 1.155
    [Fact]
    public void Multiplier_ElixirAndLodgingOfSameKindMultiply()
    {
        var multiplier = Buffs.Multiplier([Elixir(BuffKind.Experience, 1.05)], BuffKind.Experience, Now, [Stay(BuffKind.Experience, 1.10)]);

        Assert.Equal(1.155, multiplier, 10);
    }

    [Fact]
    public void Combined_LodgingOnlyInsideWindow()
    {
        var lodging = Stay(BuffKind.Experience, 1.1);

        Assert.Equal(1.1, Buffs.Multiplier([], BuffKind.Experience, lodging.StartsAt, [lodging]));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Experience, lodging.EndsAt, [lodging]));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Experience, lodging.StartsAt.AddTicks(-1), [lodging]));
    }

    [Fact]
    public void Combined_OtherKindUnaffected() =>
        Assert.Equal(1.5, Buffs.Multiplier([Elixir(BuffKind.Experience, 1.5)], BuffKind.Experience, Now, [Stay(BuffKind.Attack, 1.05)]));

    [Fact]
    public void ApplyTo_WithLodgingRoundsOnce()
    {
        var player = new CombatantState(
            HealthPoints: 60, MaxHealthPoints: 350, ManaPoints: 25, MaxManaPoints: 91, Attack: 100, MagicAttack: 40, Defense: 11, MagicDefense: 15, Agility: 12, Charisma: 10, Element: 1,
            Strength: 12, Intelligence: 12);

        var buffed = Buffs.ApplyTo(player, [Elixir(BuffKind.Attack, 1.2)], Now, [Stay(BuffKind.Attack, 1.05)]);

        Assert.Equal(DerivedStats.RoundLikePhp(126.0), buffed.Attack);
    }
}
