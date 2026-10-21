using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

public interface ISkillLoadoutRepository
{
    Task<IReadOnlyList<SkillSlot>> ListAsync(long characterId, CancellationToken cancellationToken);

    Task ReplaceAsync(long characterId, IReadOnlyList<SkillSlot> slots, CancellationToken cancellationToken);
}
