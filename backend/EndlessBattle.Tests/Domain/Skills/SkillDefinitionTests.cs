using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Domain.Skills;

public class SkillDefinitionTests
{
    private static readonly SkillDefinition Skill = new(
        "burst", "爆氣", "warrior", SkillType.Active, SkillTiming.BeforeAttack, SkillEffectKind.AtMultiplier, "說明",
        [new SkillLevel(1, 5, 3, 0, 500, 300), new SkillLevel(2, 8, 3, 0, 2000, 600)]);

    [Fact]
    public void LevelAt_ReturnsMatchingLevel()
    {
        Assert.Equal(2, Skill.MaxLevel);
        Assert.Equal(8, Skill.LevelAt(2).Sp);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void LevelAt_ThrowsOutsideRange(int level) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Skill.LevelAt(level));
}
