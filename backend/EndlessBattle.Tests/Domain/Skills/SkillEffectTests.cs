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
        new(Hp: hp, HpMax: 1000, Sp: sp, SpMax: 100, At: at, Mat: 100, Df: 50, Mdf: 50, Agi: 20, Au: 25, Element: 0, Str: str, Smart: 50);

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
        new(Hp: 3000, HpMax: 3000, Sp: 0, SpMax: 0, At: 120, Mat: 0, Df: 50, Mdf: 50, Agi: 15, Au: 10, Element: 0, Str: 0, Smart: 0);

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

    private static StatChangeEffect Change(SkillStat stat, double? percent = null, double? amount = null, IReadOnlyList<SkillScaling>? scaling = null,
        SkillTarget target = SkillTarget.Caster) => new(target, stat, percent, amount, scaling, Rounds: 0);

    private static SkillCaster AlwaysOn(CombatantState player, params SkillEffect[] effects) =>
        new(player, [new EquippedSkill(Skill(SkillType.Passive, Level(1, [], effects)), 1, 4)]);

    [Fact]
    public void Evasion_LowersOpponentHitChance()
    {
        // 怪物 agi 15 對玩家 20：命中 15/35×100 + 20 ≈ 62.9；迴避 70 後低於 0，整場打不中
        var caster = AlwaysOn(Player(), Change(SkillStat.Evasion, amount: 70));
        var outcome = WogBattle.Run(Player(), Monster(), AttackKind.Physical,
            [new EquippedSkill(Skill(SkillType.Passive, Level(1, [], [Change(SkillStat.Evasion, amount: 70)])), 1, 4)], new SeededRandom(3));

        Assert.Equal(-70, caster.Current.EnemyRag);
        Assert.DoesNotContain(outcome.Events.OfType<AttackEvent>(), attack => attack.Attacker == CombatSide.Monster && attack.Hit);
    }

    [Fact]
    public void MagicCriticalChance_RaisesMagicCriticalOdds()
    {
        var mage = Player() with { Mat = 300, Au = 10 };
        var target = Monster() with { Au = 10 };
        // 魔法必中；暴擊擲 30：基本 10% 不暴擊，加 50% 後暴擊
        var withoutBonus = RoundResolver.Strike(mage, target, AttackKind.Magic, new ScriptedRandom(0.0, Roll(30)));
        var withBonus = RoundResolver.Strike(mage, target, AttackKind.Magic, new ScriptedRandom(0.0, Roll(30)), criticalChanceBonus: 50);

        Assert.Equal((false, true), (withoutBonus.Critical, withBonus.Critical));
        Assert.Equal(50, AlwaysOn(Player(), Change(SkillStat.MagicCriticalChance, amount: 50)).Current.CriticalChanceFor(AttackKind.Magic));
        Assert.Equal(0, AlwaysOn(Player(), Change(SkillStat.MagicCriticalChance, amount: 50)).Current.CriticalChanceFor(AttackKind.Physical));
    }

    [Fact]
    public void PhysicalCriticalCap_RaisesCriticalMultiplierLimit()
    {
        // 比值 10 → 1.2×10 = 12，上限 2；上限提高 25% 後是 2.5
        Assert.Equal(2, RoundResolver.CriticalOdds(100, 10).Multiplier);
        Assert.Equal(2.5, RoundResolver.CriticalOdds(100, 10, criticalCapPercent: 25).Multiplier, 10);
        Assert.Equal(25, AlwaysOn(Player(), Change(SkillStat.PhysicalCriticalCap, amount: 25)).Current.CriticalCapFor(AttackKind.Physical));
    }

    [Fact]
    public void MagicCriticalCap_AcceptsFractionalAmount()
    {
        // 熱情律動第 1 級：魔攻必殺上限 +2.5%
        Assert.Equal(2.5, AlwaysOn(Player(), Change(SkillStat.MagicCriticalCap, amount: 2.5)).Current.CriticalCapFor(AttackKind.Magic), 10);
    }

    [Fact]
    public void EscapeRate_RaisesEscapeChance()
    {
        Assert.False(EscapeRules.Succeeds(new ScriptedRandom(Roll(25))));
        Assert.True(EscapeRules.Succeeds(new ScriptedRandom(Roll(25)), bonus: 20));
        Assert.Equal(20, AlwaysOn(Player(), Change(SkillStat.EscapeRate, amount: 20)).Current.EscapeBonus);
    }

    [Fact]
    public void OpponentStat_LowersMonsterAgility()
    {
        var monster = Monster() with { Agi = 250, Au = 100 };

        var lowered = AlwaysOn(Player(), Change(SkillStat.Agility, amount: -200, target: SkillTarget.Opponent)).Current.ApplyToMonster(monster);
        var floored = AlwaysOn(Player(), Change(SkillStat.Charisma, amount: -4900, target: SkillTarget.Opponent)).Current.ApplyToMonster(monster);

        Assert.Equal((50, 1), (lowered.Agi, floored.Au));
    }

    [Fact]
    public void Scaling_UsesCharisma()
    {
        // 力量之舞：物攻 + 魅力×5%；魅力 100 → +5
        var player = Player(at: 200) with { Au = 100 };
        var caster = AlwaysOn(player, Change(SkillStat.Attack, scaling: [new SkillScaling(SkillScalingBasis.Charisma, 0.05)]));

        Assert.Equal(205, caster.Current.ApplyToPlayer(player).At);
    }

    [Fact]
    public void Scaling_UsesVitalityFaithAndLevel()
    {
        var player = Player(at: 200) with { Vit = 40, Be = 20, Level = 10 };
        var caster = AlwaysOn(player, Change(SkillStat.Attack, scaling:
        [
            new SkillScaling(SkillScalingBasis.Vitality, 0.5), new SkillScaling(SkillScalingBasis.Faith, 1), new SkillScaling(SkillScalingBasis.Level, 2),
        ]));

        Assert.Equal(260, caster.Current.ApplyToPlayer(player).At);
    }

    [Fact]
    public void StunChance_DoesNotAffectOtherEffects()
    {
        // 斯巴達：力量比對手高、發動骰 30 ≤ 40 → 必殺率 +20 一定生效；無法行動的 12% 留到怪物出手前另外擲（WogBattleSkillTests）
        var skill = Skill(SkillType.Active, Level(1, [new SkillCondition(SkillConditionKind.StatAbove, 0) { Stat = SkillScalingBasis.Strength }, Chance(40)],
            [Change(SkillStat.PhysicalCriticalChance, amount: 20) with { Rounds = 1 }, new StunEffect(Chance: 12, Rounds: 1)]));
        var random = new ScriptedRandom(0.99, 0.29);
        var caster = new SkillCaster(Player(), [new EquippedSkill(skill, 1, 1)], opponent: Monster());

        Assert.Single(caster.CastBeforeAttack(1000, 100, random));
        Assert.Equal(1, random.Consumed);
        Assert.Equal((20, 12), (caster.Current.CriticalChanceFor(AttackKind.Physical), caster.Current.StunChance));
    }

    private static SkillCaster Active(CombatantState player, params SkillEffect[] effects) =>
        new(player, [new EquippedSkill(Skill(SkillType.Active, Level(1, [Chance(100)], effects)), 1, 1)]);

    [Fact]
    public void PercentDamage_CutsOpponentCurrentHp()
    {
        var caster = Active(Player(), new PercentDamageEffect(Percent: 10, Rounds: 0));

        var cast = Assert.Single(caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0), opponentHp: 500));

        Assert.Equal(50, cast.OpponentDamage);
    }

    [Fact]
    public void DamageOverTime_HitsEachRound()
    {
        // 施毒：每回合智力×10%（智力 50 → 5），持續 3 回合
        var caster = Active(Player(), new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 0.1)], Rounds: 3));
        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));

        var damage = new List<int> { caster.DamageOverTime() };
        for (var round = 2; round <= 4; round++)
        {
            caster.ExpireAtRoundStart();
            damage.Add(caster.DamageOverTime());
        }

        Assert.Equal([5, 5, 5, 0], damage);
    }

    // 持續傷害在回合結束時生效，記成技能事件；打倒怪物就結束戰鬥
    [Fact]
    public void DamageOverTime_DefeatsMonsterAtRoundEnd()
    {
        var poison = Skill(SkillType.Active, Level(1, [Chance(100)], [new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 100)], Rounds: 3)]));

        var outcome = WogBattle.Run(Player(), Monster(), AttackKind.Physical, [new EquippedSkill(poison, 1, 1)], new SeededRandom(7));

        Assert.Equal(CombatSide.Player, outcome.Winner);
        var tick = outcome.Events.OfType<SkillEvent>().Last();
        Assert.Equal((5000, 0), (tick.Damage, tick.OpponentHpAfter));
        Assert.IsType<DefeatEvent>(outcome.Events[^1]);
    }

    // 發動那一次是施放；同一回合結束起的持續傷害標成持續結算
    [Fact]
    public void DamageOverTime_TicksAreMarkedLasting()
    {
        var poison = Skill(SkillType.Active, Level(1, [Chance(100)], [new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 0.1)], Rounds: 3)]));

        var outcome = WogBattle.Run(Player(), Monster(), AttackKind.Physical, [new EquippedSkill(poison, 1, 1)], new SeededRandom(7));

        var events = outcome.Events.OfType<SkillEvent>().Take(2).ToList();
        Assert.Equal((false, 0), (events[0].Lasting, events[0].Damage));
        Assert.Equal((true, 5), (events[1].Lasting, events[1].Damage));
    }

    // 常駐削減對手 SP 每回合生效：事件帶出實際削減的量（30 → 10 → 0），削完就沒有事件
    [Fact]
    public void AlwaysOnSkillPointsReduction_RecordsTheSpRemoved()
    {
        var drain = Skill(SkillType.Passive, Level(1, [], [new ReduceSkillPointsEffect(Amount: 20, PercentOfMaximum: null, Rounds: 0)]));

        var outcome = WogBattle.Run(Player(), Monster() with { Sp = 30, SpMax = 30 }, AttackKind.Physical, [new EquippedSkill(drain, 1, 4)], new SeededRandom(7));

        Assert.Equal([20, 10], outcome.Events.OfType<SkillEvent>().Select(skill => skill.OpponentSpLoss));
    }

    [Fact]
    public void ChanceCoversAllEffects()
    {
        // 施毒：機率 25% 擲 20，對手物攻降低與持續傷害兩個效果都生效
        var skill = Skill(SkillType.Active, Level(1, [Chance(25)],
        [
            Change(SkillStat.Attack, percent: -8, target: SkillTarget.Opponent) with { Rounds = 3 },
            new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 0.1)], Rounds: 3),
        ]));
        var caster = new SkillCaster(Player(), [new EquippedSkill(skill, 1, 1)]);

        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(Roll(20)));

        Assert.Equal((0.92, 5), (Math.Round(caster.Current.EnemyAt, 10), caster.DamageOverTime()));
    }

    [Fact]
    public void MagicDamageReduction_LowersMagicTaken()
    {
        var caster = AlwaysOn(Player(), new DamageReductionEffect(SkillAttackKind.Magic, 20, Rounds: 0));

        Assert.Equal((80, 100), (caster.Current.DamageTaken(AttackKind.Magic, 100), caster.Current.DamageTaken(AttackKind.Physical, 100)));
    }

    [Fact]
    public void RestoreSkillPoints_UsesPercentOfMaximum()
    {
        // 魔力吸收：回復 SP 最大值×10%（最大 100 → 10）
        var caster = Active(Player(sp: 50), new RestoreEffect(SkillResource.SkillPoints, null, null, Rounds: 0) { PercentOfMaximum = 10 });

        var cast = Assert.Single(caster.CastBeforeAttack(1000, 50, new ScriptedRandom(0.0)));

        Assert.Equal((60, 10), (cast.SpAfter, cast.SpGain));
    }

    [Fact]
    public void AttackElement_ChangesElementMultiplier()
    {
        // 魔劍-火：攻擊屬性轉成火（3），打木（4）的怪是剋制 1.2
        var caster = AlwaysOn(Player(), new AttackElementEffect(Element: 3, Rounds: 0));
        var attacker = caster.Current.AttackingAs(Player());

        Assert.Equal(1.2, RoundResolver.Strike(attacker, Monster() with { Element = 4 }, AttackKind.Magic, new ScriptedRandom(0.0)).ElementMultiplier);
        Assert.Equal(0, caster.Current.DefendingAs(Player()).Element);
    }

    [Fact]
    public void ElementAdvantage_RaisesAdvantageDamage()
    {
        var fire = Player() with { Element = 3 };

        var result = RoundResolver.Strike(fire, Monster() with { Element = 4 }, AttackKind.Magic, new ScriptedRandom(0.0), elementAdvantagePercent: 5);

        Assert.Equal(1.26, result.ElementMultiplier, 10);
        Assert.Equal(5, AlwaysOn(Player(), new ElementAdvantageEffect(Percent: 5, Rounds: 0)).Current.ElementAdvantagePercent);
    }

    [Fact]
    public void ReduceSkillPoints_LowersOpponentSp()
    {
        var caster = Active(Player(), new ReduceSkillPointsEffect(Amount: 20, PercentOfMaximum: null, Rounds: 0));

        var cast = Assert.Single(caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0), opponentSp: 100));

        Assert.Equal(20, cast.OpponentSpLoss);
    }

    [Fact]
    public void Silence_BlocksOpponentSkills()
    {
        // 被封鎖 2 回合的一方：技能不發動也不擲骰，第 3 回合恢復
        var random = new ScriptedRandom(0.0);
        var silenced = Active(Player(), AttackPercent(5, rounds: 0));
        silenced.Silence(rounds: 2);

        var activations = new List<int>();
        for (var round = 1; round <= 3; round++)
        {
            if (round > 1)
            {
                silenced.ExpireAtRoundStart();
            }

            activations.Add(silenced.CastBeforeAttack(1000, 100, random).Count);
        }

        Assert.Equal([0, 0, 1], activations);
        Assert.Equal(1, random.Consumed);
        Assert.Equal((10, 2), (Active(Player(), new SilenceEffect(Chance: 10, Rounds: 2)).AfterCast().Current.SilenceChance, 2));
    }

    [Theory]
    [InlineData(25, 0)]
    [InlineData(15, 45)]
    public void BonusDamageChance_RollsAfterActivation(int roll, int expected)
    {
        // 暗器：發動後另擲 20%，擲中才追加 力量×45%（力量 100 → 45）
        var caster = Active(Player(str: 100),
            new BonusDamageEffect(SkillAttackKind.Physical, [new SkillScaling(SkillScalingBasis.Strength, 0.45)], Rounds: 1) { Chance = 20 });

        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0, 0.0, Roll(roll)));

        Assert.Equal(expected, caster.Current.BonusDamageFor(AttackKind.Physical));
    }

    [Fact]
    public void AlwaysOnPercentDamage_AppliesEveryTiming()
    {
        var caster = AlwaysOn(Player(), new PercentDamageEffect(Percent: 10, Rounds: 0));

        var first = caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0), opponentHp: 500);
        caster.ExpireAtRoundStart();
        var second = caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0), opponentHp: 450);

        Assert.Equal([50, 45], first.Concat(second).Select(cast => cast.OpponentDamage));
    }

    private static SkillCost HitPointsPercent(double percent) => new(SkillCostKind.HitPoints, 0) { Percent = percent };

    [Fact]
    public void StatAbove_BlocksWhenLower()
    {
        // 劍雨：魔攻比對手高才發動；50 對 60 不發動、不扣 SP、不擲骰
        var skill = Skill(SkillType.Active, Level(1,
            [new SkillCondition(SkillConditionKind.StatAbove, 0) { Stat = SkillScalingBasis.MagicAttack }, Chance(40)],
            [AttackPercent(5)], costs: [new SkillCost(SkillCostKind.SkillPoints, 5)]));
        var random = new ScriptedRandom(0.0);
        var caster = new SkillCaster(Player() with { Mat = 50 }, [new EquippedSkill(skill, 1, 1)], opponent: Monster() with { Mat = 60 });

        Assert.Empty(caster.CastBeforeAttack(1000, 100, random));
        Assert.Equal(0, random.Consumed);
        Assert.Single(new SkillCaster(Player() with { Mat = 70 }, [new EquippedSkill(skill, 1, 1)], opponent: Monster() with { Mat = 60 })
            .CastBeforeAttack(1000, 100, new ScriptedRandom(0.0)));
    }

    [Fact]
    public void HitPointsCost_PaidOnActivation()
    {
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(50)], costs: [HitPointsPercent(45)]));

        var cast = Assert.Single(new SkillCaster(Player(hp: 1000), [new EquippedSkill(skill, 1, 1)]).CastBeforeAttack(1000, 100, new ScriptedRandom(0.0)));

        Assert.Equal((550, 450), (cast.HpAfter, cast.HpCost));
    }

    [Fact]
    public void UnaffordableCost_BlocksSkill()
    {
        // HP 必須大於消耗量：目前 400、扣 45%（450）→ 不發動、不擲骰
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(50)], costs: [HitPointsPercent(45)]));
        var random = new ScriptedRandom(0.0);

        Assert.Empty(new SkillCaster(Player(hp: 400), [new EquippedSkill(skill, 1, 1)]).CastBeforeAttack(400, 100, random));
        Assert.Equal(0, random.Consumed);
    }

    [Fact]
    public void EffectValues_UseResourcesBeforeCost()
    {
        // 魔力爆發：消耗全部 SP，魔攻 + SP×0.1；SP 300、魔攻 100 → 130，SP 變 0
        var player = Player() with { Mat = 100, Sp = 300, SpMax = 300 };
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)],
            [Change(SkillStat.MagicAttack, scaling: [new SkillScaling(SkillScalingBasis.SkillPoints, 0.1)]) with { Rounds = 1 }],
            costs: [new SkillCost(SkillCostKind.SkillPoints, 0) { All = true }]));
        var caster = new SkillCaster(player, [new EquippedSkill(skill, 1, 1)]);

        var cast = Assert.Single(caster.CastBeforeAttack(1000, 300, new ScriptedRandom(0.0)));

        Assert.Equal((130, 0, 300), (caster.Current.ApplyToPlayer(player).Mat, cast.SpAfter, cast.SpCost));
    }

    [Fact]
    public void MonsterMoneyCost_IsNotPaid()
    {
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)], [AttackPercent(5)], costs: [new SkillCost(SkillCostKind.Money, 100)]));

        // 金錢不限（怪物）時照樣發動、不記扣錢；玩家錢不夠就不發動
        var unlimited = Assert.Single(new SkillCaster(Player(), [new EquippedSkill(skill, 1, 1)]).CastBeforeAttack(1000, 100, new ScriptedRandom(0.0)));
        var poor = new SkillCaster(Player(), [new EquippedSkill(skill, 1, 1)], money: 50).CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));
        var rich = new SkillCaster(Player(), [new EquippedSkill(skill, 1, 1)], money: 500);
        var paid = Assert.Single(rich.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0)));

        Assert.Equal((0, 0, 100, 400L), (unlimited.MoneyCost, poor.Count, paid.MoneyCost, rich.Money!.Value));
    }

    [Theory]
    [InlineData(300, 500, true)]
    [InlineData(600, 500, false)]
    public void HitPointsBelowOpponent_TriggersOnlyWhenBehind(int hp, int opponentHp, bool activates)
    {
        var skill = Skill(SkillType.Active, Level(1, [new SkillCondition(SkillConditionKind.HitPointsBelowOpponent, 0)], [AttackPercent(5)]));

        var casts = new SkillCaster(Player(hp: hp), [new EquippedSkill(skill, 1, 1)]).CastBeforeAttack(hp, 100, new ScriptedRandom(0.0), opponentHp);

        Assert.Equal(activates, casts.Count == 1);
    }

    [Fact]
    public void RoundEndTiming_ActivatesAtRoundEnd()
    {
        var skill = Skill(SkillType.Active, Level(1, [Chance(100)], [new RestoreEffect(SkillResource.HitPoints, [new SkillScaling(SkillScalingBasis.Intelligence, 1)], null, 0)]))
            with { Timing = SkillTiming.RoundEnd };
        var caster = new SkillCaster(Player(hp: 500), [new EquippedSkill(skill, 1, 1)]);

        Assert.Empty(caster.CastBeforeAttack(500, 100, new ScriptedRandom(0.0)));
        Assert.Equal(550, Assert.Single(caster.CastAtRoundEnd(500, 100, new ScriptedRandom(0.0))).HpAfter);
    }
}

internal static class SkillCasterTestExtensions
{
    // 施放一次（擲骰都中），回傳同一個 caster 方便串接
    public static SkillCaster AfterCast(this SkillCaster caster)
    {
        caster.CastBeforeAttack(1000, 100, new ScriptedRandom(0.0));
        return caster;
    }
}