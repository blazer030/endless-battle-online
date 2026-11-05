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

    [Fact]
    public void Summary_DescribesChanceDamageAndElementEffects()
    {
        var level = Level(
            [new SkillCondition(SkillConditionKind.Chance, 40)],
            [],
            [
                new BonusDamageEffect(SkillAttackKind.Physical, [new SkillScaling(SkillScalingBasis.Agility, 0.15)], Rounds: 0) { Chance = 20 },
                new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 0.1)], Rounds: 3),
                new AttackElementEffect(3, Rounds: 5),
            ]);

        Assert.Equal("機率 40%｜追加物理傷害 敏捷×15%（機率 20%）、每回合造成 智力×10% 傷害，3 回合、攻擊屬性轉為火，5 回合", SkillSummary.Of(level));
    }
}
