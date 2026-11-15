using EndlessBattle.Api.Endpoints;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// 規格 missions「Board details for the mission board screen」：用正式內容轉成回應
public class MissionResponseTests
{
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 3, 0, 0, TimeSpan.Zero);

    private static MissionEntry Entry(MissionDefinition mission, MissionState? state)
    {
        var record = state is { } current ? new MissionRecord(mission.Id, current, [], null, Now.AddHours(-2), current == MissionState.Completed ? Now : null) : null;
        return new MissionEntry(mission, DailyMissions.Resolve(mission, 1, null), record, false, null);
    }

    private static MissionResponse Respond(MissionDefinition mission, MissionState? state = null, params LearnedSkill[] learned)
    {
        var entry = Entry(mission, state);
        IReadOnlyList<MissionEntry> available = state is null ? [entry] : [];
        IReadOnlyList<MissionEntry> active = state == MissionState.Active ? [entry] : [];
        IReadOnlyList<MissionEntry> completed = state == MissionState.Completed ? [entry] : [];
        var board = new MissionBoardView(available, active, completed, Now.AddHours(22), new Dictionary<string, int>(), null, learned);
        var response = MissionEndpoints.ToResponse(board, Content);
        return Assert.Single([.. response.Available, .. response.Active, .. response.Completed]);
    }

    private static MissionDefinition Mission(string id) => Content.GetMission(id);

    [Fact]
    public void CompletionMessage_OnlyForCompletedMissionsWithAMessage()
    {
        Assert.StartsWith("感謝您的努力", Respond(Mission("mission-1"), MissionState.Completed).CompletionMessage);
        Assert.Null(Respond(Mission("mission-3"), MissionState.Completed).CompletionMessage);
        Assert.Null(Respond(Mission("mission-1")).CompletionMessage);
        Assert.Null(Respond(Mission("mission-1"), MissionState.Active).CompletionMessage);
    }

    [Fact]
    public void ItemCategories_ForRewardsAndDeliveries()
    {
        var response = Respond(Mission("mission-1"));

        Assert.Equal(("item-342", ItemCategory.Material), (Assert.Single(response.Rewards.Items).ItemId, response.Rewards.Items[0].Category));
        Assert.Equal(("item-331", ItemCategory.Material), (Assert.Single(response.Deliver).ItemId, response.Deliver[0].Category));
    }

    [Fact]
    public void RequiredSkill_CarriesTheLearnedLevel()
    {
        var mission = Mission("mission-318");

        Assert.Equal((2, (int?)null, false), Shape(Respond(mission, MissionState.Active)));
        Assert.Equal((2, (int?)1, false), Shape(Respond(mission, MissionState.Active, new LearnedSkill("skill-414", 1))));
        Assert.Equal((2, (int?)2, true), Shape(Respond(mission, MissionState.Active, new LearnedSkill("skill-414", 2))));

        static (int, int?, bool) Shape(MissionResponse response) =>
            (response.RequiredSkill!.Level, response.RequiredSkill.LearnedLevel, response.RequiredSkill.Met);
    }

    [Fact]
    public void Requirements_CarryNames()
    {
        var limited = Mission("mission-1") with { Requirements = new MissionRequirements("job-6", 2) };

        var requirements = Respond(limited).Requirements;

        Assert.Equal(("戰士", "女", (string?)null), (requirements.JobName, requirements.SexName, requirements.BirthName));
        Assert.Equal(((string?)null, (string?)null, (string?)null), (Respond(Mission("mission-1")).Requirements.JobName, Respond(Mission("mission-1")).Requirements.SexName, Respond(Mission("mission-1")).Requirements.BirthName));
    }

    [Fact]
    public void FirstDailyLevel_FromTheRealContent()
    {
        var hero = TestCharacters.Default(1, "測試者", 1, 1, Now) with { JobId = "job-6" };
        var board = MissionBoard.Build(hero, Content.AllMissions, [], new Dictionary<string, int>(), Now);

        Assert.Equal(50, MissionEndpoints.ToResponse(board, Content).FirstDailyLevel);
    }
}
