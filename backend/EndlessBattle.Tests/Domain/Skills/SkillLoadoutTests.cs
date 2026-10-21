using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Skills;

public class SkillLoadoutTests
{
    private static readonly Dictionary<string, SkillDefinition> Skills = new[]
    {
        TestSkills.Create("burst", "warrior", SkillType.Active),
        TestSkills.Create("iron-skin", "warrior", SkillType.Passive),
        TestSkills.Create("combo", "thief", SkillType.Active),
        TestSkills.Create("lullaby", "bard", SkillType.Active),
        TestSkills.Create("bloodthirst", "martialist", SkillType.Passive),
        TestSkills.Create("guard", "warrior", SkillType.Passive),
    }.ToDictionary(skill => skill.Id);

    private static readonly IReadOnlyList<LearnedSkill> Learned = Skills.Keys.Select(id => new LearnedSkill(id, 1)).ToList();

    private static IReadOnlyList<string> Validate(params SkillSlot[] slots) =>
        SkillLoadout.Validate(slots, "warrior", Learned, id => Skills[id]);

    // 主動欄可放其他職業的主動技能（轉職後保留）；被動欄的 chance 由服務層填入，這裡不檢查
    [Fact]
    public void Loadout_AcceptsThreeActiveTwoPassive()
    {
        Assert.Empty(Validate(
            new SkillSlot(1, "burst", 50),
            new SkillSlot(2, "combo", 10),
            new SkillSlot(3, "lullaby", 100),
            new SkillSlot(4, "iron-skin", 0),
            new SkillSlot(5, "guard", 99)));
        Assert.Empty(Validate());
    }

    [Fact]
    public void Loadout_RejectsPassiveInActiveSlot() =>
        Assert.Equal(["passiveInActiveSlot"], Validate(new SkillSlot(1, "iron-skin", 50)));

    [Fact]
    public void Loadout_RejectsActiveInPassiveSlot() =>
        Assert.Equal(["activeInPassiveSlot"], Validate(new SkillSlot(4, "burst", 50)));

    [Fact]
    public void Loadout_RejectsPassiveOfOtherJob() =>
        Assert.Equal(["passiveNotCurrentJob"], Validate(new SkillSlot(5, "bloodthirst", 30)));

    [Fact]
    public void Loadout_RejectsDuplicateSkill() =>
        Assert.Equal(["duplicateSkill"], Validate(new SkillSlot(1, "burst", 50), new SkillSlot(2, "burst", 50)));

    [Fact]
    public void Loadout_RejectsDuplicateSlot() =>
        Assert.Equal(["duplicateSlot"], Validate(new SkillSlot(1, "burst", 50), new SkillSlot(1, "combo", 50)));

    [Fact]
    public void Loadout_RejectsNotLearned()
    {
        Assert.Equal(["notLearned"], SkillLoadout.Validate([new SkillSlot(1, "burst", 50)], "warrior", [], id => Skills[id]));
        Assert.Equal(["notLearned"], Validate(new SkillSlot(1, "no-such-skill", 50)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Loadout_RejectsSlotOutOfRange(int slot) =>
        Assert.Equal(["slotOutOfRange"], Validate(new SkillSlot(slot, "burst", 50)));

    [Theory]
    [InlineData(55)]
    [InlineData(0)]
    [InlineData(110)]
    [InlineData(-10)]
    public void Loadout_RejectsChanceOffStep(int chance) =>
        Assert.Equal(["chanceOutOfRange"], Validate(new SkillSlot(1, "burst", chance)));

    [Fact]
    public void Loadout_ReportsEveryDistinctReasonInOrder() =>
        Assert.Equal(
            ["passiveInActiveSlot", "chanceOutOfRange", "activeInPassiveSlot"],
            Validate(new SkillSlot(1, "iron-skin", 55), new SkillSlot(2, "guard", 50), new SkillSlot(4, "burst", 50)));
}
