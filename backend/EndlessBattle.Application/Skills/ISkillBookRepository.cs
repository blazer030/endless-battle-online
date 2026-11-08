using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

public interface ISkillBookRepository
{
    Task<IReadOnlyList<LearnedSkill>> ListAsync(long characterId, CancellationToken cancellationToken);

    Task UpsertAsync(long characterId, LearnedSkill skill, CancellationToken cancellationToken);
}
