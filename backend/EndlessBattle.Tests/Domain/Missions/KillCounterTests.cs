using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Domain.Missions;

public class KillCounterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Accepted = Now.AddHours(-1);

    // 兩個未達成的任務都要測試雪精：一次擊殺兩邊各 +1（WOG 只算其中一個，裁定全部都算）；
    // 同樣要雪精但已達成的維持不動，目標不同的狼任務不動
    [Fact]
    public void KillCounter_CountsEveryMatchingMission()
    {
        var hunt = new ActiveMission("test-hunt", "測試討伐", "test-sprite", Required: 3, Progress: 0, AcceptedAt: Accepted, ExpiresAt: null);
        var bounty = new ActiveMission("test-sprite-bounty", "測試懸賞", "test-sprite", Required: 2, Progress: 1, AcceptedAt: Accepted, ExpiresAt: null);
        var reached = new ActiveMission("sprite-reached", "已達成", "test-sprite", Required: 2, Progress: 2, AcceptedAt: Accepted, ExpiresAt: null);
        var wolves = new ActiveMission("test-skill-reward", "測試傳授", "test-wolf", Required: 2, Progress: 0, AcceptedAt: Accepted, ExpiresAt: null);

        var result = KillCounter.Apply([hunt, reached, bounty, wolves], "test-sprite", Now);

        Assert.Equal([hunt with { Progress = 1 }, reached, bounty with { Progress = 2 }, wolves], result.Missions);
        Assert.Equal([hunt with { Progress = 1 }, bounty with { Progress = 2 }], result.Advanced);
    }

    // 嚴格早於 ExpiresAt 才計入：剛好在重置時刻打完不算，前一刻算
    [Fact]
    public void KillCounter_IgnoresKillsAtOrAfterExpiry()
    {
        var daily = new ActiveMission("test-daily", "測試每日", "test-sprite", Required: 2, Progress: 0, AcceptedAt: Accepted, ExpiresAt: Now);

        var atExpiry = KillCounter.Apply([daily], "test-sprite", Now);
        var justBefore = KillCounter.Apply([daily], "test-sprite", Now.AddTicks(-1));

        Assert.Equal([daily], atExpiry.Missions);
        Assert.Empty(atExpiry.Advanced);
        Assert.Equal([daily with { Progress = 1 }], justBefore.Advanced);
    }

    // 接取之前打完的不算，接取當下（含）起才計入
    [Fact]
    public void KillCounter_IgnoresKillsBeforeAcceptance()
    {
        var hunt = new ActiveMission("test-hunt", "測試討伐", "test-sprite", Required: 3, Progress: 0, AcceptedAt: Now, ExpiresAt: null);

        var before = KillCounter.Apply([hunt], "test-sprite", Now.AddTicks(-1));
        var atAcceptance = KillCounter.Apply([hunt], "test-sprite", Now);

        Assert.Equal([hunt], before.Missions);
        Assert.Empty(before.Advanced);
        Assert.Equal([hunt with { Progress = 1 }], atAcceptance.Advanced);
    }

    // 任務怪也要等接取之後才出現
    [Fact]
    public void KillCounter_MissionMonsterNotSpawnableBeforeAcceptance()
    {
        var chiefHunt = new ActiveMission("test-chief-hunt", "測試首領", "test-sprite-chief", Required: 1, Progress: 0, AcceptedAt: Now, ExpiresAt: null);

        Assert.Empty(KillCounter.SpawnableMissionIds([chiefHunt], Now.AddTicks(-1)));
        Assert.Equal(["test-chief-hunt"], KillCounter.SpawnableMissionIds([chiefHunt], Now));
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
                new ActiveMission("test-daily", "測試每日", kill.MonsterId, kill.Count, 1, Now.AddDays(-1), new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero)),
                new ActiveMission("test-hunt", "測試討伐", "test-sprite", 3, 2, Now, null),
            ],
            tracked);
        Assert.Empty(mismatched);
    }
}
