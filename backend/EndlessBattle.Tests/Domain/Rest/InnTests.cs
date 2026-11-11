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

    private static readonly InnRoom Loft = new("apprentice-loft", "學徒閣樓", BuffKind.Exp, 1.10, 2);
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

    [Fact]
    public void Book_SpendsMoneyHealsAndStartsLodging()
    {
        var booking = Inn.Book(Hero(), EquipmentBonus.Zero, Definition, Loft, Now);

        Assert.Equal(600, booking.Cost);
        Assert.Equal(400, booking.Character.Money);
        Assert.Equal((1000, 91), (booking.Character.HealthPoints, booking.Character.ManaPoints));
        Assert.Equal(new Lodging("apprentice-loft", Now, Now.AddSeconds(28_800)), booking.Lodging);
    }

    [Fact]
    public void Book_HealsToMaxIncludingEquipment()
    {
        var equipment = new EquipmentBonus(StatBonus.Zero with { MaxHealthPoints = 100 });

        var booking = Inn.Book(Hero(), equipment, Definition, Loft, Now);

        Assert.Equal((1100, 91), (booking.Character.HealthPoints, booking.Character.ManaPoints));
    }

    [Fact]
    public void Book_ReplacesLodgingAndRestartsTimer()
    {
        var first = Inn.Book(Hero(), EquipmentBonus.Zero, Definition, Loft, Now);
        var later = Now.AddHours(3);

        var second = Inn.Book(first.Character, EquipmentBonus.Zero, Definition, Bunk, later);

        Assert.Equal(new Lodging("fighters-bunk", later, later.AddSeconds(28_800)), second.Lodging);
        Assert.Equal(100, second.Character.Money);
    }

    [Fact]
    public void ToBuff_NullWhenRoomMissing()
    {
        Assert.Null(Inn.ToBuff(new Lodging("gone-room", Now, Now.AddHours(8)), Definition));
        Assert.Null(Inn.ToBuff(null, Definition));
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
        var multiplier = Buffs.Multiplier([Elixir(BuffKind.Exp, 1.5)], BuffKind.Exp, Now, Stay(BuffKind.Exp, 1.1));

        Assert.Equal(1.65, multiplier, 10);
    }

    [Fact]
    public void Combined_LodgingOnlyInsideWindow()
    {
        var lodging = Stay(BuffKind.Exp, 1.1);

        Assert.Equal(1.1, Buffs.Multiplier([], BuffKind.Exp, lodging.StartsAt, lodging));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Exp, lodging.EndsAt, lodging));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Exp, lodging.StartsAt.AddTicks(-1), lodging));
    }

    [Fact]
    public void Combined_OtherKindUnaffected() =>
        Assert.Equal(1.5, Buffs.Multiplier([Elixir(BuffKind.Exp, 1.5)], BuffKind.Exp, Now, Stay(BuffKind.Attack, 1.05)));

    [Fact]
    public void ApplyTo_WithLodgingRoundsOnce()
    {
        var player = new CombatantState(
            HealthPoints: 60, MaxHealthPoints: 350, ManaPoints: 25, MaxManaPoints: 91, Attack: 100, MagicAttack: 40, Defense: 11, MagicDefense: 15, Agility: 12, Charisma: 10, Element: 1,
            Strength: 12, Intelligence: 12);

        var buffed = Buffs.ApplyTo(player, [Elixir(BuffKind.Attack, 1.2)], Now, Stay(BuffKind.Attack, 1.05));

        Assert.Equal(DerivedStats.RoundLikePhp(126.0), buffed.Attack);
    }
}
