using EndlessBattle.Domain.Battle;
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
        SkillLoadout.Validate(slots, Learned, id => Skills[id]);

    // 主動欄可放其他職業的主動技能（轉職後保留）
    [Fact]
    public void Loadout_AcceptsThreeActiveTwoPassive()
    {
        Assert.Empty(Validate(
            new SkillSlot(1, "burst"),
            new SkillSlot(2, "combo"),
            new SkillSlot(3, "lullaby"),
            new SkillSlot(4, "iron-skin"),
            new SkillSlot(5, "guard")));
        Assert.Empty(Validate());
    }

    [Fact]
    public void Loadout_RejectsPassiveInActiveSlot() =>
        Assert.Equal(["passiveInActiveSlot"], Validate(new SkillSlot(1, "iron-skin")));

    [Fact]
    public void Loadout_RejectsActiveInPassiveSlot() =>
        Assert.Equal(["activeInPassiveSlot"], Validate(new SkillSlot(4, "burst")));

    // 被動欄也不限職業：任何職業學過的被動都能裝
    [Fact]
    public void Loadout_AcceptsPassiveOfOtherJob() =>
        Assert.Empty(Validate(new SkillSlot(5, "bloodthirst")));

    [Fact]
    public void Loadout_RejectsDuplicateSkill() =>
        Assert.Equal(["duplicateSkill"], Validate(new SkillSlot(1, "burst"), new SkillSlot(2, "burst")));

    [Fact]
    public void Loadout_RejectsDuplicateSlot() =>
        Assert.Equal(["duplicateSlot"], Validate(new SkillSlot(1, "burst"), new SkillSlot(1, "combo")));

    [Fact]
    public void Loadout_RejectsNotLearned()
    {
        Assert.Equal(["notLearned"], SkillLoadout.Validate([new SkillSlot(1, "burst")], [], id => Skills[id]));
        Assert.Equal(["notLearned"], Validate(new SkillSlot(1, "no-such-skill")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Loadout_RejectsSlotOutOfRange(int slot) =>
        Assert.Equal(["slotOutOfRange"], Validate(new SkillSlot(slot, "burst")));

    [Fact]
    public void Loadout_ReportsEveryDistinctReasonInOrder() =>
        Assert.Equal(
            ["passiveInActiveSlot", "activeInPassiveSlot"],
            Validate(new SkillSlot(1, "iron-skin"), new SkillSlot(2, "guard"), new SkillSlot(4, "burst"), new SkillSlot(5, "combo")));

    private static IReadOnlyList<(string SkillId, int Level, int Slot)> ForBattle(IReadOnlyList<LearnedSkill> learned, params SkillSlot[] slots) =>
        SkillLoadout.ForBattle(slots, learned, id => Skills.GetValueOrDefault(id))
            .Select(equipped => (equipped.Skill.Id, equipped.Level, equipped.Slot))
            .ToList();

    // 輸入順序與字母序都和欄位序相反，輸出仍依欄位 1→5；等級取技能書
    [Fact]
    public void ForBattle_OrdersBySlotWithLearnedLevel() =>
        Assert.Equal(
            [("lullaby", 3, 1), ("burst", 2, 2), ("iron-skin", 1, 4)],
            ForBattle(
                [new LearnedSkill("burst", 2), new LearnedSkill("iron-skin", 1), new LearnedSkill("lullaby", 3)],
                new SkillSlot(4, "iron-skin"), new SkillSlot(2, "burst"), new SkillSlot(1, "lullaby")));

    // 內容把技能縮成 5 級後，技能書裡的 7、9 級以 5 級施放
    [Fact]
    public void ForBattle_ClampsLevelToContentMaximum() =>
        Assert.Equal(
            [("burst", 5, 1), ("iron-skin", 5, 4)],
            ForBattle([new LearnedSkill("burst", 7), new LearnedSkill("iron-skin", 9)],
                new SkillSlot(1, "burst"), new SkillSlot(4, "iron-skin")));

    [Fact]
    public void ForBattle_SkipsSkillsNotInBook() =>
        Assert.Equal(
            [("burst", 1, 1)],
            ForBattle([new LearnedSkill("burst", 1)], new SkillSlot(1, "burst"), new SkillSlot(2, "combo")));

    // 被動欄不限職業：轉職後前一個職業的被動照樣生效
    [Fact]
    public void ForBattle_KeepsPassiveOfOtherJob() =>
        Assert.Equal(
            [("iron-skin", 1, 4), ("bloodthirst", 1, 5)],
            ForBattle(Learned, new SkillSlot(4, "iron-skin"), new SkillSlot(5, "bloodthirst")));

    // 主動欄照 WOG 不限職業
    [Fact]
    public void ForBattle_KeepsActiveOfOtherJob() =>
        Assert.Equal(
            [("burst", 1, 1), ("combo", 1, 2)],
            ForBattle(Learned, new SkillSlot(1, "burst"), new SkillSlot(2, "combo")));

    [Fact]
    public void ForBattle_SkipsSkillsRemovedFromContent() =>
        Assert.Equal(
            [("burst", 1, 2)],
            ForBattle([new LearnedSkill("retired", 1), new LearnedSkill("burst", 1)],
                new SkillSlot(1, "retired"), new SkillSlot(2, "burst")));
}
