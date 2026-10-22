namespace EndlessBattle.Application.Common;

public interface IDatabaseStatus
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
