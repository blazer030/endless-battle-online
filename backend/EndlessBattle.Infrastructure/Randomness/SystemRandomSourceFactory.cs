using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Infrastructure.Randomness;

public sealed class SystemRandomSourceFactory : IRandomSourceFactory
{
    public IRandomSource Create() => new SeededRandom(Random.Shared.Next());
}
