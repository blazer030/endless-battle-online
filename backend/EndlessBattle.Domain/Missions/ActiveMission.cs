namespace EndlessBattle.Domain.Missions;

// 只放有擊殺目標、尚未達成的進行中任務；AcceptedAt 起（含）才計入擊殺；ExpiresAt 是每日任務週期結束的時刻，一般任務為 null
public sealed record ActiveMission(
    string MissionId, string Title, string MonsterId, int Required, int Progress, DateTimeOffset AcceptedAt, DateTimeOffset? ExpiresAt);
