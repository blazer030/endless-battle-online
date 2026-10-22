using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content/missions.json 與任務怪：plan 8 Task 1 的任務總表（docs/research/mission-table.md）
public class MissionTableTests
{
    private const long MoneyPerLevel = 50;

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static IEnumerable<MissionDefinition> Regular => Content.AllMissions.Where(mission => !mission.Daily);

    private static IEnumerable<MissionDefinition> Dailies => Content.AllMissions.Where(mission => mission.Daily);

    // 懸賞類每日任務取這些地區 meet 0 的怪（WOG 模板 271～274）
    public static TheoryData<string, int[]> BountyPlaces => new()
    {
        { "daily-bounty-swamp", [3, 4] },
        { "daily-bounty-ruins", [5, 6] },
        { "daily-bounty-crown", [7, 13] },
        { "daily-bounty-beasts", [8, 16] },
    };

    [Fact]
    public void MissionTable_CoversOpenAreas()
    {
        Assert.Equal(
            [
                "slime-cleanup", "bog-cult", "crown-road-patrol", "wraith-bargain", "academy-dust", "veteran-iron-skin",
                "dryad-blessing", "sky-sentry", "fog-fox", "ogre-king", "frost-hides", "star-song", "desert-raiders",
                "relic-core", "ghost-captain-treasure",
            ],
            Regular.Select(mission => mission.Id));

        var perArea = Regular
            .GroupBy(mission => Content.GetPlace(mission.PlaceId!.Value).AreaId)
            .ToDictionary(group => group.Key, group => group.Count());
        Assert.Equal(4, perArea["central"]);
        Assert.Equal(4, perArea["magic"]);
        Assert.Equal(4, perArea["jungle"]);
        Assert.Equal(3, perArea["doom"]);
        Assert.False(perArea.ContainsKey("restricted"));
    }

    [Fact]
    public void MissionTable_MoneyIsFiftyPerLevel()
    {
        Assert.All(Regular, mission =>
        {
            Assert.Equal(MoneyPerLevel * mission.Level, mission.Rewards.Money);
            Assert.Equal(MissionDefinition.ExpPerLevel * mission.Level, mission.Exp);
        });
    }

    [Fact]
    public void MissionTable_HasSixDailies()
    {
        Assert.Equal(
            [
                "daily-hunt-novice", "daily-hunt-veteran", "daily-bounty-swamp", "daily-bounty-ruins", "daily-bounty-crown",
                "daily-bounty-beasts",
            ],
            Dailies.Select(mission => mission.Id));

        var novice = Content.GetMission("daily-hunt-novice");
        Assert.Equal(5, novice.Level);
        Assert.Equal(
            [
                new KillTarget("meadow-slime", 6), new KillTarget("shore-crab", 6), new KillTarget("lantern-bat", 6),
                new KillTarget("tunnel-goblin", 6), new KillTarget("bog-toad", 5), new KillTarget("breeze-fox", 5),
                new KillTarget("rot-crawler", 4), new KillTarget("jungle-panther", 4),
            ],
            novice.DailyKills);
        Assert.Equivalent(
            new[]
            {
                new MissionReward(300, []),
                new MissionReward(150, [new ItemQuantity("small-supply", 5)]),
                new MissionReward(150, [new ItemQuantity("iron-ore", 5)]),
                new MissionReward(150, [new ItemQuantity("magic-dust", 5)]),
                new MissionReward(150, [new ItemQuantity("iron-refine-stone-1", 1)]),
            },
            novice.DailyRewards,
            strict: true);

        var veteran = Content.GetMission("daily-hunt-veteran");
        Assert.Equal(25, veteran.Level);
        Assert.Equal(8, veteran.DailyKills.Count);
        Assert.All(veteran.DailyRewards, reward =>
        {
            Assert.Equal(1250, reward.Money);
            Assert.Equal(new ItemQuantity("medium-supply", 3), reward.Items[0]);
        });
        Assert.Equal([1, 2, 2, 2, 2], veteran.DailyRewards.Select(reward => reward.Items.Count));
    }

    [Theory]
    [MemberData(nameof(BountyPlaces))]
    public void MissionTable_BountyPoolsAreMeetZeroTimesFiveToSeven(string missionId, int[] placeIds)
    {
        var mission = Content.GetMission(missionId);
        var expectedKills = Content.AllMonsters
            .Where(monster => placeIds.Contains(monster.PlaceId) && monster.Meet == 0 && monster.MissionId is null)
            .SelectMany(monster => new[] { 5, 6, 7 }.Select(count => new KillTarget(monster.Id, count)))
            .ToList();
        Assert.Equal(expectedKills, mission.DailyKills);

        // 原為交易所資源，改發金錢 round(level × 50 × f)
        var expectedMoney = new[] { 0.8m, 1.0m, 1.2m }
            .Select(factor => (long)Math.Round(mission.Level * MoneyPerLevel * factor, MidpointRounding.AwayFromZero));
        Assert.Equal(expectedMoney, mission.DailyRewards.Select(reward => reward.Money));
        Assert.All(mission.DailyRewards, reward => Assert.Empty(reward.Items));
    }

    [Fact]
    public void MissionTable_MissionMonstersBelongToOneMission()
    {
        var missionMonsters = Content.AllMonsters.Where(monster => monster.MissionId is not null).ToList();
        Assert.Equal(["pyre-high-priest", "rift-ogre-king", "relic-core-colossus"], missionMonsters.Select(monster => monster.Id));

        Assert.All(missionMonsters, monster =>
        {
            var owner = Assert.Single(Content.AllMissions, mission => mission.Kill?.MonsterId == monster.Id);
            Assert.Equal(owner.Id, monster.MissionId);
            Assert.DoesNotContain(Dailies.SelectMany(mission => mission.DailyKills), kill => kill.MonsterId == monster.Id);
            Assert.Equal(60, monster.Meet);
            Assert.Equal([1, 2, 3, 4], monster.SubPlaces);
            Assert.Equal([new DropEntry("crystal-shard", 10)], monster.Drops);
            var place = Content.GetPlace(monster.PlaceId);
            Assert.InRange(monster.Level, place.Recommended.Min, place.Recommended.Max);
        });
        Assert.Equal(("bog-cult", 3, 20), Describe("pyre-high-priest"));
        Assert.Equal(("ogre-king", 11, 35), Describe("rift-ogre-king"));
        Assert.Equal(("relic-core", 5, 75), Describe("relic-core-colossus"));
    }

    private static (string?, int, int) Describe(string monsterId)
    {
        var monster = Content.GetMonster(monsterId);
        return (monster.MissionId, monster.PlaceId, monster.Level);
    }
}
