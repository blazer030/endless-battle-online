using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Tests.Domain;

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

    // 七項各不相同，換錯欄位就會被抓到
    [Fact]
    public void Derived_UsesEachStatDistinctly()
    {
        var derived = DerivedStats.From(new BaseStats(Str: 1, Smart: 2, Life: 3, Vit: 4, Agi: 5, Au: 6, Be: 7));

        Assert.Equal(new DerivedStats(At: 1, Mat: 2, Df: 4, Mdf: 7, HpMax: 125, SpMax: 52), derived);
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
