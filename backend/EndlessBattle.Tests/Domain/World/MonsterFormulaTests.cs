using EndlessBattle.Domain.World;

namespace EndlessBattle.Tests.Domain.World;

public class MonsterFormulaTests
{
    // L=1：hp 45+14+0.4=59.4、mat 6+2.6=8.6、mdf 2.9、agi 7.3、au 5.1、jobExp 9.6
    // L=10：hp 45+140+40=225、at 42、mat 32、df 12、mdf 11、agi 19、au 15、jobExp 24
    [Theory]
    [InlineData(1, 59, 12, 13, 9, 3, 3, 7, 5, 10)]
    [InlineData(10, 225, 30, 42, 32, 12, 11, 19, 15, 24)]
    public void Formula_MatchesTable(
        int level, int hp, int sp, int at, int mat, int df, int mdf, int agi, int au, long jobExp)
    {
        var baseline = MonsterFormula.Baseline(level);

        Assert.Equal(
            (level, hp, sp, at, mat, df, mdf, agi, au, jobExp),
            (baseline.Level, baseline.Hp, baseline.Sp, baseline.At, baseline.Mat, baseline.Df, baseline.Mdf, baseline.Agi, baseline.Au, baseline.JobExp));
        Assert.Equal($"baseline-{level}", baseline.Id);
        Assert.Equal((0, 0, 0), (baseline.Element, baseline.Meet, baseline.PlaceId));
        Assert.Empty(baseline.SubPlaces);
    }

    // 0.5 的進位方向：L=5 時 mdf = 2+4.5 = 6.5 → AwayFromZero 為 7（銀行家捨入會得 6）
    [Fact]
    public void Formula_RoundsHalfAwayFromZero() =>
        Assert.Equal(7, MonsterFormula.Baseline(5).Mdf);
}
