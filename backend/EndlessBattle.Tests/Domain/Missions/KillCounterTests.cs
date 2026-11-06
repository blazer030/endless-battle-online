using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

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
        var hunt = TrackedMissions.Single("test-hunt", "測試討伐", "test-sprite", Required: 3, Progress: 0, AcceptedAt: Accepted, ExpiresAt: null);
        var bounty = TrackedMissions.Single("test-sprite-bounty", "測試懸賞", "test-sprite", Required: 2, Progress: 1, AcceptedAt: Accepted, ExpiresAt: null);
        var reached = TrackedMissions.Single("sprite-reached", "已達成", "test-sprite", Required: 2, Progress: 2, AcceptedAt: Accepted, ExpiresAt: null);
        var wolves = TrackedMissions.Single("test-skill-reward", "測試傳授", "test-wolf", Required: 2, Progress: 0, AcceptedAt: Accepted, ExpiresAt: null);

        var result = KillCounter.Apply([hunt, reached, bounty, wolves], "test-sprite", Now);

        TrackedMissions.AssertSame([hunt.WithProgress(1), reached, bounty.WithProgress(2), wolves], result.Missions);
        TrackedMissions.AssertSame([hunt.WithProgress(1), bounty.WithProgress(2)], result.Advanced);
    }

    // 嚴格早於 ExpiresAt 才計入：剛好在重置時刻打完不算，前一刻算
    [Fact]
    public void KillCounter_IgnoresKillsAtOrAfterExpiry()
    {
        var daily = TrackedMissions.Single("test-daily", "測試每日", "test-sprite", Required: 2, Progress: 0, AcceptedAt: Accepted, ExpiresAt: Now);

        var atExpiry = KillCounter.Apply([daily], "test-sprite", Now);
        var justBefore = KillCounter.Apply([daily], "test-sprite", Now.AddTicks(-1));

        TrackedMissions.AssertSame([daily], atExpiry.Missions);
        Assert.Empty(atExpiry.Advanced);
        TrackedMissions.AssertSame([daily.WithProgress(1)], justBefore.Advanced);
    }

    // 接取之前打完的不算，接取當下（含）起才計入
    [Fact]
    public void KillCounter_IgnoresKillsBeforeAcceptance()
    {
        var hunt = TrackedMissions.Single("test-hunt", "測試討伐", "test-sprite", Required: 3, Progress: 0, AcceptedAt: Now, ExpiresAt: null);

        var before = KillCounter.Apply([hunt], "test-sprite", Now.AddTicks(-1));
        var atAcceptance = KillCounter.Apply([hunt], "test-sprite", Now);

        TrackedMissions.AssertSame([hunt], before.Missions);
        Assert.Empty(before.Advanced);
        TrackedMissions.AssertSame([hunt.WithProgress(1)], atAcceptance.Advanced);
    }

    // 任務怪也要等接取之後才出現
    [Fact]
    public void KillCounter_MissionMonsterNotSpawnableBeforeAcceptance()
    {
        var chiefHunt = TrackedMissions.Single("test-chief-hunt", "測試首領", "test-sprite-chief", Required: 1, Progress: 0, AcceptedAt: Now, ExpiresAt: null);

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
        var kill = DailyMissions.Resolve(daily, characterId: 7, yesterday).Kills[0];
        MissionRecord[] records =
        [
            new("test-daily", MissionState.Active, [1], yesterday, Now.AddDays(-1), null),
            new("test-hunt", MissionState.Active, [2], null, Now, null),
        ];

        var tracked = KillCounter.Track(records, missions, characterId: 7);
        var mismatched = KillCounter.Track([records[0] with { DailyPeriod = null }], missions, characterId: 7);

        TrackedMissions.AssertSame(
            [
                TrackedMissions.Single("test-daily", "測試每日", kill.MonsterId, kill.Count, 1, Now.AddDays(-1), new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero)),
                TrackedMissions.Single("test-hunt", "測試討伐", "test-sprite", 3, 2, Now, null),
            ],
            tracked);
        Assert.Empty(mismatched);
    }

    private static readonly MissionDefinition FakesDefinition = new(
        "mission-29", "公會", "冒牌傳說", "說明", "謝謝", 1, 1, false, [new KillTarget("monster-111", 3), new KillTarget("monster-112", 3)], [],
        new MissionRequirements(), new MissionReward(0, []), [], []);

    private static ActiveMission Fakes(int heroes, int summoners) => new(
        "mission-29", "冒牌傳說",
        [new MissionTarget("monster-111", Required: 3, heroes), new MissionTarget("monster-112", Required: 3, summoners)],
        Accepted, ExpiresAt: null);

    [Fact]
    public void KillCounter_CountsTargetsSeparately()
    {
        var result = KillCounter.Apply([Fakes(heroes: 2, summoners: 1)], "monster-112", Now);

        Assert.Equal([2, 2], result.Missions.Single().Targets.Select(target => target.Progress));
        Assert.Equal([2, 2], result.Advanced.Single().Targets.Select(target => target.Progress));
    }

    [Fact]
    public void KillCounter_FullTargetDoesNotCount()
    {
        var result = KillCounter.Apply([Fakes(heroes: 3, summoners: 1)], "monster-111", Now);

        Assert.Equal([3, 1], result.Missions.Single().Targets.Select(target => target.Progress));
        Assert.Empty(result.Advanced);
    }

    [Fact]
    public void KillCounter_MissionStaysOpenUntilEveryTargetIsFull()
    {
        Assert.Contains("mission-29", KillCounter.SpawnableMissionIds([Fakes(heroes: 3, summoners: 2)], Now));
        Assert.Empty(KillCounter.SpawnableMissionIds([Fakes(heroes: 3, summoners: 3)], Now));
    }

    [Fact]
    public void KillCounter_TracksEveryTargetWithItsOwnProgress()
    {
        var mission = FakesDefinition;

        var tracked = KillCounter.Track([new MissionRecord("mission-29", MissionState.Active, [3, 1], null, Now, null)], [mission], characterId: 7);

        Assert.Equal(
            [new MissionTarget("monster-111", 3, 3), new MissionTarget("monster-112", 3, 1)],
            tracked.Single().Targets);
    }

    [Fact]
    public void KillCounter_DoesNotTrackMissionWithEveryTargetFull()
    {
        var mission = FakesDefinition;

        var tracked = KillCounter.Track([new MissionRecord("mission-29", MissionState.Active, [3, 3], null, Now, null)], [mission], characterId: 7);

        Assert.Empty(tracked);
    }

    // 擊殺加交付的混合任務帶上 RequiresDelivery，戰報才知道擊殺滿了還不能回報
    [Fact]
    public void KillCounter_TracksWhetherDeliveryIsRequired()
    {
        MissionDefinition Mission(string id, IReadOnlyList<ItemQuantity> deliver) => new(
            id, "村長", "標題", "說明", "謝謝", 1, 1, false, [new KillTarget("test-sprite", 3)], deliver,
            new MissionRequirements(), new MissionReward(50, []), [], []);
        MissionRecord[] records =
            [new("mixed", MissionState.Active, [0], null, Now, null), new("kill-only", MissionState.Active, [0], null, Now, null)];

        var tracked = KillCounter.Track(records, [Mission("mixed", [new ItemQuantity("test-ore", 2)]), Mission("kill-only", [])], characterId: 7);

        Assert.Equal([("mixed", true), ("kill-only", false)], tracked.Select(mission => (mission.MissionId, mission.RequiresDelivery)));
    }
}
