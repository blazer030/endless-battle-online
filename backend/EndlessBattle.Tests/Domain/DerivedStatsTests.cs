using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

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
    // 裸點 (12,12,12,10,12,10,10) + 加成 str 2、smart 1、life 4、vit 3、agi 5、au 6、be 2 → 有效 (14,13,16,13,17,16,12)；
    // at 28+14、mat 7+13、df 9+13、mdf 11+12、hpMax 16×25+50+50、spMax (13+12)×3+25
    private static readonly EquipmentBonus SampleBonus = new(new StatBonus(
        Str: 2, Smart: 1, Life: 4, Vit: 3, Agi: 5, Au: 6, Be: 2, At: 28, Mat: 7, Df: 9, Mdf: 11, HpMax: 50));

    private static readonly BaseStats SampleBase = new(Str: 12, Smart: 12, Life: 12, Vit: 10, Agi: 12, Au: 10, Be: 10);

    [Fact]
    public void Derived_AddsEquipmentBonus()
    {
        var derived = DerivedStats.From(SampleBase, SampleBonus);

        Assert.Equal(new DerivedStats(At: 42, Mat: 20, Df: 22, Mdf: 23, HpMax: 500, SpMax: 100), derived);
    }

    [Fact]
    public void Effective_AddsSevenStatsOfTheBonus()
    {
        Assert.Equal(new BaseStats(Str: 14, Smart: 13, Life: 16, Vit: 13, Agi: 17, Au: 16, Be: 12), SampleBonus.Effective(SampleBase));
    }

    [Fact]
    public void Derived_ZeroBonusMatchesBareFormula()
    {
        Assert.Equal(DerivedStats.From(SampleBase), DerivedStats.From(SampleBase, EquipmentBonus.Zero));
    }
}
