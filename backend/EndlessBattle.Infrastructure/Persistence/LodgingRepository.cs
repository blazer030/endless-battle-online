using EndlessBattle.Application.Rest;
using EndlessBattle.Domain.Rest;
using Microsoft.EntityFrameworkCore;
using CharacterLodgingRow = EndlessBattle.Infrastructure.Persistence.Models.CharacterLodging;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class LodgingRepository(EndlessBattleDbContext context) : ILodgingRepository
{
    public async Task<Lodging?> FindAsync(long characterId, CancellationToken cancellationToken)
    {
        var row = await context.CharacterLodgings.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        return row is null
            ? null
            : new Lodging(
                row.RoomId,
                new DateTimeOffset(DateTime.SpecifyKind(row.StartsAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(row.EndsAt, DateTimeKind.Utc)));
    }

    public async Task SaveAsync(long characterId, Lodging lodging, CancellationToken cancellationToken)
    {
        var row = await context.CharacterLodgings
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        if (row is null)
        {
            row = new CharacterLodgingRow { CharacterId = characterId };
            context.CharacterLodgings.Add(row);
        }

        row.RoomId = lodging.RoomId;
        row.StartsAt = lodging.StartsAt.UtcDateTime;
        row.EndsAt = lodging.EndsAt.UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }
}
