namespace Inkbound.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
