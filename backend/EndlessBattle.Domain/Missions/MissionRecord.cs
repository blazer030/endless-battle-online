namespace EndlessBattle.Domain.Missions;

public enum MissionState { Active, Completed }

// 角色的一筆任務紀錄；時間皆為 UTC
public sealed record MissionRecord(
    string MissionId, MissionState State, int Progress, DateOnly? DailyPeriod, DateTimeOffset AcceptedAt, DateTimeOffset? CompletedAt);
