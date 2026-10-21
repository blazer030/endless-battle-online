using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Economy;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Economy;

public class BankTests
{
    private static Character Rich(int level = 15, long money = 1000, long bank = 500) =>
        TestCharacters.Default(1, "x", 1, 1, DateTimeOffset.UnixEpoch) with { Level = level, Money = money, Bank = bank };

    [Fact]
    public void CanDeposit_RequiresLevel15() =>
        Assert.Equal(["levelTooLow"], Bank.CanDeposit(Rich(level: 14), 100));

    [Fact]
    public void CanDeposit_AtLevel15WithinMoney() => Assert.Empty(Bank.CanDeposit(Rich(), 1000));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CanDeposit_RejectsNonPositive(long amount) => Assert.Equal(["invalidAmount"], Bank.CanDeposit(Rich(), amount));

    [Fact]
    public void CanDeposit_RejectsOverdraft() => Assert.Equal(["notEnoughMoney"], Bank.CanDeposit(Rich(), 1001));

    [Fact]
    public void CanWithdraw_AnyLevel()
    {
        Assert.Empty(Bank.CanWithdraw(Rich(level: 1), 500));
        Assert.Equal(["notEnoughBank"], Bank.CanWithdraw(Rich(), 501));
        Assert.Equal(["invalidAmount"], Bank.CanWithdraw(Rich(), 0));
    }
}
