using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Missions;

public class MissionBoardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 22);

    private static readonly Character Hero = TestCharacters.Default(1, "測試者", 1, 1, Now) with { Id = 7, Level = 10 };

    private static MissionDefinition General(string id, int level = 1, KillTarget? kill = null, IReadOnlyList<ItemQuantity>? deliver = null) => new(
        id, "村長", "標題", "說明", "謝謝", level, 1, false, kill ?? new KillTarget("test-sprite", 3), deliver ?? [],
        new MissionRequirements(), new MissionReward(50, []), [], []);

    private static MissionDefinition DailyMission(string id) => new(
        id, "公會", "每日", "說明", "明天再來", 1, null, true, null, [], new MissionRequirements(), new MissionReward(0, []),
        [new KillTarget("test-sprite", 2), new KillTarget("test-wolf", 2)], [new MissionReward(100, [])]);

    private static MissionRecord Active(string id, int progress = 0, DateOnly? period = null, int acceptedHour = 2) =>
        new(id, MissionState.Active, progress, period, new DateTimeOffset(2026, 10, 22, acceptedHour, 0, 0, TimeSpan.Zero), null);

    private static MissionRecord Completed(string id, int completedHour, DateOnly? period = null) =>
        new(id, MissionState.Completed, 3, period, Now.AddHours(-5), new DateTimeOffset(2026, 10, 22, completedHour, 0, 0, TimeSpan.Zero));

    private static readonly Dictionary<string, int> NothingOwned = [];

    [Fact]
    public void Board_ListsTooManyAsAvailable()
    {
        IReadOnlyList<MissionDefinition> missions = [General("a"), General("b"), General("c"), General("d"), General("high", level: 11)];

        var board = MissionBoard.Build(Hero, missions, [Active("a"), Active("b"), Active("c")], NothingOwned, Now);

        Assert.Equal(["d"], board.Available.Select(entry => entry.Definition.Id));
        Assert.Equal(["a", "b", "c"], board.Active.Select(entry => entry.Definition.Id));
        Assert.Equal(new DateTimeOffset(2026, 10, 23, 1, 0, 0, TimeSpan.Zero), board.NextDailyReset);
    }

    [Fact]
    public void Board_HidesExpiredDailyAndUnknownMissions()
    {
        IReadOnlyList<MissionDefinition> missions = [DailyMission("daily"), General("a")];
        IReadOnlyList<MissionRecord> records =
        [
            Active("daily", period: Today.AddDays(-1)),
            Active("removed"),
            Completed("gone", 1),
            Active("a"),
        ];

        var board = MissionBoard.Build(Hero, missions, records, NothingOwned, Now);

        Assert.Equal(["daily"], board.Available.Select(entry => entry.Definition.Id));
        Assert.Equal(["a"], board.Active.Select(entry => entry.Definition.Id));
        Assert.Empty(board.Completed);
        Assert.Equal(["a"], MissionBoard.Current(records, missions, Now).Select(record => record.MissionId));
    }

    [Fact]
    public void Board_DailyFirstThenByLevel()
    {
        IReadOnlyList<MissionDefinition> missions =
            [General("five", level: 5), DailyMission("daily-one"), General("two", level: 2), General("also-five", level: 5), DailyMission("daily-two")];

        var board = MissionBoard.Build(Hero, missions, [], NothingOwned, Now);

        Assert.Equal(["daily-one", "daily-two", "two", "five", "also-five"], board.Available.Select(entry => entry.Definition.Id));
        var daily = board.Available[0];
        Assert.Equivalent(DailyMissions.Resolve(daily.Definition, Hero.Id, Today), daily.Objective);
        Assert.Equal(DailyMissions.NextReset(Now), daily.ExpiresAt);
        Assert.Null(board.Available[2].ExpiresAt);
    }

    [Fact]
    public void Board_ActiveByAcceptedAtAndCompletedNewestFirst()
    {
        IReadOnlyList<MissionDefinition> missions = [DailyMission("daily"), General("a"), General("b"), General("c"), General("d")];
        IReadOnlyList<MissionRecord> records =
        [
            Active("b", acceptedHour: 2),
            Active("daily", period: Today, acceptedHour: 1),
            Completed("c", completedHour: 1),
            Completed("d", completedHour: 2),
        ];

        var board = MissionBoard.Build(Hero, missions, records, NothingOwned, Now);

        Assert.Equal(["daily", "b"], board.Active.Select(entry => entry.Definition.Id));
        Assert.Equal(["d", "c"], board.Completed.Select(entry => entry.Definition.Id));
        Assert.Equal(DailyMissions.PeriodStart(Today.AddDays(1)), board.Active[0].ExpiresAt);
        Assert.Equivalent(DailyMissions.Resolve(missions[0], Hero.Id, Today), board.Active[0].Objective);
        Assert.Same(records[0], board.Active[1].Record);
    }

    [Fact]
    public void Board_ReadyNeedsKillsAndItems()
    {
        IReadOnlyList<MissionDefinition> missions =
        [
            General("hunt"),
            General("delivery", deliver: [new ItemQuantity("test-ore", 3)]),
        ];
        IReadOnlyList<MissionRecord> records = [Active("hunt", progress: 3), Active("delivery", progress: 3)];

        var shortOfOre = MissionBoard.Build(Hero, missions, records, new Dictionary<string, int> { ["test-ore"] = 2 }, Now);
        var enough = MissionBoard.Build(Hero, missions, records, new Dictionary<string, int> { ["test-ore"] = 3 }, Now);
        var unfinished = MissionBoard.Build(Hero, missions, [Active("hunt", progress: 2)], NothingOwned, Now);

        Assert.Equal([true, false], shortOfOre.Active.Select(entry => entry.Ready));
        Assert.Equal([true, true], enough.Active.Select(entry => entry.Ready));
        Assert.False(unfinished.Active.Single().Ready);
    }
}
