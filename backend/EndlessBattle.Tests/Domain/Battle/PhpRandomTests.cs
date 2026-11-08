using EndlessBattle.Domain.Battle;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

public class PhpRandomTests
{
    // rand(5, 3) 對調成 rand(3, 5)：NextInt(3) + 3；0.0 → 0 → 3，0.99 → ⌊2.97⌋ = 2 → 5（兩端都含）
    [Theory]
    [InlineData(0.0, 3)]
    [InlineData(0.99, 5)]
    public void Between_SwapsReversedBounds_IncludingBothEnds(double roll, int expected)
    {
        Assert.Equal(expected, PhpRandom.Between(new ScriptedRandom(roll), 5, 3));
    }

    // PHP 把浮點參數截成整數：rand(1.2, 3.9) 等於 rand(1, 3)，NextInt(3) + 1；0.0 → 1，0.99 → ⌊2.97⌋ = 2 → 3
    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(0.99, 3)]
    public void Between_TruncatesFractionalBounds(double roll, int expected)
    {
        Assert.Equal(expected, PhpRandom.Between(new ScriptedRandom(roll), 1.2, 3.9));
    }
}
