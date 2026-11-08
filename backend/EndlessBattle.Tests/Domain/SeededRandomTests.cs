using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Tests.Domain;

public class SeededRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var first = new SeededRandom(42);
        var second = new SeededRandom(42);
        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextDouble()).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextDouble()).ToArray();
        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var first = new SeededRandom(1);
        var second = new SeededRandom(2);
        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextDouble()).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextDouble()).ToArray();
        Assert.NotEqual(firstValues, secondValues);
    }

    [Fact]
    public void NextInt_StaysWithinRange_AndCoversAllValues()
    {
        var random = new SeededRandom(7);
        var values = Enumerable.Range(0, 1000).Select(_ => random.NextInt(3)).ToArray();
        Assert.All(values, value => Assert.InRange(value, 0, 2));
        Assert.Equal(new[] { 0, 1, 2 }, values.Distinct().Order().ToArray());
    }
}
