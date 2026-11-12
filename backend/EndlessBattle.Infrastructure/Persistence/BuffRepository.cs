using EndlessBattle.Application.Items;
using EndlessBattle.Domain.Items;
using Microsoft.EntityFrameworkCore;
using CharacterBuffRow = EndlessBattle.Infrastructure.Persistence.Models.CharacterBuff;

namespace EndlessBattle.Infrastructure.Persistence;

// kind 存 BuffKindCodes 的儲存代碼（經驗是 Exp、職業經驗是 JobExp）；倍率是 DECIMAL(4,2)；item_id 是最後喝的道具，舊資料為 NULL
// 讀取代碼不分大小寫：kind 欄位的定序（utf8mb4_0900_ai_ci）本來就不分，主鍵與查詢也是
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
        var kind = BuffKindCodes.Of(buff.Kind);
        var row = await context.CharacterBuffs
            .SingleOrDefaultAsync(candidate => candidate.CharacterId == characterId && candidate.Kind == kind, cancellationToken);
        if (row is null)
        {
            row = new CharacterBuffRow { CharacterId = characterId, Kind = kind };
            context.CharacterBuffs.Add(row);
        }

        row.ItemId = buff.ItemId;
        row.Multiplier = (decimal)buff.Multiplier;
        row.StartsAt = buff.StartsAt.UtcDateTime;
        row.EndsAt = buff.EndsAt.UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static CharacterBuff ToDomain(CharacterBuffRow row) =>
        new(
            BuffKindCodes.Parse(row.Kind),
            (double)row.Multiplier,
            new DateTimeOffset(DateTime.SpecifyKind(row.StartsAt, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(row.EndsAt, DateTimeKind.Utc)),
            row.ItemId);
}
