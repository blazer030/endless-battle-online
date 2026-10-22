using EndlessBattle.Application.Common;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class MercenaryRepository(EndlessBattleDbContext context) : IMercenaryRepository
{
    public async Task<MercenaryContract?> FindAsync(long characterId, CancellationToken cancellationToken)
    {
        var row = await context.CharacterMercenaries.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        return row is null
            ? null
            : new MercenaryContract(
                row.MercenaryId, row.Name, row.Remaining, row.PlaceId,
                row.NextFightAt is { } nextFightAt ? ToUtc(nextFightAt) : null, ToUtc(row.HiredAt));
    }

    public async Task AddAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken)
    {
        var row = new CharacterMercenary { CharacterId = characterId };
        Apply(row, contract);
        context.CharacterMercenaries.Add(row);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (MySqlErrors.IsDuplicateKey(exception))
        {
            throw new ConflictException("已有傭兵合約");
        }
    }

    public async Task UpdateAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken)
    {
        var row = await context.CharacterMercenaries.SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        if (row is null)
        {
            return;
        }

        Apply(row, contract);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(long characterId, CancellationToken cancellationToken)
    {
        var row = await context.CharacterMercenaries.SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        if (row is null)
        {
            return;
        }

        context.CharacterMercenaries.Remove(row);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(CharacterMercenary row, MercenaryContract contract)
    {
        row.MercenaryId = contract.MercenaryId;
        row.Name = contract.Name;
        row.Remaining = contract.Remaining;
        row.PlaceId = contract.PlaceId;
        row.NextFightAt = contract.NextFightAt?.UtcDateTime;
        row.HiredAt = contract.HiredAt.UtcDateTime;
    }

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
