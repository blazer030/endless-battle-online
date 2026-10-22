using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Domain.Missions;

public class KillCounterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    // 兩個任務都要測試雪精：未達成的 +1，已達成的維持；目標不同的狼任務不動
    [Fact]
    public void KillCounter_CountsEveryMatchingMission()
    {
        var hunt = new ActiveMission("test-hunt", "測試討伐", "test-sprite", Required: 3, Progress: 0, ExpiresAt: null);
        var bounty = new ActiveMission("test-sprite-bounty", "測試懸賞", "test-sprite", Required: 2, Progress: 2, ExpiresAt: null);
        var wolves = new ActiveMission("test-skill-reward", "測試傳授", "test-wolf", Required: 2, Progress: 0, ExpiresAt: null);

        var result = KillCounter.Apply([hunt, bounty, wolves], "test-sprite", Now);

        Assert.Equal([hunt with { Progress = 1 }, bounty, wolves], result.Missions);
        Assert.Equal([hunt with { Progress = 1 }], result.Advanced);
    }

    // 嚴格早於 ExpiresAt 才計入：剛好在重置時刻打完不算，前一刻算
    [Fact]
    public void KillCounter_IgnoresKillsAtOrAfterExpiry()
    {
        var daily = new ActiveMission("test-daily", "測試每日", "test-sprite", Required: 2, Progress: 0, ExpiresAt: Now);

        var atExpiry = KillCounter.Apply([daily], "test-sprite", Now);
        var justBefore = KillCounter.Apply([daily], "test-sprite", Now.AddTicks(-1));

        Assert.Equal([daily], atExpiry.Missions);
        Assert.Empty(atExpiry.Advanced);
        Assert.Equal([daily with { Progress = 1 }], justBefore.Advanced);
    }

    // 每日任務以紀錄的週期算到下一個 09:00（01:00 UTC）為止，不看現在時間（前一天的紀錄照樣帶上，交給 ExpiresAt）；
    // 種類不符的紀錄（每日卻沒有週期）略過
    [Fact]
    public void KillCounter_TracksDailyUntilItsPeriodEnds()
    {
        var missions = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent")).AllMissions;
        var yesterday = new DateOnly(2026, 10, 21);
        var daily = missions.Single(mission => mission.Id == "test-daily");
        var kill = DailyMissions.Resolve(daily, characterId: 7, yesterday).Kill!;
        MissionRecord[] records =
        [
            new("test-daily", MissionState.Active, 1, yesterday, Now.AddDays(-1), null),
            new("test-hunt", MissionState.Active, 2, null, Now, null),
        ];

        var tracked = KillCounter.Track(records, missions, characterId: 7);
        var mismatched = KillCounter.Track([records[0] with { DailyPeriod = null }], missions, characterId: 7);

        Assert.Equal(
            [
                new ActiveMission("test-daily", "測試每日", kill.MonsterId, kill.Count, 1, new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero)),
                new ActiveMission("test-hunt", "測試討伐", "test-sprite", 3, 2, null),
            ],
            tracked);
        Assert.Empty(mismatched);
    }
}
