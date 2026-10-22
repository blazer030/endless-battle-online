using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Tests.Domain.Battle;

// 嘗試條件：門檻 > 0 且 hp × 100 < 門檻 × hpMax；剛好等於不逃
public class EscapeRulesTests
{
    [Theory]
    [InlineData(29, 100, 30, true)]
    [InlineData(30, 100, 30, false)]
    [InlineData(100, 100, 30, false)]
    [InlineData(105, 350, 30, false)]
    [InlineData(104, 350, 30, true)]
    [InlineData(99, 100, 100, true)]
    [InlineData(1, 100, 0, false)]
    [InlineData(0, 100, 0, false)]
    public void ShouldTry_OnlyBelowThreshold(int hp, int hpMax, int escapeHpPercent, bool expected)
    {
        Assert.Equal(expected, EscapeRules.ShouldTry(hp, hpMax, escapeHpPercent));
    }
}
