using Inkbound.Application.Common;

namespace Inkbound.Tests.Support;

public sealed class FakeClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
