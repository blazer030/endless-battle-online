using Inkbound.Application.Common;
using Inkbound.Domain.Randomness;

namespace Inkbound.Infrastructure.Randomness;

public sealed class SystemRandomSourceFactory : IRandomSourceFactory
{
    public IRandomSource Create() => new SeededRandom(Random.Shared.Next());
}
