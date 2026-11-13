using EndlessBattle.Application.Rest;
using EndlessBattle.Domain.Rest;
using Microsoft.EntityFrameworkCore;
using CharacterLodgingRow = EndlessBattle.Infrastructure.Persistence.Models.CharacterLodging;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class LodgingRepository(EndlessBattleDbContext context) : ILodgingRepository
{
    public async Task<Lodging?> FindActiveAsync(long characterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var moment = now.UtcDateTime;
        var row = await context.CharacterLodgings.AsNoTracking()
            .Where(candidate => candidate.CharacterId == characterId && candidate.StartsAt <= moment && candidate.EndsAt > moment)
            .OrderByDescending(candidate => candidate.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToLodging(row);
    }

    public async Task<IReadOnlyList<Lodging>> ListEndingAfterAsync(long characterId, DateTimeOffset after, CancellationToken cancellationToken)
    {
        var moment = after.UtcDateTime;
        var rows = await context.CharacterLodgings.AsNoTracking()
            .Where(candidate => candidate.CharacterId == characterId && candidate.EndsAt > moment)
            .OrderBy(candidate => candidate.StartsAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToLodging).ToList();
    }

    public async Task SaveBookingAsync(long characterId, Lodging booked, Lodging? ended, DateTimeOffset pruneBefore, CancellationToken cancellationToken)
    {
        if (ended is not null)
        {
            var startsAt = ended.StartsAt.UtcDateTime;
            var row = await context.CharacterLodgings
                .SingleAsync(candidate => candidate.CharacterId == characterId && candidate.StartsAt == startsAt, cancellationToken);
            row.EndsAt = ended.EndsAt.UtcDateTime;
        }

        context.CharacterLodgings.Add(new CharacterLodgingRow
        {
            CharacterId = characterId,
            RoomId = booked.RoomId,
            StartsAt = booked.StartsAt.UtcDateTime,
            EndsAt = booked.EndsAt.UtcDateTime,
        });
        await context.SaveChangesAsync(cancellationToken);

        var cutoff = pruneBefore.UtcDateTime;
        await context.CharacterLodgings
            .Where(candidate => candidate.CharacterId == characterId && candidate.EndsAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static Lodging ToLodging(CharacterLodgingRow row) =>
        new(
            row.RoomId,
            new DateTimeOffset(DateTime.SpecifyKind(row.StartsAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(row.EndsAt, DateTimeKind.Utc)));

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
