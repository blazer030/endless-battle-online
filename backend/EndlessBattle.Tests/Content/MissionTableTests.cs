using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content/missions.json 與任務怪：任務總表（docs/research/mission-table.md，計畫 8 Task 1、計畫 9 Task 8）
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
                "tamer-trial", "dryad-blessing", "sky-sentry", "fog-fox", "ogre-king", "frost-hides", "star-song", "desert-raiders",
                "relic-core", "ghost-captain-treasure",
            ],
            Regular.Select(mission => mission.Id));

        var perArea = Regular
            .GroupBy(mission => Content.GetPlace(mission.PlaceId!.Value).AreaId)
            .ToDictionary(group => group.Key, group => group.Count());
        Assert.Equal(4, perArea["central"]);
        Assert.Equal(5, perArea["magic"]);
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

    private static readonly string[] MedalOrder = ["continent-medal", "kingdom-medal", "rainforest-medal", "silver-medal", "royal-city-medal"];

    [Fact]
    public void MissionTable_HasEightDailies()
    {
        Assert.Equal(
            [
                "daily-hunt-novice", "daily-pet-plains", "daily-hunt-veteran", "daily-pet-frontier", "daily-bounty-swamp",
                "daily-bounty-ruins", "daily-bounty-crown", "daily-bounty-beasts",
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

        var veteran = Content.GetMission("daily-hunt-veteran");
        Assert.Equal(25, veteran.Level);
        Assert.Equal(8, veteran.DailyKills.Count);
        Assert.All(Dailies, mission => Assert.True(mission.DailyKills.Count > 0 ^ mission.DailyPets.Count > 0));
    }

    // WOG 222／224 的報酬是勳章（計畫 8 暫以金錢與道具代替）
    [Fact]
    public void MissionTable_HuntDailiesPayWogMedals()
    {
        var novice = Content.GetMission("daily-hunt-novice");
        Assert.Equivalent(
            MedalOrder.Select(medal => new MissionReward(0, [new ItemQuantity(medal, 1)])).ToArray(),
            novice.DailyRewards,
            strict: true);

        var veteran = Content.GetMission("daily-hunt-veteran");
        Assert.Equivalent(
            MedalOrder.Select(medal => new MissionReward(0, [new ItemQuantity("medium-supply", 3), new ItemQuantity(medal, 2)])).ToArray(),
            veteran.DailyRewards,
            strict: true);
        Assert.All(MedalOrder, medal => Assert.Equal(ItemCategory.Medal, Content.GetItem(medal).Category));
    }

    // WOG 223／225：交出攜帶中的寵物；目標池是地區 1～4／5～8 meet 0 的怪，報酬附捕捉道具
    [Theory]
    [InlineData("daily-pet-plains", 1, 4, 10, "capture-net")]
    [InlineData("daily-pet-frontier", 5, 8, 30, "capture-cage")]
    public void MissionTable_PetDailiesUseMeetZeroMonsters(string missionId, int firstPlace, int lastPlace, int level, string capture)
    {
        var mission = Content.GetMission(missionId);

        Assert.Equal(("獸欄管理員", level), (mission.Giver, mission.Level));
        var expected = Content.AllMonsters
            .Where(monster => monster.PlaceId >= firstPlace && monster.PlaceId <= lastPlace && monster.Meet == 0 && monster.MissionId is null)
            .Select(monster => monster.Id)
            .ToList();
        Assert.Equal(expected, mission.DailyPets);
        Assert.Equal(6, expected.Count);
        Assert.Empty(mission.DailyKills);

        var silver = capture == "capture-cage"
            ? new MissionReward(0, [new ItemQuantity(capture, 1), new ItemQuantity("silver-medal", 1)])
            : new MissionReward(0, [new ItemQuantity("silver-medal", 1)]);
        Assert.Equivalent(
            new[]
            {
                new MissionReward(0, [new ItemQuantity(capture, 1), new ItemQuantity("continent-medal", 1)]),
                new MissionReward(0, [new ItemQuantity(capture, 1), new ItemQuantity("kingdom-medal", 1)]),
                new MissionReward(0, [new ItemQuantity(capture, 1), new ItemQuantity("rainforest-medal", 1)]),
                silver,
                new MissionReward(0, [new ItemQuantity(capture, 1), new ItemQuantity("royal-city-medal", 1)]),
            },
            mission.DailyRewards,
            strict: true);
    }

    [Fact]
    public void MissionTable_TamerTrialRewardsPantherCub()
    {
        var mission = Content.GetMission("tamer-trial");

        Assert.Equal(("幻獸森林的馴獸師 莉亞", 18, 8), (mission.Giver, mission.Level, mission.PlaceId!.Value));
        Assert.Equal(new KillTarget("jungle-panther", 10), mission.Kill);
        Assert.Equal(900, mission.Rewards.Money);
        Assert.Empty(mission.Rewards.Items);
        Assert.Equal(new PetReward("jungle-panther", 8), mission.Rewards.Pet);
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
            // 計畫 9 起最後一項是所屬大區的勳章（MonsterRosterTests.Roster_EliteMonstersDropTheirAreaMedal）
            Assert.Equal(new DropEntry("crystal-shard", 10), monster.Drops[0]);
            Assert.Equal(2, monster.Drops.Count);
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
