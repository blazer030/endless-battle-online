using EndlessBattle.Application.Champion;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Champion;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class ChampionRepository(EndlessBattleDbContext context) : IChampionRepository
{
    public async Task<ChampionSeat?> FindAsync(long characterId, CancellationToken cancellationToken)
    {
        var row = await context.CharacterChampions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        return row is null
            ? null
            : new ChampionSeat(
                row.Name, row.Level, row.Element, row.SpecialAttackName, row.Hp, row.HpMax, row.Sp, row.SpMax,
                row.At, row.Mat, row.Df, row.Mdf, row.Agi, row.Au, row.IsPlayer, row.Defenses,
                row.CrownedAt is { } crownedAt ? ToUtc(crownedAt) : null,
                row.LockedUntil is { } lockedUntil ? ToUtc(lockedUntil) : null);
    }

    public async Task SaveAsync(long characterId, ChampionSeat seat, CancellationToken cancellationToken)
    {
        var row = await context.CharacterChampions.SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId, cancellationToken);
        if (row is null)
        {
            row = new CharacterChampion { CharacterId = characterId };
            context.CharacterChampions.Add(row);
        }

        row.Name = seat.Name;
        row.Level = seat.Level;
        row.Element = (sbyte)seat.Element;
        row.SpecialAttackName = seat.SpecialAttackName;
        row.Hp = seat.Hp;
        row.HpMax = seat.HpMax;
        row.Sp = seat.Sp;
        row.SpMax = seat.SpMax;
        row.At = seat.At;
        row.Mat = seat.Mat;
        row.Df = seat.Df;
        row.Mdf = seat.Mdf;
        row.Agi = seat.Agi;
        row.Au = seat.Au;
        row.IsPlayer = seat.IsPlayer;
        row.Defenses = seat.Defenses;
        row.CrownedAt = seat.CrownedAt?.UtcDateTime;
        row.LockedUntil = seat.LockedUntil?.UtcDateTime;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (MySqlErrors.IsDuplicateKey(exception))
        {
            // 兩個請求都以為還沒有冠軍列而同時新增
            throw new ConflictException("冠軍資料已被其他請求更新", exception);
        }
    }

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
