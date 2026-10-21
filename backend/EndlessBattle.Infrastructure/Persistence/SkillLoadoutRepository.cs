using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class SkillLoadoutRepository(EndlessBattleDbContext context) : ISkillLoadoutRepository
{
    public async Task<IReadOnlyList<SkillSlot>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        await context.CharacterSkillSlots.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Slot)
            .Select(row => new SkillSlot(row.Slot, row.SkillId, row.Chance))
            .ToListAsync(cancellationToken);

    // 先清空再寫入，包在同一個交易內，失敗時舊配置不變
    public async Task ReplaceAsync(long characterId, IReadOnlyList<SkillSlot> slots, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.CharacterSkillSlots.Where(row => row.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
        context.CharacterSkillSlots.AddRange(slots.Select(slot => new CharacterSkillSlot
        {
            CharacterId = characterId,
            Slot = checked((sbyte)slot.Slot),
            SkillId = slot.SkillId,
            Chance = slot.Chance,
        }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
