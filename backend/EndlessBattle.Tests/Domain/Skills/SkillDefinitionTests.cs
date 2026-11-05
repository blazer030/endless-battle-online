using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Domain.Skills;

public class SkillDefinitionTests
{
    private static SkillLevel Level(int level, long money) => new(level, money, 300L * level, 0,
        [new SkillCondition(SkillConditionKind.Chance, 40)], [], [new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, level, null, null, 3)]);

    private static readonly SkillDefinition Skill = new("burst", "爆氣", SkillType.Active, SkillTiming.BeforeAttack, "說明", [Level(1, 500), Level(2, 2000)]);

    [Fact]
    public void LevelAt_ReturnsMatchingLevel()
    {
        Assert.Equal(2, Skill.MaxLevel);
        Assert.Equal(2000, Skill.LevelAt(2).Money);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void LevelAt_ThrowsOutsideRange(int level) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Skill.LevelAt(level));
}
