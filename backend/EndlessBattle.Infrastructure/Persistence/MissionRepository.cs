using EndlessBattle.Application.Common;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class MissionRepository(EndlessBattleDbContext context) : IMissionRepository
{
    public async Task<IReadOnlyList<MissionRecord>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterMissions.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new MissionRecord(
            row.MissionId, Enum.Parse<MissionState>(row.State), row.Progress,
            row.DailyPeriod is { } period ? DateOnly.FromDateTime(period) : null,
            ToUtc(row.AcceptedAt), row.CompletedAt is { } completedAt ? ToUtc(completedAt) : null)).ToList();
    }

    public async Task AddAsync(long characterId, MissionRecord record, CancellationToken cancellationToken)
    {
        context.CharacterMissions.Add(new CharacterMission
        {
            CharacterId = characterId,
            MissionId = record.MissionId,
            State = record.State.ToString(),
            Progress = record.Progress,
            DailyPeriod = record.DailyPeriod?.ToDateTime(TimeOnly.MinValue),
            AcceptedAt = record.AcceptedAt.UtcDateTime,
            CompletedAt = record.CompletedAt?.UtcDateTime,
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (MySqlErrors.IsDuplicateKey(exception))
        {
            throw new ConflictException("任務已存在", exception);
        }
    }

    public async Task UpdateProgressAsync(long characterId, string missionId, int progress, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, missionId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.Progress = progress;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(long characterId, string missionId, DateTimeOffset completedAt, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, missionId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.State = nameof(MissionState.Completed);
        row.CompletedAt = completedAt.UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(long characterId, string missionId, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, missionId, cancellationToken);
        if (row is null)
        {
            return;
        }

        context.CharacterMissions.Remove(row);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<CharacterMission?> FindAsync(long characterId, string missionId, CancellationToken cancellationToken) =>
        context.CharacterMissions.SingleOrDefaultAsync(row => row.CharacterId == characterId && row.MissionId == missionId, cancellationToken);

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
