using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Tests.Support;

// 單一擊殺目標的進行中任務；ActiveMission 的目標是清單，比對時要比內容
public static class TrackedMissions
{
    public static ActiveMission Single(
        string missionId, string title, string monsterId, int Required, int Progress, DateTimeOffset AcceptedAt, DateTimeOffset? ExpiresAt,
        bool RequiresDelivery = false) =>
        new(missionId, title, [new MissionTarget(monsterId, Required, Progress)], AcceptedAt, ExpiresAt, RequiresDelivery);

    public static ActiveMission WithProgress(this ActiveMission mission, int progress) =>
        mission with { Targets = [mission.Targets[0] with { Progress = progress }] };

    public static void AssertSame(IEnumerable<ActiveMission> expected, IEnumerable<ActiveMission> actual) =>
        Assert.Equal(expected.Select(Describe), actual.Select(Describe));

    private static string Describe(ActiveMission mission) =>
        $"{mission with { Targets = [] }} {string.Join(",", mission.Targets)}";
}
