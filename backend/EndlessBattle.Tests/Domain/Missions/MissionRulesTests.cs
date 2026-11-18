using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Missions;

public class MissionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static Character Hero(int level = 1) => TestCharacters.Default(1, "測試者", 1, 1, Now) with { Level = level };

    private static MissionDefinition Mission(string id, int level = 1, MissionRequirements? requirements = null) => new(
        id, "村長", "標題", "說明", "謝謝", level, 1, false, [new KillTarget("test-sprite", 3)], [],
        requirements ?? new MissionRequirements(), new MissionReward(50, []), [], []);

    private static MissionRecord Active(string id) => new(id, MissionState.Active, [0], null, Now, null);

    private static MissionRecord Completed(string id) => new(id, MissionState.Completed, [3], null, Now, Now);

    [Fact]
    public void CanAccept_AllowsWhenEveryRequirementMet()
    {
        var mission = Mission("hunt", requirements: new MissionRequirements("warrior", 1, 1, "first", "other"));

        Assert.Empty(MissionRules.CanAccept(Hero(), mission, [Completed("first")]));
    }

    [Fact]
    public void CanAccept_UnknownMission()
    {
        Assert.Equal(["MISSION_UNKNOWN"], MissionRules.CanAccept(Hero(), null, []));
    }

    [Fact]
    public void CanAccept_RejectsFourthActive()
    {
        var mission = Mission("hunt");

        Assert.Equal(["TOO_MANY"], MissionRules.CanAccept(Hero(), mission, [Active("a"), Active("b"), Active("c")]));
        Assert.Empty(MissionRules.CanAccept(Hero(), mission, [Completed("a"), Completed("b"), Completed("c")]));
    }

    [Fact]
    public void CanAccept_LevelIsTheMissionLevel()
    {
        var mission = Mission("hunt", level: 15);

        Assert.Equal(["LEVEL_TOO_LOW"], MissionRules.CanAccept(Hero(14), mission, []));
        Assert.Empty(MissionRules.CanAccept(Hero(15), mission, []));
    }

    [Theory]
    [InlineData("sorcerer", null, null, "JOB_MISMATCH")]
    [InlineData(null, 2, null, "SEX_MISMATCH")]
    [InlineData(null, null, 2, "BIRTH_MISMATCH")]
    public void CanAccept_ChecksJobSexBirth(string? jobId, int? sex, int? birth, string expected)
    {
        var mission = Mission("hunt", requirements: new MissionRequirements(jobId, sex, birth));

        Assert.Equal([expected], MissionRules.CanAccept(Hero(), mission, []));
    }

    [Fact]
    public void CanAccept_PrerequisiteMustBeCompleted()
    {
        var mission = Mission("hunt", requirements: new MissionRequirements(PrerequisiteId: "first"));

        Assert.Equal(["PREREQUISITE_MISSING"], MissionRules.CanAccept(Hero(), mission, [Active("first")]));
        Assert.Equal(["PREREQUISITE_MISSING"], MissionRules.CanAccept(Hero(), mission, []));
        Assert.Empty(MissionRules.CanAccept(Hero(), mission, [Completed("first")]));
    }

    [Fact]
    public void CanAccept_ConflictBlocksAnyState()
    {
        var mission = Mission("hunt", requirements: new MissionRequirements(ConflictsWith: "rival"));

        Assert.Equal(["MISSION_CONFLICT"], MissionRules.CanAccept(Hero(), mission, [Active("rival")]));
        Assert.Equal(["MISSION_CONFLICT"], MissionRules.CanAccept(Hero(), mission, [Completed("rival")]));
    }

    [Fact]
    public void CanAccept_ReportsActiveAndCompletedSeparately()
    {
        var mission = Mission("hunt");

        Assert.Equal(["ALREADY_ACTIVE"], MissionRules.CanAccept(Hero(), mission, [Active("hunt")]));
        Assert.Equal(["ALREADY_COMPLETED"], MissionRules.CanAccept(Hero(), mission, [Completed("hunt")]));
    }

    [Fact]
    public void CanAccept_ListsEveryReasonInOrder()
    {
        var mission = Mission("hunt", level: 10, requirements: new MissionRequirements("sorcerer", 2, PrerequisiteId: "first"));

        Assert.Equal(
            ["LEVEL_TOO_LOW", "JOB_MISMATCH", "SEX_MISMATCH", "PREREQUISITE_MISSING"],
            MissionRules.CanAccept(Hero(1), mission, []));
    }

    [Fact]
    public void CanAccept_ListsEveryCoOccurringReasonInOrder()
    {
        var mission = Mission("hunt", level: 10, requirements: new MissionRequirements("sorcerer", 2, 2, "first", "rival"));

        Assert.Equal(
            ["TOO_MANY", "LEVEL_TOO_LOW", "JOB_MISMATCH", "SEX_MISMATCH", "BIRTH_MISMATCH", "PREREQUISITE_MISSING", "MISSION_CONFLICT"],
            MissionRules.CanAccept(Hero(1), mission, [Active("rival"), Active("b"), Active("c")]));
    }

    // 規格 missions「Accept reasons in order」：Lv5、手邊 3 張，接 Lv10 限術士且前置未完成
    [Fact]
    public void CanAccept_ExampleReasons()
    {
        var mission = Mission("hunt", level: 10, requirements: new MissionRequirements("sorcerer", PrerequisiteId: "first"));

        Assert.Equal(
            ["TOO_MANY", "LEVEL_TOO_LOW", "JOB_MISMATCH", "PREREQUISITE_MISSING"],
            MissionRules.CanAccept(Hero(5), mission, [Active("a"), Active("b"), Active("c")]));
    }
}
