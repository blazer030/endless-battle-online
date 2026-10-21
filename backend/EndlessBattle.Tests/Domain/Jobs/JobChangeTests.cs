using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Jobs;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Jobs;

public class JobChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JobGrowth Growth = new(new(0, 1), new(0, 1), new(0, 1), new(0, 1), new(0, 1), new(0, 1), new(0, 1));

    private static readonly JobDefinition Thief = new("thief", "盜賊", 1, null, 1, 0, [], Growth);
    private static readonly JobDefinition Berserker = new("berserker", "狂戰士", 2, "warrior", 30, 3500, [new StatRequirement("str", 60)], Growth);

    private static Character Warrior(int level = 30, int str = 60) =>
        TestCharacters.Default(1, "測試者", "test-stage-1", Now) with { Level = level, Base = new BaseStats(str, 12, 12, 10, 12, 10, 10) };

    private static IReadOnlyList<JobProficiency> Ch(long proficiency) => [new JobProficiency("warrior", proficiency, 0)];

    [Fact]
    public void Evaluate_TierOne_AlwaysEligible()
    {
        var check = JobChange.Evaluate(Warrior(level: 1), Thief, []);

        Assert.True(check.Eligible);
        Assert.Empty(check.Reasons);
    }

    [Theory]
    [InlineData(29, 3500, 60, new[] { "levelTooLow" })]
    [InlineData(30, 3499, 60, new[] { "parentProficiencyTooLow" })]
    [InlineData(30, 3500, 59, new[] { "statTooLow:str" })]
    [InlineData(1, 0, 1, new[] { "levelTooLow", "parentProficiencyTooLow", "statTooLow:str" })]
    public void Evaluate_RequiresLevelParentProficiencyAndStats(int level, long proficiency, int str, string[] expected)
    {
        var check = JobChange.Evaluate(Warrior(level, str), Berserker, Ch(proficiency));

        Assert.False(check.Eligible);
        Assert.Equal(expected, check.Reasons);
    }

    [Fact]
    public void Evaluate_MissingProficiencyRow_CountsAsZero()
    {
        var check = JobChange.Evaluate(Warrior(), Berserker, []);

        Assert.Equal(["parentProficiencyTooLow"], check.Reasons);
    }

    [Fact]
    public void Evaluate_SameJob_Rejected()
    {
        var check = JobChange.Evaluate(Warrior(), new JobDefinition("warrior", "戰士", 1, null, 1, 0, [], Growth), []);

        Assert.False(check.Eligible);
        Assert.Equal(["sameJob"], check.Reasons);
    }

    [Fact]
    public void Evaluate_EligibleWhenAllMet()
    {
        var check = JobChange.Evaluate(Warrior(), Berserker, Ch(3500));

        Assert.True(check.Eligible);
        Assert.Empty(check.Reasons);
    }

    [Fact]
    public void Apply_ChangesOnlyJobId()
    {
        var character = Warrior();

        Assert.Equal(character with { JobId = "thief" }, JobChange.Apply(character, Thief));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    [InlineData(3499, 99)]
    [InlineData(3500, 100)]
    [InlineData(7000, 100)]
    public void Percent_CapsAtHundred(long proficiency, int expected) =>
        Assert.Equal(expected, JobProficiencyDisplay.Percent(proficiency));
}
