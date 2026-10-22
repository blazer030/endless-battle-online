using EndlessBattle.Application.Items;
using EndlessBattle.Domain.Items;
using Microsoft.EntityFrameworkCore;
using CharacterBuffRow = EndlessBattle.Infrastructure.Persistence.Models.CharacterBuff;

namespace EndlessBattle.Infrastructure.Persistence;

// kind 存 BuffKind 名稱；倍率是 DECIMAL(4,2)
public sealed class BuffRepository(EndlessBattleDbContext context) : IBuffRepository
{
    public async Task<IReadOnlyList<CharacterBuff>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterBuffs.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDomain).OrderBy(buff => buff.Kind).ToList();
    }

    public async Task SaveAsync(long characterId, CharacterBuff buff, CancellationToken cancellationToken)
    {
        var kind = buff.Kind.ToString();
        var row = await context.CharacterBuffs
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId && candidate.Kind == kind, cancellationToken);
        if (row is null)
        {
            row = new CharacterBuffRow { CharacterId = characterId, Kind = kind };
            context.CharacterBuffs.Add(row);
        }

        row.Multiplier = (decimal)buff.Multiplier;
        row.StartsAt = buff.StartsAt.UtcDateTime;
        row.EndsAt = buff.EndsAt.UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static CharacterBuff ToDomain(CharacterBuffRow row) =>
        new(
            Enum.Parse<BuffKind>(row.Kind),
            (double)row.Multiplier,
            new DateTimeOffset(DateTime.SpecifyKind(row.StartsAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(row.EndsAt, DateTimeKind.Utc)));
}
