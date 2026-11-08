using EndlessBattle.Application.World;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class AreaPassRepository(EndlessBattleDbContext context) : IAreaPassRepository
{
    public async Task<AreaPass?> FindAsync(long characterId, int placeId, CancellationToken cancellationToken)
    {
        var row = await context.CharacterAreaPasses.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId && candidate.PlaceId == placeId, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<AreaPass>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterAreaPasses.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.PlaceId)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task SaveAsync(long characterId, AreaPass pass, CancellationToken cancellationToken)
    {
        var row = await context.CharacterAreaPasses
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId && candidate.PlaceId == pass.PlaceId, cancellationToken);
        if (row is null)
        {
            row = new CharacterAreaPass { CharacterId = characterId, PlaceId = pass.PlaceId };
            context.CharacterAreaPasses.Add(row);
        }

        row.ExpiresAt = pass.ExpiresAt.UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static AreaPass ToDomain(CharacterAreaPass row) =>
        new(row.PlaceId, new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAt, DateTimeKind.Utc)));
}
