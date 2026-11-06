namespace EndlessBattle.Domain.Missions;

public sealed record MissionTarget(string MonsterId, int Required, int Progress)
{
    public bool Full => Progress >= Required;
}

// 只放開放中的進行中任務（見 KillCounter.Track）；Targets 為空表示只交寵物；AcceptedAt 起（含）才計入擊殺；ExpiresAt 是每日任務週期結束的時刻，一般任務為 null；
// RequiresDelivery：目標另有交付道具或寵物，擊殺滿了還不能回報
public sealed record ActiveMission(
    string MissionId,
    string Title,
    IReadOnlyList<MissionTarget> Targets,
    DateTimeOffset AcceptedAt,
    DateTimeOffset? ExpiresAt,
    bool RequiresDelivery = false)
{
    public bool TargetsFull => Targets.All(target => target.Full);
}
