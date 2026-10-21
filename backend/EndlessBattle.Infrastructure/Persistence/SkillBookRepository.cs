using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class SkillBookRepository(EndlessBattleDbContext context) : ISkillBookRepository
{
    public async Task<IReadOnlyList<LearnedSkill>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        await context.CharacterSkills.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.SkillId)
            .Select(row => new LearnedSkill(row.SkillId, row.Level))
            .ToListAsync(cancellationToken);

    public async Task UpsertAsync(long characterId, LearnedSkill skill, CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO character_skills (character_id, skill_id, level)
            VALUES ({characterId}, {skill.SkillId}, {skill.Level})
            ON DUPLICATE KEY UPDATE level = {skill.Level}
            """,
            cancellationToken);
}
