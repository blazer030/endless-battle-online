namespace EndlessBattle.Domain.Missions;

// Advanced 是這次 +1 的任務（新狀態），依 Missions 的順序
public sealed record KillResult(IReadOnlyList<ActiveMission> Missions, IReadOnlyList<ActiveMission> Advanced);

public static class KillCounter
{
    // 以這隻怪為目標、尚未達成且未過期的任務各 +1（WOG 同一隻怪只算到其中一個任務，裁定全部都算）
    public static KillResult Apply(IReadOnlyList<ActiveMission> missions, string monsterId, DateTimeOffset killedAt)
    {
        var advanced = new List<ActiveMission>();
        var updated = missions.Select(mission =>
        {
            if (mission.MonsterId != monsterId || !IsOpen(mission, killedAt))
            {
                return mission;
            }

            var next = mission with { Progress = mission.Progress + 1 };
            advanced.Add(next);
            return next;
        }).ToList();

        return new KillResult(updated, advanced);
    }

    // 進行中、有擊殺目標且尚未達成的任務；內容中不存在或種類不符（每日卻無週期、一般卻有週期）的紀錄略過。
    // 不以現在時間過濾每日任務：跨過重置的離線結算中，重置前打完的場次仍要計入，交給 ExpiresAt 判斷
    public static IReadOnlyList<ActiveMission> Track(
        IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, long characterId)
    {
        var byId = missions.ToDictionary(mission => mission.Id);
        var tracked = new List<ActiveMission>();
        foreach (var record in records)
        {
            if (record.State != MissionState.Active
                || !byId.TryGetValue(record.MissionId, out var mission)
                || mission.Daily != record.DailyPeriod.HasValue)
            {
                continue;
            }

            var objective = DailyMissions.Resolve(mission, characterId, record.DailyPeriod);
            if (objective.Kill is not { } kill || record.Progress >= kill.Count)
            {
                continue;
            }

            var expiresAt = record.DailyPeriod is { } period ? DailyMissions.PeriodStart(period.AddDays(1)) : (DateTimeOffset?)null;
            tracked.Add(new ActiveMission(mission.Id, mission.Title, kill.MonsterId, kill.Count, record.Progress, expiresAt));
        }

        return tracked;
    }

    // 任務怪只在其任務尚未達成且未過期時出現
    public static IReadOnlySet<string> SpawnableMissionIds(IReadOnlyList<ActiveMission> missions, DateTimeOffset at) =>
        missions.Where(mission => IsOpen(mission, at)).Select(mission => mission.MissionId).ToHashSet();

    // 嚴格早於 ExpiresAt 才算未過期
    private static bool IsOpen(ActiveMission mission, DateTimeOffset at) =>
        mission.Progress < mission.Required && (mission.ExpiresAt is not { } expiresAt || at < expiresAt);
}
