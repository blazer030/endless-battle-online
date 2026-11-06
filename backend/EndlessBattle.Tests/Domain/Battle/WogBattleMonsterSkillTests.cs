using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// ScriptedRandom 的擲骰換算同 WogBattleTests：rand(1,100) = ⌊v×100⌋ + 1。
// 每回合的擲骰順序：玩家攻擊前技能 → 怪物攻擊前技能 → 先攻 → 攻擊（命中、暴擊、傷害）。
// 基準：玩家 agi 20 對怪物 10 → 玩家先攻門檻 70、暴擊率 20；怪物暴擊率 10。
// 傷害 = at·(at/df) − df：at 100 打 df 50 = 150，比值 1.5 → 傷害擲骰 rand(0,0)；at 200 = 750，比值 3.75 → rand(1,1) → 751；at 50 算出 0 → 1
public class WogBattleMonsterSkillTests
{
    private const double FirstRoll = 0.0;
    private const double LastRoll = 0.99;
    private const double NoCritical = 0.5;

    private static CombatantState Player(int hp = 1000, int sp = 100, int element = 0) =>
        new(hp, HpMax: 1000, sp, SpMax: 100, At: 100, Mat: 0, Df: 50, Mdf: 50, Agi: 20, Au: 10, element, Str: 100, Smart: 20);

    private static CombatantState Monster(int hp = 1000, int hpMax = 1000, int sp = 100, int at = 100) =>
        new(hp, hpMax, sp, SpMax: 100, at, Mat: 0, Df: 50, Mdf: 50, Agi: 10, Au: 10, Element: 0, Str: at, Smart: 0);

    private static EquippedSkill Skill(string name, IReadOnlyList<SkillCost> costs, params SkillEffect[] effects) =>
        new(new SkillDefinition(name, name, SkillType.Active, SkillTiming.BeforeAttack, "",
            [new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 100)], costs, effects)]), 1, 1);

    private static readonly SkillEffect RaiseAttack = new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, 100, null, null, Rounds: 3);

    [Fact]
    public void Run_MonsterCastsAfterPlayer_RecordsMonsterSkillEvent()
    {
        var player = Skill("玩家的技能", [new SkillCost(SkillCostKind.SkillPoints, 5)], RaiseAttack);
        var monster = Skill("怪物的技能", [new SkillCost(SkillCostKind.SkillPoints, 4), new SkillCost(SkillCostKind.HitPoints, 10)], RaiseAttack);

        var outcome = WogBattle.Run(Player(), Monster(), null, [player], null, new ScriptedRandom(LastRoll), null, monsterSkills: [monster]);

        var casts = outcome.Events.OfType<SkillEvent>().Take(2).ToList();
        Assert.Equal([CombatSide.Player, CombatSide.Monster], casts.Select(cast => cast.Caster));
        Assert.Equal("怪物的技能", casts[1].SkillName);
        Assert.Equal((0.0, 96, 4, 10), (casts[1].AtSeconds, casts[1].SpAfter, casts[1].SpCost, casts[1].HpCost));
    }

    [Fact]
    public void Run_MonsterHeals_RaisesMonsterHp()
    {
        var heal = Skill("治癒術", [], new RestoreEffect(SkillResource.HitPoints, null, null, 0) { PercentOfMaximum = 5 });

        // 怪物 800 回復 50，玩家先攻命中 150 → 700
        var outcome = WogBattle.Run(Player(), Monster(hp: 800), null, [], null,
            new ScriptedRandom(LastRoll, FirstRoll, FirstRoll, FirstRoll, NoCritical, FirstRoll), null, monsterSkills: [heal]);

        var cast = outcome.Events.OfType<SkillEvent>().First();
        Assert.Equal((CombatSide.Monster, 50), (cast.Caster, cast.Heal));
        Assert.Equal(700, outcome.Events.OfType<AttackEvent>().First(attack => attack.Attacker == CombatSide.Player).TargetHpAfter);
    }

    [Fact]
    public void Run_MonsterWithoutEnoughSp_DoesNotRoll()
    {
        var skill = Skill("爆氣", [new SkillCost(SkillCostKind.SkillPoints, 4)], RaiseAttack);

        var withSkill = WogBattle.Run(Player(), Monster(sp: 3), null, [], null, new SeededRandom(7), null, monsterSkills: [skill]);
        var withoutSkill = WogBattle.Run(Player(), Monster(sp: 3), null, [], null, new SeededRandom(7), null);

        Assert.Empty(withSkill.Events.OfType<SkillEvent>());
        Assert.Equal(withoutSkill.Events, withSkill.Events);
    }

    [Fact]
    public void Run_MonsterDefeatedByPlayerCast_DoesNotCast()
    {
        var finisher = Skill("星芒", [], new PercentDamageEffect(100, 0));
        var monsterSkill = Skill("爆氣", [], RaiseAttack);

        var outcome = WogBattle.Run(Player(), Monster(), null, [finisher], null, new ScriptedRandom(FirstRoll), null, monsterSkills: [monsterSkill]);

        Assert.DoesNotContain(outcome.Events.OfType<SkillEvent>(), cast => cast.Caster == CombatSide.Monster);
        Assert.Equal(CombatSide.Player, outcome.Winner);
    }

    [Fact]
    public void Run_MonsterCastDefeatsPlayer_EndsBattle()
    {
        var finisher = Skill("星芒", [], new PercentDamageEffect(100, 0));

        var outcome = WogBattle.Run(Player(), Monster(), null, [], null, new ScriptedRandom(FirstRoll), null, monsterSkills: [finisher]);

        Assert.Equal(CombatSide.Monster, outcome.Winner);
        Assert.Equal(CombatSide.Player, outcome.Events.OfType<DefeatEvent>().Single().Loser);
        Assert.Empty(outcome.Events.OfType<AttackEvent>());
        Assert.Equal(0, outcome.Player.Hp);
        Assert.Equal(0, outcome.Events.OfType<SkillEvent>().Single().OpponentHpAfter);
    }

    [Fact]
    public void Run_MonsterMoneyCost_IsNotPaid()
    {
        var skill = Skill("賄賂", [new SkillCost(SkillCostKind.Money, 500)], RaiseAttack);

        var outcome = WogBattle.Run(Player(), Monster(), null, [], null, new ScriptedRandom(LastRoll), null, money: 1000, monsterSkills: [skill]);

        Assert.Equal(0, outcome.Events.OfType<SkillEvent>().First().MoneyCost);
        Assert.Equal(0, outcome.MoneySpent);
    }

    private static AttackEvent FirstAttackBy(BattleOutcome outcome, CombatSide side) =>
        outcome.Events.OfType<AttackEvent>().First(attack => attack.Attacker == side);

    // 擲骰：怪物施放 → 先攻 → 命中 → 暴擊 → 傷害
    private static readonly double[] MonsterAttacksFirst = [FirstRoll, LastRoll, FirstRoll, NoCritical, FirstRoll];
    private static readonly double[] PlayerAttacksFirst = [FirstRoll, FirstRoll, FirstRoll, NoCritical, FirstRoll];

    private static BattleOutcome RunWithMonsterSkill(EquippedSkill skill, double[] rolls, CombatantState? player = null, CombatantState? monster = null) =>
        WogBattle.Run(player ?? Player(), monster ?? Monster(), null, [], null, new ScriptedRandom(NoCritical, rolls), null, monsterSkills: [skill]);

    [Fact]
    public void Run_MonsterBuff_RaisesItsDamage()
    {
        var outcome = RunWithMonsterSkill(Skill("爆氣", [], RaiseAttack), MonsterAttacksFirst);

        Assert.Equal(751, FirstAttackBy(outcome, CombatSide.Monster).Damage);
    }

    [Fact]
    public void Run_MonsterDebuff_LowersPlayerDamage()
    {
        var weaken = Skill("減益", [], new StatChangeEffect(SkillTarget.Opponent, SkillStat.Attack, -50, null, null, Rounds: 3));

        var outcome = RunWithMonsterSkill(weaken, PlayerAttacksFirst);

        Assert.Equal(1, FirstAttackBy(outcome, CombatSide.Player).Damage);
    }

    [Fact]
    public void Run_BothSidesAttackChanges_Combine()
    {
        var playerSkill = Skill("爆氣", [], RaiseAttack);
        var weaken = Skill("減益", [], new StatChangeEffect(SkillTarget.Opponent, SkillStat.Attack, -50, null, null, Rounds: 3));

        // 擲骰：玩家施放 → 怪物施放 → 先攻 → 命中 → 暴擊 → 傷害；100 × 2 × 0.5 = 100
        var outcome = WogBattle.Run(Player(), Monster(), null, [playerSkill], null,
            new ScriptedRandom(NoCritical, FirstRoll, FirstRoll, FirstRoll, FirstRoll, NoCritical, FirstRoll), null, monsterSkills: [weaken]);

        Assert.Equal(150, FirstAttackBy(outcome, CombatSide.Player).Damage);
    }

    [Fact]
    public void Run_MonsterDamageReduction_AppliesToPlayerAndPet()
    {
        var reduction = Skill("魔力盾", [], new DamageReductionEffect(SkillAttackKind.Physical, 50, Rounds: 3));
        var pet = new PetCombatant("小雪", Attack: 100, Magic: 0, Guard: 500, Activation: 35);

        // 擲骰：怪物施放 → 先攻玩家 → 玩家命中、不暴擊、傷害 → 怪物落空 → 寵物出擊、物理、命中、不暴擊、傷害
        var outcome = WogBattle.Run(Player(), Monster(), null, [], null,
            new ScriptedRandom(LastRoll, FirstRoll, FirstRoll, FirstRoll, NoCritical, FirstRoll, LastRoll, FirstRoll, FirstRoll, FirstRoll, NoCritical, FirstRoll),
            pet, monsterSkills: [reduction]);

        // 玩家 150 減半 75；寵物 150 + 附加 1 = 151 減半四捨五入 76
        Assert.Equal(75, FirstAttackBy(outcome, CombatSide.Player).Damage);
        Assert.Equal(76, outcome.Events.OfType<PetAttackEvent>().First().Damage);
    }

    [Fact]
    public void Run_MonsterSpeedChange_AffectsFirstAttackerThatRound()
    {
        // 怪物敏捷 10 → 40，高過玩家的 20：玩家先攻門檻從 70 降到 30，擲 51 變成怪物先攻
        var haste = Skill("急影", [], new StatChangeEffect(SkillTarget.Caster, SkillStat.Agility, 300, null, null, Rounds: 3));

        var outcome = RunWithMonsterSkill(haste, [FirstRoll, NoCritical]);

        Assert.Equal(CombatSide.Monster, outcome.Events.OfType<AttackEvent>().First().Attacker);
    }

    [Fact]
    public void Run_MonsterEvasion_LowersPlayerHit()
    {
        // 玩家命中 66.7 + 20 = 86.7，怪物迴避 30 → 56.7；擲 61 落空
        var evasion = Skill("煙遁", [], new StatChangeEffect(SkillTarget.Caster, SkillStat.Evasion, null, 30, null, Rounds: 3));

        var outcome = RunWithMonsterSkill(evasion, [FirstRoll, FirstRoll, 0.6]);

        Assert.False(FirstAttackBy(outcome, CombatSide.Player).Hit);
    }

    [Fact]
    public void Run_MonsterHitRate_RaisesItsHit()
    {
        // 怪物命中 33.3 + 20 = 53.3，命中 +30 → 83.3；擲 61 打中
        var focus = Skill("心眼", [], new StatChangeEffect(SkillTarget.Caster, SkillStat.HitRate, null, 30, null, Rounds: 3));

        var outcome = RunWithMonsterSkill(focus, [FirstRoll, LastRoll, 0.6]);

        Assert.True(FirstAttackBy(outcome, CombatSide.Monster).Hit);
    }

    [Fact]
    public void Run_MonsterCriticalBonus_RaisesItsCriticalChance()
    {
        // 怪物暴擊率 10 + 50 = 60；擲 31 暴擊
        var critical = Skill("要害狙擊", [], new StatChangeEffect(SkillTarget.Caster, SkillStat.PhysicalCriticalChance, null, 50, null, Rounds: 3));

        var outcome = RunWithMonsterSkill(critical, [FirstRoll, LastRoll, FirstRoll, 0.3]);

        Assert.True(FirstAttackBy(outcome, CombatSide.Monster).Critical);
    }

    [Fact]
    public void Run_MonsterBonusDamage_AddsToItsHit()
    {
        // 怪物力量 100 × 20% = 20，150 + 20
        var bonus = Skill("重擊", [], new BonusDamageEffect(SkillAttackKind.Physical, [new SkillScaling(SkillScalingBasis.Strength, 0.2)], Rounds: 3));

        var outcome = RunWithMonsterSkill(bonus, MonsterAttacksFirst);

        Assert.Equal(170, FirstAttackBy(outcome, CombatSide.Monster).Damage);
    }

    [Fact]
    public void Run_MonsterAttackElement_AppliesToItsHit()
    {
        // 火 3 剋木 4：150 × 1.2 = 180
        var element = Skill("魔劍-火", [], new AttackElementEffect(3, Rounds: 3));

        var outcome = RunWithMonsterSkill(element, MonsterAttacksFirst, Player(element: 4));

        Assert.Equal(180, FirstAttackBy(outcome, CombatSide.Monster).Damage);
    }

    [Fact]
    public void Run_MonsterDefenseElement_AppliesToPlayerHit()
    {
        // 火 3 打防禦屬性轉成木 4 的怪物：150 × 1.2 = 180
        var element = Skill("魔盾-木", [], new DefenseElementEffect(4, Rounds: 3));

        var outcome = RunWithMonsterSkill(element, PlayerAttacksFirst, Player(element: 3));

        Assert.Equal(180, FirstAttackBy(outcome, CombatSide.Player).Damage);
    }
}
