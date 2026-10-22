using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Tests.Support;

// 永遠取上限，讓 PhpRandom.Between 回傳最大值；用來估算最大單擊
public sealed class HighestRandom : IRandomSource
{
    public double NextDouble() => 0.999999;

    public int NextInt(int maxExclusive) => maxExclusive - 1;
}
