using Inkbound.Domain.Randomness;

namespace Inkbound.Application.Common;

public interface IRandomSourceFactory
{
    IRandomSource Create();
}
