using EndlessBattle.Domain.Economy;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Economy;

public class ShopTests
{
    private static readonly ItemDefinition Sword = TestItems.Equipment("sword") with { Price = 201, ShopSellable = true };
    private static readonly ItemDefinition Supply = TestItems.Consumable("supply") with { Price = 20, ShopSellable = true };

    [Fact]
    public void BuyCost_IsPriceTimesQuantity() => Assert.Equal(60, Shop.BuyCost(Supply, 3));

    // 201 的一半是 100.5：無條件捨去
    [Theory]
    [InlineData(1, 100)]
    [InlineData(3, 301)]
    public void SellPrice_FloorsHalfPrice(int quantity, long expected) =>
        Assert.Equal(expected, Shop.SellPrice(Supply with { Price = 201 }, quantity));

    [Fact]
    public void CanBuy_RejectsInOrder()
    {
        var poor = TestCharacters.Default(1, "x", 1, 1, DateTimeOffset.UnixEpoch) with { Money = 10 };

        Assert.Equal(["notForSale"], Shop.CanBuy(poor, Supply with { ShopSellable = false }, 1, []));
        Assert.Equal(["invalidQuantity"], Shop.CanBuy(poor, Sword, 2, []));
        Assert.Equal(["invalidQuantity"], Shop.CanBuy(poor, Supply, 100, []));
        Assert.Equal(["invalidQuantity"], Shop.CanBuy(poor, Supply, 0, []));
        Assert.Equal(["notEnoughMoney"], Shop.CanBuy(poor, Supply, 1, []));
    }

    [Fact]
    public void CanBuy_RejectsWhenBagWouldDiscard()
    {
        var rich = TestCharacters.Default(1, "x", 1, 1, DateTimeOffset.UnixEpoch) with { Money = 100000 };
        var full = Enumerable.Range(1, 15).Select(index => TestItems.Entry(index, $"other-{index}")).ToList();

        Assert.Equal(["bagFull"], Shop.CanBuy(rich, Sword, 1, full));
        Assert.Empty(Shop.CanBuy(rich, Sword, 1, full.Take(14).ToList()));
    }

    [Fact]
    public void CanSell_ChecksQuantityAndAllowsBound()
    {
        var bound = Sword with { Bound = true };

        Assert.Empty(Shop.CanSell(TestItems.Entry(1, "sword"), bound, 1));
        Assert.Equal(["invalidQuantity"], Shop.CanSell(TestItems.Entry(1, "sword"), Sword, 2));
        Assert.Empty(Shop.CanSell(TestItems.Entry(1, "supply", 10), Supply, 10));
        Assert.Equal(["invalidQuantity"], Shop.CanSell(TestItems.Entry(1, "supply", 10), Supply, 11));
        Assert.Equal(["invalidQuantity"], Shop.CanSell(TestItems.Entry(1, "supply", 10), Supply, 0));
    }
}
