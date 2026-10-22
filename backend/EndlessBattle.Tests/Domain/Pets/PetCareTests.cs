using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Pets;

// ScriptedRandom 的 rand(a, b) = ⌊擲骰 × (b − a + 1)⌋ + a：飽腹 rand(10,20) 擲 0.5 → 15、親密度 rand(2,6) 擲 0.5 → 4、
// 飢餓 rand(8,12) 擲 0.0 → 8
public class PetCareTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DerivedStats Owner = new(At: 180, Mat: 180, Df: 50, Mdf: 80, HpMax: 500, SpMax: 100);

    private static PetFood Food(string id) => PetCare.Foods.Single(food => food.Id == id);

    private static Pet Fed(int attack = 10, int magic = 10, int defense = 10, int spirit = 1, int satiety = 50) =>
        TestPets.Create(attack: attack, magic: magic, defense: defense, spirit: spirit) with
        {
            Satiety = satiety,
            HungerSince = Now.AddSeconds(-60),
        };

    [Fact]
    public void Foods_AreTheWogMenu()
    {
        Assert.Equal(
            [
                new PetFood("fruit-vigor-small", "初級活力果實", PetStat.Attack, 2, 20, 10, 20),
                new PetFood("fruit-aroma-small", "初級香氣果實", PetStat.Magic, 2, 20, 10, 20),
                new PetFood("fruit-sweet-small", "初級甜味果實", PetStat.Defense, 2, 20, 10, 20),
                new PetFood("fruit-wild-small", "初級野性果實", PetStat.Spirit, 2, 20, 10, 20),
                new PetFood("fruit-vigor-medium", "中級活力果實", PetStat.Attack, 4, 50, 18, 28),
                new PetFood("fruit-aroma-medium", "中級香氣果實", PetStat.Magic, 4, 50, 18, 28),
                new PetFood("fruit-sweet-medium", "中級甜味果實", PetStat.Defense, 4, 50, 18, 28),
                new PetFood("fruit-wild-medium", "中級野性果實", PetStat.Spirit, 4, 50, 18, 28),
            ],
            PetCare.Foods);
    }

    [Fact]
    public void Feed_RaisesStatSatietyAndIntimacy()
    {
        var pet = Fed() with { Intimacy = 3 };
        var random = new ScriptedRandom(0.0, 0.5, 0.5);

        var fed = PetCare.Feed(pet, Food("fruit-vigor-small"), Now, random);

        Assert.Equal(pet with { Attack = 12, Satiety = 65, Intimacy = 7, HungerSince = Now }, fed);
        Assert.Equal(2, random.Consumed);
    }

    // 先擲飽腹再擲親密度：飽腹擲 0.0 → 10、親密度擲 0.99 → 6
    [Fact]
    public void Feed_RollsSatietyBeforeIntimacy()
    {
        var fed = PetCare.Feed(Fed(), Food("fruit-wild-medium"), Now, new ScriptedRandom(0.0, 0.0, 0.99));

        Assert.Equal((5, 68, 6), (fed.Spirit, fed.Satiety, fed.Intimacy));
    }

    [Theory]
    [InlineData(178, new string[0])]
    [InlineData(179, new[] { "overOwner" })]
    public void Feed_AllowsEqualToOwnerRejectsAbove(int attack, string[] reasons)
    {
        Assert.Equal(reasons, PetCare.CanFeed(Fed(attack: attack), Food("fruit-vigor-small"), Owner, 2000, Now));
    }

    [Fact]
    public void Feed_MagicCapIsOwnerMat()
    {
        Assert.Empty(PetCare.CanFeed(Fed(magic: 176), Food("fruit-aroma-medium"), Owner, 2000, Now));
        Assert.Equal(["overOwner"], PetCare.CanFeed(Fed(magic: 177), Food("fruit-aroma-medium"), Owner, 2000, Now));
    }

    [Theory]
    [InlineData(78, new string[0])]
    [InlineData(79, new[] { "overOwner" })]
    public void Feed_DefenseCapIsTheHigherOfDfAndMdf(int defense, string[] reasons)
    {
        Assert.Equal(reasons, PetCare.CanFeed(Fed(defense: defense), Food("fruit-sweet-small"), Owner, 2000, Now));
    }

    [Fact]
    public void Feed_SpiritHasNoCap()
    {
        Assert.Empty(PetCare.CanFeed(Fed(spirit: 999), Food("fruit-wild-small"), Owner, 2000, Now));
    }

    [Fact]
    public void Feed_CooldownIsTenSeconds()
    {
        var food = Food("fruit-vigor-small");

        Assert.Equal(["tooSoon"], PetCare.CanFeed(Fed() with { HungerSince = Now.AddMilliseconds(-9_999) }, food, Owner, 2000, Now));
        Assert.Empty(PetCare.CanFeed(Fed() with { HungerSince = Now.AddSeconds(-10) }, food, Owner, 2000, Now));
        Assert.Equal(Now.AddSeconds(10), PetCare.NextFeedAt(Fed() with { HungerSince = Now }));
    }

    [Fact]
    public void Feed_RejectsWhenSatietyAtLeastHundred()
    {
        var food = Food("fruit-vigor-small");

        Assert.Equal(["full"], PetCare.CanFeed(Fed(satiety: 100), food, Owner, 2000, Now));
        Assert.Empty(PetCare.CanFeed(Fed(satiety: 99), food, Owner, 2000, Now));
        Assert.Equal(119, PetCare.Feed(Fed(satiety: 99), food, Now, new ScriptedRandom(0.99)).Satiety);
    }

    [Fact]
    public void Feed_RejectsWithoutMoney()
    {
        var food = Food("fruit-vigor-small");

        Assert.Equal(["notEnoughMoney"], PetCare.CanFeed(Fed(), food, Owner, 19, Now));
        Assert.Empty(PetCare.CanFeed(Fed(), food, Owner, 20, Now));

        // 全部不符時只回第一個
        var everything = Fed(attack: 179, satiety: 100) with { HungerSince = Now };
        Assert.Equal(["tooSoon"], PetCare.CanFeed(everything, food, Owner, 19, Now));
        Assert.Equal(["full"], PetCare.CanFeed(everything with { HungerSince = Now.AddSeconds(-10) }, food, Owner, 19, Now));
    }

    // 只給一個值（0.5 → rand(8,12) = 10）：飢餓剛好用掉這一個，之後拿到的是 fallback
    [Fact]
    public void Hunger_ConsumesExactlyOneRoll()
    {
        var pet = Fed(satiety: 50) with { Intimacy = 5, HungerSince = Now.AddHours(-1) };
        var random = new ScriptedRandom(0.99, 0.5);

        var hungry = PetCare.Hunger(pet, Now, random);

        Assert.Equal(40, hungry.Satiety);
        Assert.Equal(1, random.Consumed);
        Assert.Equal(0.99, random.NextDouble());
    }

    [Fact]
    public void Hunger_TicksOncePerCheck()
    {
        var pet = Fed(satiety: 50) with { Intimacy = 5, HungerSince = Now.AddHours(-1) };
        var random = new ScriptedRandom(0.0);

        var hungry = PetCare.Hunger(pet, Now, random);

        Assert.Equal(pet with { Intimacy = 4, Satiety = 42, HungerSince = Now }, hungry);
        Assert.Equal(1, random.Consumed);

        var exactly = pet with { HungerSince = Now.AddSeconds(-900) };
        var untouched = new ScriptedRandom(0.0);
        Assert.Same(exactly, PetCare.Hunger(exactly, Now, untouched));

        var stabled = pet with { Carried = false };
        Assert.Same(stabled, PetCare.Hunger(stabled, Now, untouched));
        Assert.Equal(0, untouched.Consumed);
    }

    // 飽腹與親密度都不低於 0
    [Fact]
    public void Hunger_FloorsAtZero()
    {
        var pet = Fed(satiety: 5) with { Intimacy = 0, HungerSince = Now.AddHours(-1) };

        var hungry = PetCare.Hunger(pet, Now, new ScriptedRandom(0.99));

        Assert.Equal((0, 0), (hungry.Satiety, hungry.Intimacy));
    }

    [Theory]
    [InlineData(4.99, 0)]
    [InlineData(5, 1)]
    [InlineData(15, 2)]
    public void Age_IsTenDaysPerYear(double days, int age)
    {
        var pet = Fed() with { BornAt = Now.AddDays(-days) };

        Assert.Equal(age, PetCare.Age(pet, Now));
    }
}
