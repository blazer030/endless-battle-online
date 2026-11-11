using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Tests.Domain.Progression;

public class ExperienceCurveTests
{
    // 6·lv² + 1080·lv：Lv2 24+2160、Lv10 600+10800、Lv50 15000+54000、Lv100 60000+108000
    [Theory]
    [InlineData(2, 2184)]
    [InlineData(10, 11400)]
    [InlineData(50, 69000)]
    [InlineData(100, 168000)]
    public void NextExp_MatchesCurve(int level, long expected)
    {
        Assert.Equal(expected, ExperienceCurve.NextLevelExperienceFor(level));
    }
}
