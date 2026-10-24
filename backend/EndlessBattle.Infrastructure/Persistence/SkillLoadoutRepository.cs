using EndlessBattle.Application.Common;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class SkillLoadoutRepository(EndlessBattleDbContext context) : ISkillLoadoutRepository
{
    private readonly IUnitOfWork unitOfWork = new EfUnitOfWork(context);

    public async Task<IReadOnlyList<SkillSlot>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        await context.CharacterSkillSlots.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Slot)
            .Select(row => new SkillSlot(row.Slot, row.SkillId))
            .ToListAsync(cancellationToken);

    // 先清空再寫入，包在同一個交易內，失敗時舊配置不變；在用例的交易裡呼叫時加入外層交易
    public Task ReplaceAsync(long characterId, IReadOnlyList<SkillSlot> slots, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            await context.CharacterSkillSlots.Where(row => row.CharacterId == characterId).ExecuteDeleteAsync(cancellationToken);
            context.CharacterSkillSlots.AddRange(slots.Select(slot => new CharacterSkillSlot
            {
                CharacterId = characterId,
                Slot = checked((sbyte)slot.Slot),
                SkillId = slot.SkillId,
            }));
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
}
