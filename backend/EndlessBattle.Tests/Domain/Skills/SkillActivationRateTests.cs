using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Skills;

// 怪物與武器的發動率取代技能的 Chance 條件（spec monster-skills）；rand(1,100) = ⌊v×100⌋ + 1
public class SkillActivationRateTests
{
    private static readonly SkillEffect Effect = new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, 5, null, null, Rounds: 0);

    private static CombatantState Combatant(int hp = 1000) =>
        new(hp, HpMax: 1000, Sp: 100, SpMax: 100, At: 100, Mat: 0, Df: 50, Mdf: 50, Agi: 20, Au: 10, Element: 0, Str: 100, Smart: 20);

    private static SkillDefinition Skill(IReadOnlyList<SkillCondition> conditions, params SkillEffect[] effects) =>
        new("skill", "技能", SkillType.Active, SkillTiming.BeforeAttack, "", [new SkillLevel(1, 0, 0, 0, conditions, [], effects)]);

    private static SkillCaster Caster(SkillDefinition skill, CombatantState combatant) => new(combatant, [new EquippedSkill(skill, 1, 1)]);

    [Theory]
    [InlineData(0.09, 1)]
    [InlineData(0.10, 0)]
    public void WithActivationRate_ReplacesChance(double roll, int expectedCasts)
    {
        // 爆氣第 2 級 Chance 50，怪物發動率 10：擲 10 發動、擲 11 不發動
        var skill = Skill([new SkillCondition(SkillConditionKind.Chance, 50)], Effect).WithActivationRate(10);

        var casts = Caster(skill, Combatant()).CastBeforeAttack(1000, 100, new ScriptedRandom(roll));

        Assert.Equal(expectedCasts, casts.Count);
    }

    [Fact]
    public void WithActivationRate_ReplacesEveryChanceAtEveryLevel()
    {
        SkillLevel Level(int level) => new(level, 0, 0, 0,
            [new SkillCondition(SkillConditionKind.Chance, 40), new SkillCondition(SkillConditionKind.Chance, 60)], [], [Effect]);
        var skill = new SkillDefinition("skill", "技能", SkillType.Active, SkillTiming.BeforeAttack, "", [Level(1), Level(2)]).WithActivationRate(10);

        Assert.All(skill.Levels, level => Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 10)], level.Conditions));
    }

    [Fact]
    public void WithActivationRate_KeepsOtherConditions()
    {
        // 條件 HitPointsBelow 50、Chance 40，發動率 100：HP 60% 不發動、40% 發動
        var skill = Skill([new SkillCondition(SkillConditionKind.HitPointsBelow, 50), new SkillCondition(SkillConditionKind.Chance, 40)], Effect)
            .WithActivationRate(100);

        Assert.Empty(Caster(skill, Combatant()).CastBeforeAttack(600, 100, new ScriptedRandom(0.0)));
        Assert.Single(Caster(skill, Combatant()).CastBeforeAttack(400, 100, new ScriptedRandom(0.0)));
    }

    [Theory]
    [InlineData(0.11, 1)]
    [InlineData(0.12, 0)]
    public void WithActivationRate_AlwaysOnSkillRollsTheRate(double roll, int expectedCasts)
    {
        // 燃燒魔力常駐沒有條件，發動率 12：擲 12 發動、擲 13 不發動
        var skill = Skill([], new ReduceSkillPointsEffect(40, null, Rounds: 0)).WithActivationRate(12);

        var casts = Caster(skill, Combatant()).CastBeforeAttack(1000, 100, new ScriptedRandom(roll), opponentSp: 100);

        Assert.False(skill.IsAlwaysOn);
        Assert.Equal(expectedCasts, casts.Count);
    }

    [Fact]
    public void WithActivationRate_EffectChanceStillRolls()
    {
        // 暈擊第 2 級 Stun 機率 6、怪物發動率 10：擲 10 發動；玩家出手前擲 7，沒有暈眩
        var stun = new EquippedSkill(Skill([new SkillCondition(SkillConditionKind.Chance, 50)], new StunEffect(6, Rounds: 1)).WithActivationRate(10), 1, 1);

        var outcome = WogBattle.Run(Combatant(), Combatant(), null, [], null, new ScriptedRandom(0.99, 0.09, 0.0, 0.06), null, monsterSkills: [stun]);

        Assert.Single(outcome.Events.OfType<SkillEvent>().Take(1));
        Assert.False(outcome.Events.OfType<AttackEvent>().First(attack => attack.Attacker == CombatSide.Player).Stunned);
    }
}
