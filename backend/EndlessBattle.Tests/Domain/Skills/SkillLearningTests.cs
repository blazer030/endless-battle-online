using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Skills;

// 預設角色是戰士、2000 錢；burst 每級 money 500·lv²、proficiency 300·lv
public class SkillLearningTests
{
    private static readonly Character Warrior = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);
    private static readonly SkillDefinition Burst = TestSkills.Create("burst", "warrior", SkillType.Active);
    private static readonly SkillDefinition Combo = TestSkills.Create("combo", "thief", SkillType.Active);

    // 目前職業有沒有教這個技能（job-skills.json）與前置技能已學的等級，由呼叫端查好傳入
    private static readonly SkillTeaching Taught = new(TaughtByCurrentJob: true, PrerequisiteLevel: 0);
    private static readonly SkillTeaching NotTaught = new(TaughtByCurrentJob: false, PrerequisiteLevel: 0);

    [Fact]
    public void Learn_FirstLevel_DeductsMoneyAndSkillPoints()
    {
        var check = SkillLearning.Evaluate(Warrior, Burst, 0, 300, Taught);
        Assert.True(check.Allowed);
        Assert.Empty(check.Reasons);
        Assert.Equal(1, check.TargetLevel);

        var (character, proficiencyCost, learned) = SkillLearning.Apply(Warrior, Burst, 0, 300, Taught);

        Assert.Equal(1500, character.Money);
        Assert.Equal(300, proficiencyCost);
        Assert.Equal(new LearnedSkill("burst", 1), learned);
    }

    // 只會學 currentLevel+1：從 lv1 學到 lv2，收 lv2 的價錢
    [Fact]
    public void Learn_NextLevel_RequiresPreviousLevel()
    {
        var (character, proficiencyCost, learned) = SkillLearning.Apply(Warrior, Burst, 1, 600, Taught);

        Assert.Equal(0, character.Money);
        Assert.Equal(600, proficiencyCost);
        Assert.Equal(new LearnedSkill("burst", 2), learned);
        Assert.Equal(["notEnoughProficiency"], SkillLearning.Evaluate(Warrior, Burst, 0, 299, Taught).Reasons);
    }

    [Fact]
    public void Learn_DeductsNothing_WhenProficiencyInsufficient()
    {
        var check = SkillLearning.Evaluate(Warrior, Burst, 0, 299, Taught);

        Assert.False(check.Allowed);
        Assert.Equal(["notEnoughProficiency"], check.Reasons);
        var exception = Assert.Throws<SkillLearningException>(() => SkillLearning.Apply(Warrior, Burst, 0, 299, Taught));
        Assert.Equal(["notEnoughProficiency"], exception.Reasons);
    }

    [Fact]
    public void Learn_ReportsMoneyAndProficiencyTogether()
    {
        var check = SkillLearning.Evaluate(Warrior with { Money = 499 }, Burst, 0, 0, Taught);

        Assert.Equal(["notEnoughProficiency", "notEnoughMoney"], check.Reasons);
        Assert.Equal(1, check.TargetLevel);
    }

    [Fact]
    public void Learn_RejectsOtherJobSkill()
    {
        var check = SkillLearning.Evaluate(Warrior, Combo, 0, 300, NotTaught);

        Assert.False(check.Allowed);
        Assert.Equal(["wrongJob"], check.Reasons);
        Assert.Throws<SkillLearningException>(() => SkillLearning.Apply(Warrior, Combo, 0, 300, NotTaught));
    }

    [Fact]
    public void Learn_RejectsMaxLevel()
    {
        var rich = Warrior with { Money = 1_000_000 };

        var check = SkillLearning.Evaluate(rich, Burst, 5, 100_000, Taught);

        Assert.False(check.Allowed);
        Assert.Equal(["maxLevel"], check.Reasons);
        Assert.Equal(5, check.TargetLevel);
        Assert.Throws<SkillLearningException>(() => SkillLearning.Apply(rich, Burst, 5, 100_000, Taught));
    }

    [Fact]
    public void ListedSkill_CanBeLearned()
    {
        var check = SkillLearning.Evaluate(Warrior, Combo, 0, 300, Taught);

        Assert.True(check.Allowed);
    }

    [Fact]
    public void UnlistedSkill_IsRefusedAsWrongJob()
    {
        Assert.Equal(["wrongJob"], SkillLearning.Evaluate(Warrior, Burst, 0, 300, NotTaught).Reasons);
    }

    [Fact]
    public void MissingPrerequisite_IsRefused()
    {
        var advanced = Burst with { Id = "advanced-burst", Prerequisite = new SkillPrerequisite("burst", 3) };

        Assert.Equal(["prerequisite"], SkillLearning.Evaluate(Warrior, advanced, 0, 300, Taught with { PrerequisiteLevel = 2 }).Reasons);
        Assert.True(SkillLearning.Evaluate(Warrior, advanced, 0, 300, Taught with { PrerequisiteLevel = 3 }).Allowed);
    }
}
