namespace EndlessBattle.Domain.Missions;

public enum MissionState { Active, Completed }

// 角色的一筆任務紀錄；時間皆為 UTC。Progress 依擊殺目標的順序，比目標少的部分視為 0，多的忽略
public sealed record MissionRecord(
    string MissionId, MissionState State, IReadOnlyList<int> Progress, DateOnly? DailyPeriod, DateTimeOffset AcceptedAt, DateTimeOffset? CompletedAt)
{
    public int ProgressOf(int targetIndex) => targetIndex < Progress.Count ? Progress[targetIndex] : 0;
}
