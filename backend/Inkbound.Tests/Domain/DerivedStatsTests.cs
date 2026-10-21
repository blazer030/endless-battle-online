using Inkbound.Domain.Characters;

namespace Inkbound.Tests.Domain;

public class DerivedStatsTests
{
    [Fact]
    public void Derived_MatchesWogFormulas()
    {
        var derived = DerivedStats.From(new BaseStats(Str: 17, Smart: 12, Life: 15, Vit: 10, Agi: 14, Au: 10, Be: 10));

        Assert.Equal(17, derived.At);
        Assert.Equal(12, derived.Mat);
        Assert.Equal(10, derived.Df);
        Assert.Equal(10, derived.Mdf);
        Assert.Equal(425, derived.HpMax);
        Assert.Equal(91, derived.SpMax);
    }

    // life×25 與 (smart+be)×3 在整數輸入下不會產生 .5，所以直接驗證四捨五入輔助函式與 PHP round 一致
    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -3)]
    [InlineData(2.4, 2)]
    public void Derived_RoundsHalfAwayFromZero(double value, int expected)
    {
        Assert.Equal(expected, DerivedStats.RoundLikePhp(value));
    }
}
