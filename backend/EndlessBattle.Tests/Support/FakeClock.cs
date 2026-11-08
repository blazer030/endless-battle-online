using EndlessBattle.Application.Common;

namespace EndlessBattle.Tests.Support;

public sealed class FakeClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan duration) => UtcNow += duration;
}
