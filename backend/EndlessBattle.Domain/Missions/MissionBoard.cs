using System.Diagnostics.CodeAnalysis;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Missions;

public sealed record MissionEntry(MissionDefinition Definition, MissionObjective Objective, MissionRecord? Record, bool Ready, DateTimeOffset? ExpiresAt);

// Owned：建看板時的背包道具總數、CarriedPetMonsterId：攜帶中寵物的物種，回應交付進度用
public sealed record MissionBoardView(
    IReadOnlyList<MissionEntry> Available,
    IReadOnlyList<MissionEntry> Active,
    IReadOnlyList<MissionEntry> Completed,
    DateTimeOffset NextDailyReset,
    IReadOnlyDictionary<string, int> Owned,
    string? CarriedPetMonsterId = null);

public static class MissionBoard
{
    // owned：背包中各道具的總數、carriedPetMonsterId：攜帶中寵物的物種（算 Ready 用）；records 為原始紀錄，這裡負責去掉過期與未知
    public static MissionBoardView Build(
        Character character,
        IReadOnlyList<MissionDefinition> missions,
        IReadOnlyList<MissionRecord> records,
        IReadOnlyDictionary<string, int> owned,
        DateTimeOffset now,
        string? carriedPetMonsterId = null)
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
            .Select(record => Entry(character, byId[record.MissionId], record, owned, carriedPetMonsterId))
            .ToList();

        var completed = current
            .Where(record => record.State == MissionState.Completed)
            .OrderByDescending(record => record.CompletedAt)
            .Select(record => Entry(character, byId[record.MissionId], record, owned, carriedPetMonsterId))
            .ToList();

        return new MissionBoardView(available, active, completed, nextReset, owned, carriedPetMonsterId);
    }

    // 未知任務、過期每日紀錄，以及與定義種類不符（每日卻無週期、一般卻有週期）的紀錄一律忽略
    public static IReadOnlyList<MissionRecord> Current(
        IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, DateTimeOffset now)
    {
        var byId = missions.ToDictionary(mission => mission.Id);
        return records
            .Where(record => Known(record, byId.GetValueOrDefault(record.MissionId)) && !DailyMissions.IsExpired(record, now))
            .ToList();
    }

    // 寫入時要刪掉的紀錄：過期的每日紀錄，以及與定義種類不符的紀錄（Current 看不到，留著會讓再接取撞上唯一鍵）；
    // 未知任務的紀錄保留，內容可能再加回來
    public static IReadOnlyList<MissionRecord> Stale(
        IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, DateTimeOffset now)
    {
        var byId = missions.ToDictionary(mission => mission.Id);
        return records
            .Where(record => DailyMissions.IsExpired(record, now)
                || (byId.TryGetValue(record.MissionId, out var mission) && !Known(record, mission)))
            .ToList();
    }

    // 每個擊殺目標都達成；沒有擊殺目標也算
    public static bool Ready(MissionObjective objective, MissionRecord record) =>
        objective.Kills.Select((kill, index) => record.ProgressOf(index) >= kill.Count).All(reached => reached);

    // 內容中有這個任務，且紀錄的種類與定義相符（每日任務有週期、一般任務沒有）
    internal static bool Known(MissionRecord record, [NotNullWhen(true)] MissionDefinition? definition) =>
        definition is not null && definition.Daily == record.DailyPeriod.HasValue;

    private static MissionEntry Entry(
        Character character, MissionDefinition mission, MissionRecord record, IReadOnlyDictionary<string, int> owned, string? carriedPetMonsterId)
    {
        var objective = DailyMissions.Resolve(mission, character.Id, record.DailyPeriod);
        var ready = record.State == MissionState.Active
            && Ready(objective, record)
            && objective.Deliver.All(item => owned.GetValueOrDefault(item.ItemId) >= item.Quantity)
            && (objective.DeliverPet is null || objective.DeliverPet == carriedPetMonsterId);
        var expiresAt = record.DailyPeriod is { } period ? DailyMissions.PeriodStart(period.AddDays(1)) : (DateTimeOffset?)null;
        return new MissionEntry(mission, objective, record, ready, expiresAt);
    }
}
