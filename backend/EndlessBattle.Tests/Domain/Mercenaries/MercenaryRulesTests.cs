using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Mercenaries;

public class MercenaryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly MercenaryDefinition Hired = new("hired", "傭兵", Price: 100, Fights: 3, 50, 100, 50, DeathCost: 2);

    [Theory]
    [InlineData(30, 1.7)]
    [InlineData(100, 1.0)]
    [InlineData(1, 1.0)]
    public void Rules_DropFactorFollowsPhp(int dropPercent, double factor)
    {
        Assert.Equal(factor, MercenaryRules.DropDenominatorFactor(dropPercent), 9);
    }

    [Fact]
    public void Rules_DiscountRoundsLikePhp()
    {
        Assert.Equal(78, MercenaryRules.Discount(155, 50));
        Assert.Equal(0, MercenaryRules.Discount(0, 50));
        Assert.Equal(155, MercenaryRules.Discount(155, 100));
    }

    // 勳章所的傭兵沒有金錢價格，也算付不起
    [Fact]
    public void CanHire_ReasonsInOrder()
    {
        var broke = TestCharacters.Default(1, "勇者", 1, 1, Now) with { Money = 0 };
        var honourOnly = Hired with { Price = null };
        var current = new MercenaryContract("hired", "傭兵", 3, PlaceId: null, NextFightAt: null, Now);

        Assert.Equal(["honourOnly", "mercenaryHired", "notEnoughMoney"], MercenaryRules.CanHire(broke, honourOnly, current));
        Assert.Equal(["mercenaryUnknown"], MercenaryRules.CanHire(broke, definition: null, current));
        Assert.Equal(["notEnoughMoney"], MercenaryRules.CanHire(broke with { Money = 99 }, Hired, current: null));
        Assert.Empty(MercenaryRules.CanHire(broke with { Money = 100 }, Hired, current: null));
    }

    [Fact]
    public void CanDeploy_RejectsRestricted()
    {
        var plain = new PlaceDefinition(1, "平原", "central", [], new LevelRange(1, 10), Restricted: false);

        Assert.Equal(["placeRestricted"], MercenaryRules.CanDeploy(plain with { Restricted = true }));
        Assert.Equal(["placeUnknown"], MercenaryRules.CanDeploy(null));
        Assert.Empty(MercenaryRules.CanDeploy(plain));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("一二三四五六七八九", false)]
    [InlineData("一二三四五六七八", true)]
    public void Rename_LimitsToEightCharacters(string? name, bool valid)
    {
        Assert.Equal(valid ? [] : ["nameInvalid"], MercenaryRules.CanRename(name));
    }
}
