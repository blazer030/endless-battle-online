using Inkbound.Application.Common;
using Inkbound.Domain.Randomness;

namespace Inkbound.Tests.Support;

public sealed class FixedSeedRandomFactory(int seed) : IRandomSourceFactory
{
    public IRandomSource Create() => new SeededRandom(seed);
}
