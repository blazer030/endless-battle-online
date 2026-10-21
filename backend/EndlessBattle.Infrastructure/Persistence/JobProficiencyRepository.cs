using EndlessBattle.Application.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class JobProficiencyRepository(EndlessBattleDbContext context) : IJobProficiencyRepository
{
    public async Task<IReadOnlyList<JobProficiency>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        await context.CharacterJobProficiencies.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.JobId)
            .Select(row => new JobProficiency(row.JobId, row.Proficiency, row.SkillPoints))
            .ToListAsync(cancellationToken);

    // 單人遊戲，不處理同時寫入：先讀再新增或累加
    public async Task AddAsync(
        long characterId, string jobId, long proficiencyDelta, long skillPointsDelta, CancellationToken cancellationToken)
    {
        var row = await context.CharacterJobProficiencies
            .SingleOrDefaultAsync(existing => existing.CharacterId == characterId && existing.JobId == jobId, cancellationToken);
        if (row is null)
        {
            row = new CharacterJobProficiency { CharacterId = characterId, JobId = jobId };
            context.CharacterJobProficiencies.Add(row);
        }

        row.Proficiency += proficiencyDelta;
        row.SkillPoints += skillPointsDelta;
        await context.SaveChangesAsync(cancellationToken);
    }
}
