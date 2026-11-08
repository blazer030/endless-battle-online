using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Tests.Support;

// 依序回傳指定的數值，用完之後一律回傳 fallback
public sealed class ScriptedRandom(double fallback, params double[] scripted) : IRandomSource
{
    private readonly Queue<double> values = new(scripted);

    public int Consumed { get; private set; }

    public double NextDouble()
    {
        Consumed++;
        return values.Count > 0 ? values.Dequeue() : fallback;
    }

    public int NextInt(int maxExclusive) => (int)(NextDouble() * maxExclusive);
}
