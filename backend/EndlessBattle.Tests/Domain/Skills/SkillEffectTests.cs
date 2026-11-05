using System.Text.Json;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Skills;

// 技能由發動條件、消耗、效果組成：戰鬥只依種類與數值計算（spec skill-effects）
public class SkillEffectTests
{
    private static CombatantState Player(int at = 200, int hp = 1000, int sp = 100, int str = 100) =>
        new(Hp: hp, HpMax: 1000, Sp: sp, SpMax: 100, At: at, Mat: 100, Df: 50, Mdf: 50, Agi: 20, Au: 25, Element: 0,
            SpecialAttackName: null, Str: str, Smart: 50);

    private static SkillDefinition Skill(SkillType type, params SkillLevel[] levels) =>
        new("skill", "技能", type, SkillTiming.BeforeAttack, "說明", levels);

    private static SkillLevel Level(int level, IReadOnlyList<SkillCondition> conditions, IReadOnlyList<SkillEffect> effects,
        IReadOnlyList<SkillCost>? costs = null, int cooldown = 0) =>
        new(level, Money: 500, Proficiency: 300, cooldown, conditions, costs ?? [], effects);

    private static SkillCondition Chance(int percent) => new(SkillConditionKind.Chance, percent);

    private static StatChangeEffect AttackPercent(double percent, int rounds = 3) =>
        new(SkillTarget.Caster, SkillStat.Attack, percent, Amount: null, Scaling: null, rounds);

    private static int AttackAfter(SkillCaster caster, CombatantState player) => caster.Current.ApplyToPlayer(player).At;

    [Fact]
    public void StatChange_RaisesAttackByLevelPercent()
    {
        var skill = Skill(SkillType.Active,
            Level(1, [Chance(40)], [AttackPercent(1)]),
            Level(2, [Chance(50)], [AttackPercent(2)]),
            Level(3, [Chance(60)], [AttackPercent(3)]));
        var player = Player(at: 200);
        var caster = new SkillCaster(player, [new EquippedSkill(skill, Level: 3, Slot: 1)]);

        caster.CastBeforeAttack(player.Hp, player.Sp, new ScriptedRandom(0.0));
        var rounds = new List<int> { AttackAfter(caster, player) };
        for (var round = 2; round <= 4; round++)
        {
            caster.ExpireAtRoundStart();
            rounds.Add(AttackAfter(caster, player));
        }

        Assert.Equal([206, 206, 206, 200], rounds);
    }

    // PhpRandom.Between(1, 100) 擲出 roll 的亂數值
    private static double Roll(int roll) => (roll - 0.5) / 100;

    private static SkillCondition HitPointsBelow(int percent) => new(SkillConditionKind.HitPointsBelow, percent);

    [Fact]
    public void CombinedEffects_ApplyTogether()
    {
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)],
        [
            new BonusDamageEffect(SkillAttackKind.Physical, [new SkillScaling(SkillScalingBasis.Strength, 0.45)], Rounds: 1),
            new StatChangeEffect(SkillTarget.Caster, SkillStat.HitRate, Percent: null, Amount: -70, Scaling: null, Rounds: 1),
        ]));
        var caster = new SkillCaster(Player(str: 100), [new EquippedSkill(skill, Level: 1, Slot: 1)]);

        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));

        Assert.Equal((45, -70), (caster.Current.BonusDamageFor(AttackKind.Physical), caster.Current.Rag));
        Assert.Equal(0, caster.Current.BonusDamageFor(AttackKind.Magic));
    }

    [Theory]
    [InlineData(500, null, false)]
    [InlineData(200, 25, true)]
    [InlineData(200, 31, false)]
    public void Conditions_AllMustHold(int hp, int? roll, bool activates)
    {
        var skill = Skill(SkillType.Active, Level(1, [HitPointsBelow(30), Chance(30)], [AttackPercent(10)]));
        var random = new ScriptedRandom(0.0, roll is { } value ? [Roll(value)] : []);
        var caster = new SkillCaster(Player(hp: hp), [new EquippedSkill(skill, Level: 1, Slot: 1)]);

        var casts = caster.CastBeforeAttack(hp, 100, random);

        Assert.Equal(activates, casts.Count == 1);
        Assert.Equal(roll is null ? 0 : 1, random.Consumed);
    }

    [Fact]
    public void NoConditions_IsAlwaysOn()
    {
        var skill = Skill(SkillType.Passive, Level(1, [], [new StatChangeEffect(SkillTarget.Caster, SkillStat.Defense, 10, null, null, Rounds: 0)]));
        var random = new ScriptedRandom(0.99);
        var caster = new SkillCaster(Player(), [new EquippedSkill(skill, Level: 1, Slot: 4)]);

        Assert.Empty(caster.CastBeforeAttack(1000, 100, random));
        caster.ExpireAtRoundStart();

        Assert.Equal((55, 0), (caster.Current.ApplyToPlayer(Player()).Df, random.Consumed));
    }

    [Fact]
    public void LastingSkill_DoesNotReactivate()
    {
        var skill = Skill(SkillType.Active, Level(1, [HitPointsBelow(50), Chance(100)], [AttackPercent(10, rounds: 3)]));
        var random = new ScriptedRandom(0.0);
        var caster = new SkillCaster(Player(hp: 300), [new EquippedSkill(skill, Level: 1, Slot: 1)]);

        var activations = new List<int>();
        var consumed = new List<int>();
        for (var round = 1; round <= 4; round++)
        {
            if (round > 1)
            {
                caster.ExpireAtRoundStart();
            }

            var before = random.Consumed;
            activations.Add(caster.CastBeforeAttack(300, 100, random).Count);
            consumed.Add(random.Consumed - before);
        }

        Assert.Equal([1, 0, 0, 1], activations);
        Assert.Equal([1, 0, 0, 1], consumed);
    }

    [Fact]
    public void Stacking_SameSkillAddsDifferentSkillsMultiply()
    {
        var player = Player(at: 200);
        var twice = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(5), AttackPercent(5)]));
        var once = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(5)]));

        var sameSkill = new SkillCaster(player, [new EquippedSkill(twice, Level: 1, Slot: 1)]);
        sameSkill.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));
        var twoSkills = new SkillCaster(player,
            [new EquippedSkill(once, Level: 1, Slot: 1), new EquippedSkill(once with { Id = "other" }, Level: 1, Slot: 2)]);
        twoSkills.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));

        Assert.Equal((220, 221), (AttackAfter(sameSkill, player), AttackAfter(twoSkills, player)));
    }

    [Fact]
    public void LevelValues_AreUsedAsWritten()
    {
        var skill = Skill(SkillType.Active,
            Level(1, [Chance(40)], [AttackPercent(1)]),
            Level(2, [Chance(70)], [AttackPercent(9)]));
        var player = Player(at: 200);
        var caster = new SkillCaster(player, [new EquippedSkill(skill, Level: 2, Slot: 1)]);

        var casts = caster.CastBeforeAttack(1000, 100, new ScriptedRandom(Roll(65)));

        Assert.Single(casts);
        Assert.Equal(218, AttackAfter(caster, player));
    }

    [Fact]
    public void StunChance_RolledAfterActivation()
    {
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(20), new StunEffect(Chance: 12, Rounds: 1)]));
        var player = Player(at: 200);
        var caster = new SkillCaster(player, [new EquippedSkill(skill, Level: 1, Slot: 1)]);

        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));

        Assert.Equal((240, 12), (AttackAfter(caster, player), caster.Current.StunChance));
    }

    private static CombatantState Monster() =>
        new(Hp: 3000, HpMax: 3000, Sp: 0, SpMax: 0, At: 120, Mat: 0, Df: 50, Mdf: 50, Agi: 15, Au: 10, Element: 0,
            SpecialAttackName: null, Str: 0, Smart: 0);

    private static string BattleWith(SkillDefinition skill, int level, int slot = 1) =>
        JsonSerializer.Serialize(WogBattle.Run(Player(), Monster(), AttackKind.Physical, [new EquippedSkill(skill, level, slot)], new SeededRandom(7)).Events);

    private static SkillLevel AttackLevel(int level, double percent) =>
        Level(level, [Chance(60)], [AttackPercent(percent)], costs: [new SkillCost(SkillCostKind.SkillPoints, 5)]);

    [Fact]
    public void SameParts_GiveSameBattle()
    {
        var ruin = Skill(SkillType.Active, AttackLevel(1, 5));
        var burst = Skill(SkillType.Active, AttackLevel(1, 1), AttackLevel(2, 2), AttackLevel(3, 3), AttackLevel(4, 4), AttackLevel(5, 5));

        // 戰鬥事件會記錄技能等級，比較時去掉這個欄位
        static string WithoutLevel(string events) => System.Text.RegularExpressions.Regex.Replace(events, "\"Level\":\\d+", "");

        Assert.Equal(WithoutLevel(BattleWith(ruin, level: 1)), WithoutLevel(BattleWith(burst, level: 5)));
    }

    [Fact]
    public void ActiveAndPassive_BehaveTheSame()
    {
        var active = Skill(SkillType.Active, AttackLevel(1, 5));
        var passive = Skill(SkillType.Passive, AttackLevel(1, 5));

        Assert.Equal(BattleWith(active, level: 1, slot: 1), BattleWith(passive, level: 1, slot: 4));
        Assert.Contains("\"SpAfter\":95", BattleWith(passive, level: 1, slot: 4));
    }

    [Fact]
    public void ChangingValue_ChangesResult()
    {
        var player = Player(at: 200);
        int AttackWith(double percent)
        {
            var caster = new SkillCaster(player, [new EquippedSkill(Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(percent)])), 1, 1)]);
            caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));
            return AttackAfter(caster, player);
        }

        Assert.Equal((206, 212), (AttackWith(3), AttackWith(6)));
    }
}
