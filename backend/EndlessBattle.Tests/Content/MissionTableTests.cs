using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 匯入的原版任務與任務怪；每日任務的目標池與報酬池照抄原版 mission/wog_mission_<id>.php
public class MissionTableTests
{
    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static IEnumerable<MissionDefinition> Regular => Content.AllMissions.Where(mission => !mission.Daily);

    private static IEnumerable<MissionDefinition> Dailies => Content.AllMissions.Where(mission => mission.Daily);

    private static readonly string[] MedalOrder = ["item-1304", "item-1305", "item-1306", "item-2222", "item-1307"];

    // 懸賞類每日任務取這些地區 meet 0 的一般怪（原版 271～274）；原版報酬是交易所物資，沿用計畫 8 的金錢
    public static TheoryData<string, int[], long[]> BountyPools => new()
    {
        { "mission-271", [3, 4], [400, 500, 600] },
        { "mission-272", [5, 6], [2400, 3000, 3600] },
        { "mission-273", [7, 13], [1000, 1250, 1500] },
        { "mission-274", [8, 16], [800, 1000, 1200] },
    };

    [Fact]
    public void MissionTable_RegularMissionsAreOriginal()
    {
        Assert.Equal(240, Regular.Count());
        Assert.All(Regular, mission =>
        {
            Assert.False(CustomContent.IsCustom(mission.Id));
            Assert.Equal(MissionDefinition.ExpPerLevel * mission.Level, mission.Exp);
            if (mission.Kill is { } kill)
            {
                Assert.Equal(Content.GetMonster(kill.MonsterId).PlaceId, mission.PlaceId);
            }
        });
    }

    [Fact]
    public void MissionTable_HasEightDailies()
    {
        Assert.Equal(
            ["mission-222", "mission-223", "mission-224", "mission-225", "mission-271", "mission-272", "mission-273", "mission-274"],
            Dailies.Select(mission => mission.Id));

        var patrol = Content.GetMission("mission-222");
        Assert.Equal(("全國冒險者工會", "[每日]治安維持任務", 50), (patrol.Giver, patrol.Title, patrol.Level));
        Assert.Equal(
            [
                new KillTarget("monster-1", 6), new KillTarget("monster-49", 6), new KillTarget("monster-19", 6),
                new KillTarget("monster-104", 6), new KillTarget("monster-22", 5), new KillTarget("monster-26", 5),
                new KillTarget("monster-36", 4), new KillTarget("monster-57", 4),
            ],
            patrol.DailyKills);
        Assert.Equal(8, Content.GetMission("mission-224").DailyKills.Count);
        Assert.All(Dailies, mission => Assert.True(mission.DailyKills.Count > 0 ^ mission.DailyPets.Count > 0));
    }

    [Fact]
    public void MissionTable_HuntDailiesPayWogMedals()
    {
        Assert.Equivalent(
            MedalOrder.Select(medal => new MissionReward(0, [new ItemQuantity(medal, 1)])).ToArray(),
            Content.GetMission("mission-222").DailyRewards,
            strict: true);
        Assert.Equivalent(
            MedalOrder.Select(medal => new MissionReward(0, [new ItemQuantity("item-260", 3), new ItemQuantity(medal, 2)])).ToArray(),
            Content.GetMission("mission-224").DailyRewards,
            strict: true);
    }

    // 原版 223／225：交出攜帶中的寵物，目標池是地區 1～4／5～8 meet 0 的一般怪；報酬的寶石箱沒有匯入，只剩勳章
    [Theory]
    [InlineData("mission-223", 1, 4)]
    [InlineData("mission-225", 5, 8)]
    public void MissionTable_PetDailiesUseMeetZeroMonsters(string missionId, int firstPlace, int lastPlace)
    {
        var mission = Content.GetMission(missionId);

        var expected = Content.AllMonsters.Where(monster => !CustomContent.IsCustom(monster.Id))
            .Where(monster => monster.PlaceId >= firstPlace && monster.PlaceId <= lastPlace && monster.Meet == 0 && monster.MissionId is null)
            .Select(monster => monster.Id)
            .ToList();
        Assert.NotEmpty(expected);
        Assert.Equal(expected, mission.DailyPets);
        Assert.Empty(mission.DailyKills);
        Assert.Equivalent(
            MedalOrder.Select(medal => new MissionReward(0, [new ItemQuantity(medal, 1)])).ToArray(),
            mission.DailyRewards,
            strict: true);
    }

    [Fact]
    public void MissionTable_PetRewardKeepsOriginalLevel()
    {
        var mission = Content.GetMission("mission-168");

        Assert.Equal(("詩人 艾亞斯", "遺忘之地", 660), (mission.Giver, mission.Title, mission.Level));
        Assert.Equal(new PetReward("monster-177", 250), mission.Rewards.Pet);
    }

    [Theory]
    [MemberData(nameof(BountyPools))]
    public void MissionTable_BountyPoolsAreMeetZeroTimesFiveToSeven(string missionId, int[] placeIds, long[] money)
    {
        var mission = Content.GetMission(missionId);
        var expectedKills = Content.AllMonsters.Where(monster => !CustomContent.IsCustom(monster.Id))
            .Where(monster => placeIds.Contains(monster.PlaceId) && monster.Meet == 0 && monster.MissionId is null)
            .SelectMany(monster => new[] { 5, 6, 7 }.Select(count => new KillTarget(monster.Id, count)))
            .ToList();

        Assert.NotEmpty(expectedKills);
        Assert.Equal(expectedKills, mission.DailyKills);
        Assert.Equal(money, mission.DailyRewards.Select(reward => reward.Money));
        Assert.All(mission.DailyRewards, reward => Assert.Empty(reward.Items));
    }

    [Fact]
    public void MissionTable_MissionMonstersBelongToOneMission()
    {
        var missionMonsters = Content.AllMonsters.Where(monster => monster.MissionId is not null).ToList();

        Assert.Equal(54, missionMonsters.Count);
        Assert.All(missionMonsters, monster =>
        {
            var owner = Assert.Single(Content.AllMissions, mission => mission.Kill?.MonsterId == monster.Id);
            Assert.Equal((owner.Id, owner.PlaceId), (monster.MissionId, monster.PlaceId));
            Assert.DoesNotContain(Dailies.SelectMany(mission => mission.DailyKills), kill => kill.MonsterId == monster.Id);
        });
        Assert.Equal(("mission-2", 4, 40), (Content.GetMonster("monster-212").MissionId, Content.GetMonster("monster-212").PlaceId, Content.GetMonster("monster-212").Level));
    }
}
