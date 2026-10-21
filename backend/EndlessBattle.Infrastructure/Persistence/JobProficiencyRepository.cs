using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Progression;
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

    // 單一 upsert 由資料庫累加，同時寫入也不會遺失差值
    public async Task AddAsync(
        long characterId, string jobId, long proficiencyDelta, long skillPointsDelta, CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO character_job_proficiency (character_id, job_id, proficiency, skill_points)
            VALUES ({characterId}, {jobId}, {proficiencyDelta}, {skillPointsDelta})
            ON DUPLICATE KEY UPDATE proficiency = proficiency + {proficiencyDelta}, skill_points = skill_points + {skillPointsDelta}
            """,
            cancellationToken);

    // 檢查與扣除在同一句 UPDATE：讀取後被別的請求花掉也不會扣成負數
    public async Task SpendSkillPointsAsync(long characterId, string jobId, long cost, CancellationToken cancellationToken)
    {
        var affected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE character_job_proficiency SET skill_points = skill_points - {cost}
            WHERE character_id = {characterId} AND job_id = {jobId} AND skill_points >= {cost}
            """,
            cancellationToken);
        if (affected == 0)
        {
            throw new ConflictException("技能熟練度已變動");
        }
    }
}
