using System.Text.Json;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// ScriptedRandom 的擲骰換算同 WogBattleTests：rand(1,100) = ⌊v×100⌋ + 1；rand(1,10) = ⌊v×10⌋ + 1。
// 每回合玩家側的擲骰順序：生效中技能失效 → 欄位 1→5 的攻擊前技能（持續中或 SP 不足不擲）→ 先攻 → 攻擊。
// 玩家 at 100 ≥ mat 0，開戰不擲攻擊方式；怪物 at ≥ mat 時也不擲。
// 基準：玩家 agi 20 對怪物 10 → 先攻門檻 70、命中 86.7、物理暴擊率 20；怪物命中 53.3、暴擊率 10。
// 玩家 at 100 打 df 50 = (100·2 − 50) = 150，比值 1.5 → 傷害擲骰 rand(0,0)
public class WogBattleSkillTests
{
    private const string SpecialAttack = "烈焰斬";

    private static readonly SkillDefinition Burst = TestSkills.Battle("burst", "爆氣", SkillEffectKind.AtMultiplier);
    private static readonly SkillDefinition Combo = TestSkills.Battle("combo", "連擊", SkillEffectKind.MultiHit);
    private static readonly SkillDefinition Heal = TestSkills.Battle("heal", "治癒", SkillEffectKind.Heal, rounds: 1);
    private static readonly SkillDefinition Stun = TestSkills.Battle("stun", "暈眩", SkillEffectKind.Stun);
    private static readonly SkillDefinition Bloodthirst = TestSkills.Battle("bloodthirst", "嗜血", SkillEffectKind.Bloodthirst, SkillType.Passive, SkillTiming.AfterAttack, rounds: 1);
    private static readonly SkillDefinition Counter = TestSkills.Battle("counter", "反擊", SkillEffectKind.Counter, SkillType.Passive);
    private static readonly SkillDefinition WarGodWrath = TestSkills.Battle("war-god-wrath", "戰神之怒", SkillEffectKind.WarGodWrath);
    private static readonly SkillDefinition Weaken = TestSkills.Battle("weaken", "減益", SkillEffectKind.EnemyAttackDown);

    private static CombatantState Player(int hp = 1000, int hpMax = 1000, int sp = 100, int str = 100, int smart = 20) =>
        new(hp, hpMax, sp, SpMax: 100, At: 100, Mat: 0, Df: 50, Mdf: 50, Agi: 20, Au: 10, Element: 0, SpecialAttack, str, smart);

    private static CombatantState Monster(int hp = 1000, int at = 0, int mat = 0) =>
        new(hp, hp, Sp: 0, SpMax: 0, at, mat, Df: 50, Mdf: 50, Agi: 10, Au: 10, Element: 0, SpecialAttackName: null, Str: at, Smart: mat);

    private static EquippedSkill Equip(SkillDefinition skill, int level, int chance = 100, int slot = 1) => new(skill, level, chance, slot);

    private static List<AttackEvent> AttacksBy(BattleOutcome outcome, CombatSide side) =>
        outcome.Events.OfType<AttackEvent>().Where(attack => attack.Attacker == side).ToList();

    // 連打的時間是半回合平分後的小數，時間比到小數 6 位，其餘欄位完全相同
    private static void AssertEventsAt(IEnumerable<BattleEvent> expected, IEnumerable<BattleEvent> actual)
    {
        var expectedEvents = expected.ToList();
        var actualEvents = actual.ToList();
        Assert.Equal(expectedEvents.Count, actualEvents.Count);
        foreach (var (expectedEvent, actualEvent) in expectedEvents.Zip(actualEvents))
        {
            Assert.Equal(expectedEvent.AtSeconds, actualEvent.AtSeconds, 6);
            Assert.Equal(expectedEvent, actualEvent with { AtSeconds = expectedEvent.AtSeconds });
        }
    }

    // SP 4 < 爆氣 lv1 的 5：整場不發動、不扣 SP，也不擲發動骰，所以事件與耗用的亂數都和沒有技能時相同
    [Fact]
    public void Battle_SkipsSkillWhenSpInsufficient()
    {
        var player = Player(hp: 300, sp: 4);
        var monster = Monster(at: 60);
        EquippedSkill[] skills = [Equip(Burst, level: 1)];

        var withSkill = WogBattle.Run(player, monster, null, skills, new SeededRandom(7));
        var withoutSkill = WogBattle.Run(player, monster, null, new SeededRandom(7));

        Assert.Empty(withSkill.Events.OfType<SkillEvent>());
        Assert.Equal(withoutSkill.Events, withSkill.Events);
        Assert.Equal(4, withSkill.Player.Sp);

        var scriptedWithSkill = new ScriptedRandom(0.5);
        var scriptedWithoutSkill = new ScriptedRandom(0.5);
        WogBattle.Run(player, monster, null, skills, scriptedWithSkill);
        WogBattle.Run(player, monster, null, scriptedWithoutSkill);
        Assert.Equal(scriptedWithoutSkill.Consumed, scriptedWithSkill.Consumed);
    }

    // 爆氣 lv5（sp 17、持續 3 回合）、欄位發動率 50。at 105：105·2.1 − 50 = 170.5，比值 1.62 → rand(0,0) → 171；at 100 → 150。
    // 每回合：[爆氣] 先攻 0.0 玩家先、命中 0.0、暴擊 0.99 不中、傷害 0.0；怪物 at 0 命中 0.99 → 100 未命中。
    // 第 1 回合爆氣 0.0 → 1 ≤ 50 發動（SP 100 → 83）；第 2、3 回合持續中不擲（若擲了，0.0 會被當成發動骰、整串錯位）；
    // 第 4 回合開頭失效，發動骰 0.99 → 100 > 50 不發動，at 回到 100。之後全是 0.99：怪物先、雙方都未命中
    [Fact]
    public void Battle_BuffExpiresAndDoesNotStack()
    {
        var random = new ScriptedRandom(0.99,
            0.0, 0.0, 0.0, 0.99, 0.0, 0.99,
            0.0, 0.0, 0.99, 0.0, 0.99,
            0.0, 0.0, 0.99, 0.0, 0.99,
            0.99, 0.0, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(hp: 10000), null, [Equip(Burst, level: 5, chance: 50)], random);

        Assert.Equal([171, 171, 171, 150], AttacksBy(outcome, CombatSide.Player).Take(4).Select(attack => attack.Damage));
        Assert.Equal(new SkillEvent(0, CombatSide.Player, "burst", "爆氣", Level: 5, SpAfter: 83, Heal: 0), Assert.Single(outcome.Events.OfType<SkillEvent>()));
        Assert.Equal(83, outcome.Player.Sp);
    }

    // 連擊 lv2（sp 8）：物理 3 下、每下 at ×0.98 → 98：98·1.96 − 50 = 142.08 → 142；平均分在攻方的半回合（0.4 秒）內：0、0.1333、0.2667，各自擲命中／暴擊／傷害。
    // 擲骰：連擊 0.0；先攻 0.0；第 1 下 命中 0.0、暴擊 0.99、傷害 0.0；第 2 下 命中 0.99 → 100 未命中；
    // 第 3 下 命中 0.0、暴擊 0.99、傷害 0.0；怪物命中 0.99 未命中
    [Fact]
    public void Battle_ComboHitsLevelPlusOneTimes()
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.0, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(), null, [Equip(Combo, level: 2)], random);

        AssertEventsAt(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new SkillEvent(0, CombatSide.Player, "combo", "連擊", Level: 2, SpAfter: 92, Heal: 0),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 142, ElementMultiplier: 1.0, TargetHpAfter: 858, SpecialAttackName: null),
                new AttackEvent(0.133333, CombatSide.Player, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 858, SpecialAttackName: null),
                new AttackEvent(0.266667, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 142, ElementMultiplier: 1.0, TargetHpAfter: 716, SpecialAttackName: null),
                new AttackEvent(0.4, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 1000, SpecialAttackName: null),
            },
            outcome.Events.Take(6));
    }

    // 連擊 lv5 → 6 下，間隔 0.4 / 6 = 0.0667：最後一下 0.3333 仍早於怪物在 0.4 出手。
    // 擲骰：連擊 0.0；先攻 0.0；6 下命中都擲 0.99 → 未命中（不再擲暴擊／傷害）；怪物命中 0.99 未命中
    [Fact]
    public void Battle_ComboHitsStayInsideHalfRound()
    {
        var outcome = WogBattle.Run(Player(), Monster(), null, [Equip(Combo, level: 5)], new ScriptedRandom(0.99, 0.0, 0.0));

        var firstRound = outcome.Events.OfType<AttackEvent>().Where(attack => attack.AtSeconds < BattleTiming.SecondsPerRound).ToList();
        var playerHits = firstRound.Where(attack => attack.Attacker == CombatSide.Player).Select(attack => attack.AtSeconds).ToList();
        Assert.Equal(6, playerHits.Count);
        foreach (var (expected, actual) in new[] { 0, 1 / 15.0, 2 / 15.0, 3 / 15.0, 4 / 15.0, 5 / 15.0 }.Zip(playerHits))
        {
            Assert.Equal(expected, actual, 6);
        }

        Assert.True(playerHits[^1] < 0.4);
        Assert.Equal(0.4, firstRound.Single(attack => attack.Attacker == CombatSide.Monster).AtSeconds);
    }

    // 同上的 142 點連打，怪物只有 200 血：第 2 下擊倒（0.1333 秒）就停，不打第 3 下。
    // 擲骰：連擊 0.0；先攻 0.0；兩下都是 命中 0.0、暴擊 0.99、傷害 0.0
    [Fact]
    public void Battle_ComboStopsWhenMonsterFalls()
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.0, 0.0, 0.99, 0.0, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(), Monster(hp: 200), null, [Equip(Combo, level: 2)], random);

        AssertEventsAt(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new SkillEvent(0, CombatSide.Player, "combo", "連擊", Level: 2, SpAfter: 92, Heal: 0),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 142, ElementMultiplier: 1.0, TargetHpAfter: 58, SpecialAttackName: null),
                new AttackEvent(0.133333, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 142, ElementMultiplier: 1.0, TargetHpAfter: 0, SpecialAttackName: null),
                new DefeatEvent(0.133333, CombatSide.Monster),
            },
            outcome.Events);
    }

    // 治癒 lv3（sp 11）：round(smart 20 × (1 + 0.375·2)) = 35，不超過 HpMax 100（滿血不施放，見 Battle_HealSkipsAtFullHp）。
    // 擲骰：治癒 0.0；先攻 0.0；命中 0.0；暴擊 0.99；傷害 0.0 → 150 擊倒 100 血的怪物
    [Theory]
    [InlineData(50, 35, 85)]
    [InlineData(90, 10, 100)]
    public void Battle_HealCapsAtHpMax(int hp, int expectedHeal, int expectedHp)
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.0, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(hp: hp, hpMax: 100), Monster(hp: 100), null, [Equip(Heal, level: 3)], random);

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new SkillEvent(0, CombatSide.Player, "heal", "治癒", Level: 3, SpAfter: 89, expectedHeal),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 0, SpecialAttackName: null),
                new DefeatEvent(0, CombatSide.Monster),
            },
            outcome.Events);
        Assert.Equal(expectedHp, outcome.Player.Hp);
    }

    // SP 只在住宿回復：滿血時治癒不擲骰、不扣 SP、不產生事件，事件與耗用的亂數都和沒有技能時相同
    [Fact]
    public void Battle_HealSkipsAtFullHp()
    {
        var player = Player(hp: 100, hpMax: 100);
        var withSkill = new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.0);
        var withoutSkill = new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(player, Monster(hp: 100), null, [Equip(Heal, level: 3)], withSkill);
        var baseline = WogBattle.Run(player, Monster(hp: 100), null, withoutSkill);

        Assert.Empty(outcome.Events.OfType<SkillEvent>());
        Assert.Equal(100, outcome.Player.Sp);
        Assert.Equal(baseline.Events, outcome.Events);
        Assert.Equal(withoutSkill.Consumed, withSkill.Consumed);
    }

    // 嗜血同理：滿血（1000）命中後不擲骰。怪物 at 0、雙方各一下：先攻 0.0；命中 0.0、暴擊 0.99、傷害 0.0 → 150；怪物命中 0.99 未命中
    [Fact]
    public void Battle_BloodthirstSkipsAtFullHp()
    {
        var withSkill = new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.0);
        var withoutSkill = new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(), Monster(hp: 150), null, [Equip(Bloodthirst, level: 5, chance: 100, slot: 4)], withSkill);
        var baseline = WogBattle.Run(Player(), Monster(hp: 150), null, withoutSkill);

        Assert.Empty(outcome.Events.OfType<SkillEvent>());
        Assert.Equal(100, outcome.Player.Sp);
        Assert.Equal(baseline.Events, outcome.Events);
        Assert.Equal(withoutSkill.Consumed, withSkill.Consumed);
    }

    // 暈眩 lv5（持續 3 回合）：怪物出手前擲 rand(1,100) ≤ 15 → 跳過。怪物 at 100 打玩家 df 50 = 150。
    // 第 1 回合：暈眩 0.0；先攻 0.99 → 100 > 70 怪物先；暈眩骰 0.14 → 15 ≤ 15 → 跳過；玩家命中 0.99 未命中。
    // 第 2 回合：暈眩持續中不擲；先攻 0.99 怪物先；暈眩骰 0.15 → 16 > 15 → 出手：命中 0.0、暴擊 0.99、傷害 0.0 → 150；玩家 0.99 未命中
    [Fact]
    public void Battle_StunSkipsMonsterTurn()
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.99, 0.14, 0.99, 0.99, 0.15, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(at: 100), null, [Equip(Stun, level: 5)], random);

        Assert.Equal(
            new[]
            {
                new AttackEvent(0, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 1000, SpecialAttackName: null, Stunned: true),
                new AttackEvent(0.8, CombatSide.Monster, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 850, SpecialAttackName: null),
            },
            AttacksBy(outcome, CombatSide.Monster).Take(2));
    }

    // 嗜血 lv5（被動、攻擊後）：常駐，物理命中後不擲骰、不扣 SP，回復 round(150 × 0.1) = 15，事件與那一下同時。
    // 第 1 回合：先攻 0.0；命中 0.0、暴擊 0.99、傷害 0.0 → 150；怪物命中 0.99 未命中。
    // 第 2 回合：先攻 0.0；命中 0.99 未命中 → 不回復；怪物 0.99 未命中。之後全是 0.99，雙方都未命中
    [Fact]
    public void Battle_BloodthirstHealsAfterPhysicalHit()
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99, 0.99);

        var outcome = WogBattle.Run(Player(hp: 500), Monster(), null, [Equip(Bloodthirst, level: 5, slot: 4)], random);

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 850, SpecialAttackName: null),
                new SkillEvent(0, CombatSide.Player, "bloodthirst", "嗜血", Level: 5, SpAfter: 100, Heal: 15),
                new AttackEvent(0.4, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 515, SpecialAttackName: null),
                new RoundEvent(0.8, 2),
                new AttackEvent(0.8, CombatSide.Player, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 850, SpecialAttackName: null),
                new AttackEvent(0.8 + 0.4, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHpAfter: 515, SpecialAttackName: null),
            },
            outcome.Events.Take(7));
        Assert.Single(outcome.Events.OfType<SkillEvent>());
        Assert.Equal(515, outcome.Player.Hp);
    }

    // 反擊 lv5（被動、常駐不擲骰）：受物傷 ×(0.92 − 0.1) = 0.82。怪物 at 100 原傷害 150 → 123。
    // 擲骰：先攻 0.99 怪物先；怪物命中 0.0、暴擊 0.99、傷害 0.0；玩家命中 0.99 未命中
    [Fact]
    public void Battle_CounterReducesPhysicalDamageTaken()
    {
        var random = new ScriptedRandom(0.99, 0.99, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(at: 100), null, [Equip(Counter, level: 5, slot: 4)], random);

        Assert.Equal(
            new AttackEvent(0, CombatSide.Monster, AttackKind.Physical, Hit: true, Critical: false, Damage: 123, ElementMultiplier: 1.0, TargetHpAfter: 877, SpecialAttackName: null),
            AttacksBy(outcome, CombatSide.Monster)[0]);
    }

    // 戰神之怒 lv5：rag −75 + 25 = −50 → 命中門檻 20/30×100 − 50 + 20 = 36.67（沒有技能時 86.67，37 也會命中）；
    // at += round(str 100 × 0.45) = 45 → 145：145·2.9 − 50 = 370.5，比值 2.56 → rand(0,1) 擲 0.0 → 0 → 371。
    // 擲骰：戰神之怒 0.0；先攻 0.0；命中依參數（0.355 → 36 命中、0.365 → 37 未命中）；命中時暴擊 0.99、傷害 0.0
    [Theory]
    [InlineData(0.355, true, 371)]
    [InlineData(0.365, false, 0)]
    public void Battle_WarGodWrathLowersHitChance(double hitRoll, bool expectedHit, int expectedDamage)
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.0, hitRoll, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(str: 100), Monster(), null, [Equip(WarGodWrath, level: 5)], random);

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Physical, expectedHit, Critical: false, expectedDamage, ElementMultiplier: 1.0,
                TargetHpAfter: 1000 - expectedDamage, SpecialAttackName: null),
            AttacksBy(outcome, CombatSide.Player)[0]);
    }

    // 減益 lv5：敵 at、mat ×0.9 → 90：90·1.8 − 50 = 112（沒有技能時 150），物理打 df、魔法打 mdf 都是 50。
    // 擲骰：減益 0.0；先攻 0.99 怪物先；物理時命中 0.0／魔法時攻擊方式 0.0 → 1 ≤ 8；暴擊 0.99；傷害 0.0；玩家命中 0.99 未命中
    [Theory]
    [InlineData(100, 0, AttackKind.Physical)]
    [InlineData(0, 100, AttackKind.Magic)]
    public void Battle_WeakenLowersMonsterAttack(int monsterAt, int monsterMat, AttackKind expectedKind)
    {
        var random = new ScriptedRandom(0.99, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(at: monsterAt, mat: monsterMat), null, [Equip(Weaken, level: 5)], random);

        Assert.Equal(
            new AttackEvent(0, CombatSide.Monster, expectedKind, Hit: true, Critical: false, Damage: 112, ElementMultiplier: 1.0, TargetHpAfter: 888, SpecialAttackName: null),
            AttacksBy(outcome, CombatSide.Monster)[0]);
    }

    // 物暴擊率 lv5（被動、常駐不擲骰）：物理暴擊率 +10。玩家 agi 10 對 10 → 先攻門檻 30、基礎暴擊率物理與魔法都是 10。
    // 擲骰：先攻 0.0；物理時命中 0.0；暴擊 0.145 → 15（物理 15 < 20 暴擊、魔法 15 < 10 不暴擊）；傷害 0.0。
    // 物理暴擊：A = 120，120·2.4 − 50 = 238；魔法 mat 100 → 150
    [Theory]
    [InlineData(AttackKind.Physical, true, 238)]
    [InlineData(AttackKind.Magic, false, 150)]
    public void Battle_KeenEdgeAffectsPhysicalCritOnly(AttackKind kind, bool expectedCritical, int expectedDamage)
    {
        var keenEdge = TestSkills.Battle("keen-edge", "物暴擊率", SkillEffectKind.CritChanceUp, SkillType.Passive);
        double[] rolls = kind == AttackKind.Physical ? [0.0, 0.0, 0.145, 0.0] : [0.0, 0.145, 0.0];
        var player = Player() with { Mat = 100, Agi = 10 };

        var outcome = WogBattle.Run(player, Monster(), kind, [Equip(keenEdge, level: 5, slot: 4)], new ScriptedRandom(0.99, rolls));

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, kind, Hit: true, expectedCritical, expectedDamage, ElementMultiplier: 1.0,
                TargetHpAfter: 1000 - expectedDamage, expectedCritical ? SpecialAttack : null),
            AttacksBy(outcome, CombatSide.Player)[0]);
    }

    // 附加傷 lv5：round(au 10 × 0.4) = 4，物理與魔法每次命中都加；魔連擊 lv1 → 魔法 2 下（0、0.2 秒）。
    // 玩家指定魔法、mat 100 打 mdf 50 = 150 + 4 = 154。擲骰：魔連擊 0.0、附加傷 0.0；先攻 0.0；
    // 兩下各 暴擊 0.99（au 相同，機率 10）、傷害 0.0；怪物命中 0.99 未命中
    [Fact]
    public void Battle_BonusDamageAddsToEveryMagicHit()
    {
        var magicCombo = TestSkills.Battle("magic-combo", "魔連擊", SkillEffectKind.MagicMultiHit);
        var bonusStrike = TestSkills.Battle("bonus-strike", "附加傷", SkillEffectKind.BonusDamage, rounds: 1);
        var random = new ScriptedRandom(0.99, 0.0, 0.0, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99);
        var player = Player() with { At = 0, Mat = 100 };

        var outcome = WogBattle.Run(player, Monster(), AttackKind.Magic, [Equip(magicCombo, level: 1), Equip(bonusStrike, level: 5, slot: 2)], random);

        AssertEventsAt(
            new[]
            {
                new AttackEvent(0, CombatSide.Player, AttackKind.Magic, Hit: true, Critical: false, Damage: 154, ElementMultiplier: 1.0, TargetHpAfter: 846, SpecialAttackName: null),
                new AttackEvent(0.2, CombatSide.Player, AttackKind.Magic, Hit: true, Critical: false, Damage: 154, ElementMultiplier: 1.0, TargetHpAfter: 692, SpecialAttackName: null),
            },
            AttacksBy(outcome, CombatSide.Player).Take(2));
    }

    // 嗜血是常駐被動：只在命中後觸發，而且不擲骰。怪物 150 血。
    // 第 1 回合：先攻 0.0；命中 0.99 未命中 → 不回復；怪物命中 0.99 未命中。
    // 第 2 回合：先攻 0.0；命中 0.0、暴擊 0.99、傷害 0.0 → 150 擊倒；嗜血不擲骰直接回復 15。共 3 + 4 = 7 顆
    [Fact]
    public void Battle_BloodthirstTriggersOnHitWithoutRoll()
    {
        var random = new ScriptedRandom(0.0, 0.0, 0.99, 0.99, 0.0, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(hp: 500), Monster(hp: 150), null, [Equip(Bloodthirst, level: 5, slot: 4)], random);

        Assert.Equal(7, random.Consumed);
        Assert.Equal(15, Assert.Single(outcome.Events.OfType<SkillEvent>()).Heal);
        Assert.Equal((CombatSide?)CombatSide.Player, outcome.Winner);
        Assert.Equal(2, outcome.Rounds);
        Assert.Equal((515, 100), (outcome.Player.Hp, outcome.Player.Sp));
    }

    // 治癒 lv3 = 35，以當下 HP 封頂（不是開戰時的 HP）。怪物 at 60 打 df 50：60·1.2 − 50 = 22。
    // 第 1 回合：滿血不擲治癒；先攻 0.99 怪物先；命中 0.0、暴擊 0.99、傷害 0.0 → 100 → 78；玩家命中 0.99 未命中。
    // 第 2 回合：治癒 0.0 → 發動，回復 min(35, 100 − 78) = 22。之後全是 0.99：治癒不發動、雙方都未命中
    [Fact]
    public void Battle_HealCapsAtCurrentHp()
    {
        var random = new ScriptedRandom(0.99, 0.99, 0.0, 0.99, 0.0, 0.99, 0.0);

        var outcome = WogBattle.Run(Player(hp: 100, hpMax: 100), Monster(at: 60), null, [Equip(Heal, level: 3, chance: 50)], random);

        Assert.Equal(new SkillEvent(0.8, CombatSide.Player, "heal", "治癒", Level: 3, SpAfter: 89, Heal: 22), Assert.Single(outcome.Events.OfType<SkillEvent>()));
        Assert.Equal(100, outcome.Player.Hp);
    }

    // 反擊只減物理傷害。怪物 at 0 < mat 100，攻擊方式 0.0 → 1 ≤ 8 魔法：mat 100 打 mdf 50 = 150，不乘 0.82。
    // 擲骰：先攻 0.99 怪物先；攻擊方式 0.0；暴擊 0.99；傷害 0.0；玩家命中 0.99 未命中
    [Fact]
    public void Battle_CounterDoesNotReduceMagicDamage()
    {
        var random = new ScriptedRandom(0.99, 0.99, 0.0, 0.99, 0.0, 0.99);

        var outcome = WogBattle.Run(Player(), Monster(mat: 100), null, [Equip(Counter, level: 5, slot: 4)], random);

        Assert.Equal(
            new AttackEvent(0, CombatSide.Monster, AttackKind.Magic, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 850, SpecialAttackName: null),
            AttacksBy(outcome, CombatSide.Monster)[0]);
    }

    // 急影 lv5（被動、常駐不擲骰）：agi 20 ×1.05 = 21 > 怪物 20 → 先攻門檻由 30 變 70。
    // 擲骰：先攻 0.5 → 51（≤ 70 玩家先；沒有急影時 > 30 怪物先）
    [Fact]
    public void Battle_FirstStrikeUsesModifiedAgi()
    {
        var swiftShadow = TestSkills.Battle("swift-shadow", "急影", SkillEffectKind.AgiMultiplier, SkillType.Passive);

        var outcome = WogBattle.Run(Player(), Monster() with { Agi = 20 }, null, [Equip(swiftShadow, level: 5, slot: 4)], new ScriptedRandom(0.99, 0.5));

        var first = outcome.Events.OfType<AttackEvent>().First();
        Assert.Equal(CombatSide.Player, first.Attacker);
        Assert.Equal(0, first.AtSeconds);
    }

    // 固定種子的整場戰鬥：數值是加入技能前（a61c9de）的舊程式跑出來的，新舊兩個多載都必須完全相同
    [Fact]
    public void Battle_WithoutSkills_IsUnchanged()
    {
        var player = new CombatantState(900, 900, 50, 50, 40, 10, 20, 15, 18, 12, 1, SpecialAttack, Str: 40, Smart: 10);
        var monster = new CombatantState(1500, 1500, 0, 0, 30, 35, 18, 12, 15, 10, 3, SpecialAttackName: null, Str: 30, Smart: 35);

        var legacy = WogBattle.Run(player, monster, null, new SeededRandom(20261021));
        var withEmptySkills = WogBattle.Run(player, monster, null, [], new SeededRandom(20261021));

        Assert.Equal(legacy.Events, withEmptySkills.Events);
        var attacks = legacy.Events.OfType<AttackEvent>().ToList();
        Assert.Equal(
            (46, (CombatSide?)CombatSide.Monster, 15, 1256, 0, 244, 50),
            (legacy.Events.Count, legacy.Winner, legacy.Rounds, legacy.DamageDealtToMonster, legacy.Player.Hp, legacy.Monster.Hp, legacy.Player.Sp));
        Assert.Equal((30, 3, 1, 13), (attacks.Count, attacks.Count(attack => attack.Critical), attacks.Count(attack => !attack.Hit), attacks.Count(attack => attack.Kind == AttackKind.Magic)));
        Assert.Equal(new DefeatEvent(attacks[^1].AtSeconds, CombatSide.Player), legacy.Events[^1]);
    }

    [Fact]
    public void SkillEvent_SerializesWithSkillDiscriminator()
    {
        BattleEvent skillEvent = new SkillEvent(0, CombatSide.Player, "burst", "爆氣", Level: 1, SpAfter: 10, Heal: 0);

        var json = JsonSerializer.Serialize(skillEvent, JsonSerializerOptions.Web);

        Assert.Contains("\"type\":\"skill\"", json);
    }
}
