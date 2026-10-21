using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Tests.Support;

public sealed class FixedSeedRandomFactory(int seed) : IRandomSourceFactory
{
    public IRandomSource Create() => new SeededRandom(seed);
}
