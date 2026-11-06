using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// ScriptedRandom 的擲骰換算同 WogBattleTests：rand(1,100) = ⌊v×100⌋ + 1。
// 每回合的擲骰順序：玩家攻擊前技能 → 怪物攻擊前技能 → 先攻 → 攻擊（命中、暴擊、傷害）。
// 基準：玩家 agi 20 對怪物 10 → 玩家先攻門檻 70、暴擊率 20；怪物暴擊率 10。
// 雙方 at 100 打 df 50 = (100·2 − 50) = 150，比值 1.5 → 傷害擲骰 rand(0,0)
public class WogBattleMonsterSkillTests
{
    private const double FirstRoll = 0.0;
    private const double LastRoll = 0.99;
    private const double NoCritical = 0.5;

    private static CombatantState Player(int hp = 1000, int sp = 100) =>
        new(hp, HpMax: 1000, sp, SpMax: 100, At: 100, Mat: 0, Df: 50, Mdf: 50, Agi: 20, Au: 10, Element: 0, Str: 100, Smart: 20);

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
}
