using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Application;

// 效果文字由每級的條件、消耗、效果產生（spec skill-effects「Effect text is generated from the parts」）
public class SkillSummaryTests
{
    private static SkillLevel Level(IReadOnlyList<SkillCondition> conditions, IReadOnlyList<SkillCost> costs, IReadOnlyList<SkillEffect> effects,
        int cooldown = 0) => new(1, 500, 300, cooldown, conditions, costs, effects);

    [Fact]
    public void Summary_ReflectsLevelValues()
    {
        var burst = Level(
            [new SkillCondition(SkillConditionKind.Chance, 40)],
            [new SkillCost(SkillCostKind.SkillPoints, 2)],
            [new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, 1, null, null, Rounds: 3)]);

        Assert.Equal("機率 40%｜消耗 SP 2｜自己物攻 +1%，3 回合", SkillSummary.Of(burst));
    }
}
