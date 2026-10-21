namespace EndlessBattle.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
