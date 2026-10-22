using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Missions;

public sealed record MissionEntry(MissionDefinition Definition, MissionObjective Objective, MissionRecord? Record, bool Ready, DateTimeOffset? ExpiresAt);

public sealed record MissionBoardView(
    IReadOnlyList<MissionEntry> Available, IReadOnlyList<MissionEntry> Active, IReadOnlyList<MissionEntry> Completed, DateTimeOffset NextDailyReset);

public static class MissionBoard
{
    // owned：背包中各道具的總數（算 Ready 用）；records 為原始紀錄，這裡負責去掉過期與未知
    public static MissionBoardView Build(
        Character character,
        IReadOnlyList<MissionDefinition> missions,
        IReadOnlyList<MissionRecord> records,
        IReadOnlyDictionary<string, int> owned,
        DateTimeOffset now)
    {
        var current = Current(records, missions, now);
        var byId = missions.ToDictionary(mission => mission.Id);
        var period = DailyMissions.PeriodOf(now);
        var nextReset = DailyMissions.NextReset(now);

        var available = missions
            .Where(mission => MissionRules.CanAccept(character, mission, current) is [] or ["tooMany"])
            .OrderBy(mission => mission.Daily ? 0 : 1)
            .ThenBy(mission => mission.Daily ? 0 : mission.Level)
            .Select(mission => new MissionEntry(
                mission, DailyMissions.Resolve(mission, character.Id, period), null, false, mission.Daily ? nextReset : null))
            .ToList();

        var active = current
            .Where(record => record.State == MissionState.Active)
            .OrderBy(record => record.AcceptedAt)
            .Select(record => Entry(character, byId[record.MissionId], record, owned))
            .ToList();

        var completed = current
            .Where(record => record.State == MissionState.Completed)
            .OrderByDescending(record => record.CompletedAt)
            .Select(record => Entry(character, byId[record.MissionId], record, owned))
            .ToList();

        return new MissionBoardView(available, active, completed, nextReset);
    }

    // 未知任務、過期每日紀錄，以及與定義種類不符（每日卻無週期、一般卻有週期）的紀錄一律忽略
    public static IReadOnlyList<MissionRecord> Current(
        IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, DateTimeOffset now)
    {
        var dailyById = missions.ToDictionary(mission => mission.Id, mission => mission.Daily);
        return records
            .Where(record => dailyById.TryGetValue(record.MissionId, out var daily)
                && daily == record.DailyPeriod.HasValue
                && !DailyMissions.IsExpired(record, now))
            .ToList();
    }

    private static MissionEntry Entry(Character character, MissionDefinition mission, MissionRecord record, IReadOnlyDictionary<string, int> owned)
    {
        var objective = DailyMissions.Resolve(mission, character.Id, record.DailyPeriod);
        var ready = record.State == MissionState.Active
            && (objective.Kill is null || record.Progress >= objective.Kill.Count)
            && objective.Deliver.All(item => owned.GetValueOrDefault(item.ItemId) >= item.Quantity);
        var expiresAt = record.DailyPeriod is { } period ? DailyMissions.PeriodStart(period.AddDays(1)) : (DateTimeOffset?)null;
        return new MissionEntry(mission, objective, record, ready, expiresAt);
    }
}
