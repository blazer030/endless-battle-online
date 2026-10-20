namespace Inkbound.Domain.Randomness;

public sealed class SeededRandom(int seed) : IRandomSource
{
    private readonly Random random = new(seed);

    public double NextDouble() => random.NextDouble();

    public int NextInt(int maxExclusive) => random.Next(maxExclusive);
}
