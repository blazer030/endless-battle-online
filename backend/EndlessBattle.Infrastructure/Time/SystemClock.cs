using EndlessBattle.Application.Common;

namespace EndlessBattle.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
