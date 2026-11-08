using EndlessBattle.Application.Common;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class EfDatabaseStatus(EndlessBattleDbContext database) : IDatabaseStatus
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        database.Database.CanConnectAsync(cancellationToken);
}
