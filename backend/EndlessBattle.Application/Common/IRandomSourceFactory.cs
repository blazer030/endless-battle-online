using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Application.Common;

public interface IRandomSourceFactory
{
    IRandomSource Create();
}
