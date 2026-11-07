using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Missions;

// Advanced 是這次 +1 的任務（新狀態），依 Missions 的順序
public sealed record KillResult(IReadOnlyList<ActiveMission> Missions, IReadOnlyList<ActiveMission> Advanced);

public static class KillCounter
{
    // 以這隻怪為目標、該目標未滿、已接取且未過期的任務各 +1（WOG 同一隻怪只算到其中一個任務，裁定全部都算）
    public static KillResult Apply(IReadOnlyList<ActiveMission> missions, string monsterId, DateTimeOffset killedAt)
    {
        var advanced = new List<ActiveMission>();
        var updated = missions.Select(mission =>
        {
            var target = mission.Targets.FirstOrDefault(candidate => candidate.MonsterId == monsterId);
            if (target is null || target.Full || !IsOpen(mission, killedAt))
            {
                return mission;
            }

            var next = mission with
            {
                Targets = mission.Targets.Select(candidate => candidate == target ? candidate with { Progress = candidate.Progress + 1 } : candidate).ToList(),
            };
            advanced.Add(next);
            return next;
        }).ToList();

        return new KillResult(updated, advanced);
    }

    // 進行中且開放（還有未滿的擊殺目標，或沒有擊殺目標但要交寵物）的任務；只交道具的任務不會遇到任務怪，不追蹤。內容中不存在或種類不符（每日卻無週期、一般卻有週期）的紀錄略過。
    // 不以現在時間過濾每日任務：跨過重置的離線結算中，重置前打完的場次仍要計入，交給 ExpiresAt 判斷
    public static IReadOnlyList<ActiveMission> Track(
        IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, long characterId, IReadOnlyList<LearnedSkill> learned)
    {
        var byId = missions.ToDictionary(mission => mission.Id);
        var tracked = new List<ActiveMission>();
        foreach (var record in records)
        {
            var mission = byId.GetValueOrDefault(record.MissionId);
            if (record.State != MissionState.Active || !MissionBoard.Known(record, mission))
            {
                continue;
            }

            var objective = DailyMissions.Resolve(mission, characterId, record.DailyPeriod);
            if (objective.Kills.Count == 0 ? objective.DeliverPet is null : MissionBoard.Ready(objective, record))
            {
                continue;
            }

            var expiresAt = record.DailyPeriod is { } period ? DailyMissions.PeriodStart(period.AddDays(1)) : (DateTimeOffset?)null;
            var targets = objective.Kills.Select((kill, index) => new MissionTarget(kill.MonsterId, kill.Count, record.ProgressOf(index))).ToList();
            tracked.Add(new ActiveMission(mission.Id, mission.Title, targets, record.AcceptedAt, expiresAt,
                RequiresDelivery: objective.Deliver.Count > 0 || objective.DeliverPet is not null,
                SkillTooLow: !MissionRules.HasRequiredSkill(mission, learned)));
        }

        return tracked;
    }

    // 任務怪只在其任務已接取、開放且未過期時出現；任務還開放時，目標已滿的任務怪也會出現，只是不再計數
    public static IReadOnlySet<string> SpawnableMissionIds(IReadOnlyList<ActiveMission> missions, DateTimeOffset at) =>
        missions.Where(mission => IsOpen(mission, at)).Select(mission => mission.MissionId).ToHashSet();

    // 接取當下（含）起、嚴格早於 ExpiresAt 為止
    private static bool IsOpen(ActiveMission mission, DateTimeOffset at) =>
        (mission.Targets.Count == 0 || !mission.TargetsFull)
        && mission.AcceptedAt <= at
        && (mission.ExpiresAt is not { } expiresAt || at < expiresAt);
}
