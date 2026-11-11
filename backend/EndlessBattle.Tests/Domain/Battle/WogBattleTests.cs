using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// ScriptedRandom 的擲骰換算：rand(1,100) = ⌊v×100⌋ + 1；rand(1,10) = ⌊v×10⌋ + 1；rand(a,b) = ⌊v×(b−a+1)⌋ + a
public class WogBattleTests
{
    private static CombatantState Player(int hp = 100, int at = 100, int mat = 0, int df = 50, int mdf = 50, int agi = 20, int au = 10, int element = 0) =>
        new(hp, hp, ManaPoints: 0, MaxManaPoints: 0, at, mat, df, mdf, agi, au, element, Strength: at, Intelligence: mat);

    private static CombatantState Monster(int hp = 100, int at = 0, int mat = 0, int df = 50, int mdf = 50, int agi = 10, int au = 10, int element = 0) =>
        new(hp, hp, ManaPoints: 0, MaxManaPoints: 0, at, mat, df, mdf, agi, au, element, Strength: at, Intelligence: mat);

    // 玩家 agi 20 > 怪物 10，先攻門檻 70。
    // 擲骰：先攻 0.695 → 70 ≤ 70 玩家先；命中 0.0 → 1，20/30×100+20 = 86.7 > 1 命中；
    // 暴擊 0.99 → 100，比值 2 → 機率 20，100 < 20 不成立；傷害 rand(0,0) 吃掉 0.5。
    // 傷害 (100·2 − 50)·1 = 150 ≥ 怪物 100 血 → 第 1 回合擊倒
    [Fact]
    public void Battle_PlayerStrikesFirst_WhenRollBelowSeventy()
    {
        var outcome = WogBattle.Run(Player(), Monster(), null, new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
                new DefeatEvent(0, CombatSide.Monster),
            },
            outcome.Events);
        Assert.Equal(CombatSide.Player, outcome.Winner);
        Assert.Equal(1, outcome.Rounds);
        Assert.Equal(150, outcome.DamageDealtToMonster);
        Assert.Equal(0, outcome.Monster.HealthPoints);
        Assert.Equal(100, outcome.Player.HealthPoints);
    }

    // 擲骰：先攻 0.705 → 71 > 70 怪物先；怪物命中 0.99 → 100，10/30×100+20 = 53.3 > 100 不成立 → 未命中；
    // 玩家命中 0.0、暴擊 0.99 不中、傷害 rand(0,0) 吃掉 0.5 → 150，第 1 回合後攻擊倒
    [Fact]
    public void Battle_MonsterStrikesFirst_WhenRollAboveSeventy()
    {
        var outcome = WogBattle.Run(Player(), Monster(), null, new ScriptedRandom(0.99, 0.705, 0.99, 0.0, 0.99, 0.5));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHealthPointsAfter: 100),
                new AttackEvent(0.4, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
                new DefeatEvent(0.4, CombatSide.Monster),
            },
            outcome.Events);
    }

    // 地 1 打水 2 本該 ×1.2，但未命中時不計屬性。
    // 擲骰：先攻 0.0 → 1 玩家先；命中 0.99 → 100，86.7 > 100 不成立 → 未命中，傷害 0
    [Fact]
    public void Battle_PhysicalMiss_DealsZero()
    {
        var outcome = WogBattle.Run(Player(element: 1), Monster(element: 2), null, new ScriptedRandom(0.99, 0.0, 0.99));

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHealthPointsAfter: 100),
            outcome.Events.OfType<AttackEvent>().First());
    }

    // 玩家 agi 1 對 1000，物理命中率只有 20.1%；指定魔法則不擲命中。
    // 擲骰：先攻 0.0 → 1 ≤ 30 玩家先；暴擊 0.99 → 100，au 相同 → 機率 10 不中（若誤擲命中，這顆 100 會變成未命中）；
    // 傷害 rand(0,0) 吃掉 0.5 → (100·2 − 50)·1 = 150，擊倒 100 血的怪物
    [Fact]
    public void Battle_MagicAlwaysHits()
    {
        var outcome = WogBattle.Run(Player(at: 0, mat: 100, agi: 1), Monster(agi: 1000), AttackKind.Magic, new ScriptedRandom(0.99, 0.0, 0.99, 0.5));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Player, AttackKind.Magic, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
                new DefeatEvent(0, CombatSide.Monster),
            },
            outcome.Events);
    }

    // 玩家 agi 90 對 10：比值 9 → 機率 min(20, 90) = 20，倍率 min(10.8, 2) = 2。
    // 擲骰：先攻 0.0 玩家先；命中 0.0；暴擊依參數；傷害擲骰 0.0。
    // 0.185 → 19 < 20 暴擊：A = 200，(200·4 − 50) = 750，rand(⌊3.75/3⌋, ⌊3.75/2⌋) = rand(1,1) = 1 → 751，剩 249
    // 0.195 → 20 < 20 不成立（未封頂的 90 會暴擊）：150 + rand(0,0) → 150，剩 850
    [Theory]
    [InlineData(0.185, true, 751)]
    [InlineData(0.195, false, 150)]
    public void Battle_CritUsesAgiRatio_CappedAtTwenty(double criticalRoll, bool expectedCritical, int expectedDamage)
    {
        var outcome = WogBattle.Run(Player(agi: 90), Monster(hp: 1000), null, new ScriptedRandom(0.99, 0.0, 0.0, criticalRoll, 0.0));

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, expectedCritical, expectedDamage, ElementMultiplier: 1.0,
                TargetHealthPointsAfter: 1000 - expectedDamage),
            outcome.Events.OfType<AttackEvent>().First());
    }

    // 怪物 agi 90 對玩家 10，用與玩家相同的公式（WOG 原程式的反向比值是 bug）：機率 20、倍率 2。
    // 擲骰：先攻 0.99 → 100 > 30 怪物先；命中 0.0；暴擊 0.185 → 19 < 20；傷害擲骰 0.0 → 751，玩家 HP 不低於 0
    [Fact]
    public void Battle_MonsterCritUsesSameFormula()
    {
        var outcome = WogBattle.Run(Player(agi: 10), Monster(at: 100, agi: 90), null, new ScriptedRandom(0.99, 0.99, 0.0, 0.185, 0.0));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Monster, AttackKind.Physical, Hit: true, Critical: true, Damage: 751, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
                new DefeatEvent(0, CombatSide.Player),
            },
            outcome.Events);
        Assert.Equal(CombatSide.Monster, outcome.Winner);
        Assert.Equal(0, outcome.Player.HealthPoints);
        Assert.Equal(0, outcome.DamageDealtToMonster);
    }

    // 雙方 agi 10、at 10 對 df 50，全部擲骰 0.0 → 1：每回合玩家先攻（1 ≤ 30）、必中（70 > 1）、
    // 暴擊（1 < 10，A = 12）但 12·(12/50) − 50 ≤ 0 → 1 點；怪物同樣 1 點。
    // 40 回合各扣 40，雙方 1000 血都還在 → 第 40 回合結束（40 × 0.8 = 32 秒）逾時，累計傷害 40。
    // 部分經驗由 Task 5 依 DamageDealt 計算，這裡只驗事件與傷害
    [Fact]
    public void Battle_EndsAfterFortyRounds_WithPartialExp()
    {
        var outcome = WogBattle.Run(Player(hp: 1000, at: 10, agi: 10), Monster(hp: 1000, at: 10, agi: 10), null, new ScriptedRandom(0.0));

        Assert.Equal(new TimeoutEvent(32, Rounds: 40, DamageDealt: 40), outcome.Events[^1]);
        Assert.Empty(outcome.Events.OfType<DefeatEvent>());
        Assert.Equal(40, outcome.Events.OfType<RoundEvent>().Count());
        Assert.Null(outcome.Winner);
        Assert.Equal(40, outcome.Rounds);
        Assert.Equal(40, outcome.DamageDealtToMonster);
        Assert.Equal(960, outcome.Monster.HealthPoints);
        Assert.Equal(960, outcome.Player.HealthPoints);
    }

    // 同上的 1 點對 1 點局面（全部擲骰 0.0：玩家先攻、必中、暴擊），怪物只有 2 血。
    // 第 r 回合從 (r−1)×0.8 秒開始：先攻者同時出手、後攻者 +0.4；擊倒的同一刻接 DefeatEvent 並結束
    [Fact]
    public void Battle_EventsAreOrderedByRound()
    {
        var outcome = WogBattle.Run(Player(hp: 1000, at: 10, agi: 10), Monster(hp: 2, at: 10, agi: 10), null, new ScriptedRandom(0.0));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: true, Damage: 1, ElementMultiplier: 1.0, TargetHealthPointsAfter: 1),
                new AttackEvent(0.4, CombatSide.Monster, AttackKind.Physical, Hit: true, Critical: true, Damage: 1, ElementMultiplier: 1.0, TargetHealthPointsAfter: 999),
                new RoundEvent(0.8, 2),
                new AttackEvent(0.8, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: true, Damage: 1, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
                new DefeatEvent(0.8, CombatSide.Monster),
            },
            outcome.Events);
        Assert.Equal(2, outcome.Rounds);
    }

    // 逃跑局面：雙方每下 1 點（同上），玩家 HP 20／100、門檻 30%。第 1 回合不逃；
    // 第 1 回合擲 7 次（先攻 1、雙方各命中／暴擊／傷害 3），第 2 回合開頭 HP 19 < 30% → 第 8 次擲逃跑 rand(1,100)
    private static CombatantState Fleeing() => Player(hp: 100, at: 10, agi: 10) with { HealthPoints = 20 };

    private static ScriptedRandom EscapeRoll(double roll) => new(0.0, [.. Enumerable.Repeat(0.0, 7), roll]);

    private static BattleOutcome RunFleeing(IRandomSource random, int escapeHealthPointsPercent) =>
        WogBattle.Run(Fleeing(), Monster(hp: 1000, at: 10, agi: 10), null, [], supply: null, random, pet: null, escapeHealthPointsPercent);

    // 0.095 → rand(1,100) = 10 ≤ 10 成功：這場在第 2 回合開頭結束，之後沒有任何出手
    [Fact]
    public void Battle_EscapesWhenBelowThresholdAndRollSucceeds()
    {
        var random = EscapeRoll(0.095);

        var outcome = RunFleeing(random, escapeHealthPointsPercent: 30);

        var escapeIndex = outcome.Events.ToList().IndexOf(new EscapeEvent(0.8));
        Assert.Equal(outcome.Events.Count - 1, escapeIndex);
        Assert.Equal(new RoundEvent(0.8, 2), outcome.Events[escapeIndex - 1]);
        Assert.True(outcome.Escaped);
        Assert.Null(outcome.Winner);
        Assert.Equal(2, outcome.Rounds);
        Assert.Equal(19, outcome.Player.HealthPoints);
        Assert.Equal(8, random.Consumed);
    }

    // 0.105 → 11 > 10 失敗：第 2 回合照常出手；第 3 回合開頭 HP 18 再擲（0.0 → 1）成功
    [Fact]
    public void Battle_EscapeRollFailsAt11()
    {
        var outcome = RunFleeing(EscapeRoll(0.105), escapeHealthPointsPercent: 30);

        var events = outcome.Events.ToList();
        var roundTwo = events.IndexOf(new RoundEvent(0.8, 2));
        var roundThree = events.IndexOf(new RoundEvent(1.6, 3));
        Assert.Equal(2, events.Skip(roundTwo).Take(roundThree - roundTwo).OfType<AttackEvent>().Count());
        Assert.Equal(new EscapeEvent(1.6), events.OfType<EscapeEvent>().Single());
        Assert.True(outcome.Escaped);
        Assert.Equal(3, outcome.Rounds);
    }

    // 第 1 回合發動逃跑率 +5（發動骰 0.0 → 1 ≤ 100），之後同上的 7 次；第 2 回合開頭擲 0.145 → 15：
    // 持續 2 回合時加成還在（15 ≤ 15）第 2 回合就逃；持續 1 回合時第 2 回合開頭已失效（15 > 10），第 3 回合才逃
    [Theory]
    [InlineData(2, 0.8)]
    [InlineData(1, 1.6)]
    public void Battle_EscapeBonusEndsWithItsRounds(int rounds, double escapeAt)
    {
        var smokeScreen = new SkillDefinition("smoke-screen", "煙遁", SkillType.Active, SkillTiming.BeforeAttack, "說明",
        [
            new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 100)], [],
                [new StatChangeEffect(SkillTarget.Caster, SkillStat.EscapeRate, null, 5, null, rounds)]),
        ]);
        var random = new ScriptedRandom(0.0, [.. Enumerable.Repeat(0.0, 8), 0.145]);

        var outcome = WogBattle.Run(
            Fleeing(), Monster(hp: 1000, at: 10, agi: 10), null, [new EquippedSkill(smokeScreen, 1, 1)], supply: null, random, pet: null, escapeHealthPointsPercent: 30);

        Assert.Equal(new EscapeEvent(escapeAt), outcome.Events.OfType<EscapeEvent>().Single());
    }

    // 門檻 0 不逃也不擲骰：同上的局面每回合固定擲 7 次，玩家 20 血每回合扣 1，第 20 回合倒下。
    // 期望值寫死（計畫 9 的序列）：20 回合 × 7 = 140 次；事件 20 × (回合 + 雙方出手) + 倒下 = 61
    [Fact]
    public void Battle_NoEscapeRollWhenThresholdZero()
    {
        var random = new ScriptedRandom(0.0);

        var outcome = RunFleeing(random, escapeHealthPointsPercent: 0);

        Assert.Equal(20, outcome.Rounds);
        Assert.Equal(61, outcome.Events.Count);
        Assert.Equal(140, random.Consumed);
        Assert.Equal(new DefeatEvent(19 * BattleTiming.SecondsPerRound + 0.4, CombatSide.Player), outcome.Events[^1]);
        Assert.Empty(outcome.Events.OfType<EscapeEvent>());
        Assert.False(outcome.Escaped);
    }

    // HP 100／100、門檻 30：每回合只扣 1，40 回合都在門檻之上 → 不擲逃跑骰，40 × 7 = 280 次後超限
    [Fact]
    public void Battle_NoEscapeWhileHpAboveThreshold()
    {
        var random = new ScriptedRandom(0.0);

        var outcome = WogBattle.Run(
            Fleeing() with { HealthPoints = 100 }, Monster(hp: 1000, at: 10, agi: 10), null, [], supply: null, random, pet: null, escapeHealthPointsPercent: 30);

        Assert.Empty(outcome.Events.OfType<EscapeEvent>());
        Assert.False(outcome.Escaped);
        Assert.Equal(40, outcome.Rounds);
        Assert.Equal(60, outcome.Player.HealthPoints);
        Assert.Equal(280, random.Consumed);
    }

    // 未指定攻擊方式且 at 50 < mat 100：開戰時擲一次 rand(1,10)，≤ 8 魔法、否則物理（WOG m_type，80/20），整場沿用。
    // 擲骰：方式依參數（0.75 → 8、0.85 → 9）；先攻 0.0 玩家先；
    // 魔法：暴擊 0.99 不中、傷害 rand(0,0) 吃掉 0.5 → (100·2 − 50) = 150；物理：命中 0.99 → 100，86.7 > 100 不成立 → 未命中。
    // 之後擲骰都是 0.99（rand(1,10) = 10），若每回合重擲就會變成物理
    [Theory]
    [InlineData(0.75, AttackKind.Magic, true, 150)]
    [InlineData(0.85, AttackKind.Physical, false, 0)]
    public void Battle_PlayerWithHigherMat_ChoosesKindOncePerBattle(double kindRoll, AttackKind expectedKind, bool expectedHit, int expectedDamage)
    {
        var outcome = WogBattle.Run(Player(at: 50, mat: 100), Monster(hp: 1000), null, new ScriptedRandom(0.99, kindRoll, 0.0, 0.99, 0.5));

        var playerAttacks = outcome.Events.OfType<AttackEvent>().Where(attack => attack.Attacker == CombatSide.Player).ToList();
        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, expectedKind, expectedHit, Critical: false, expectedDamage, ElementMultiplier: 1.0, TargetHealthPointsAfter: 1000 - expectedDamage),
            playerAttacks[0]);
        Assert.True(playerAttacks.Count > 1);
        Assert.All(playerAttacks, attack => Assert.Equal(expectedKind, attack.Kind));
    }

    // 怪物 at 0 < mat 100：每次出手前擲 m_type，rand(1,10) ≤ 8 魔法、否則物理。
    // 第 1 回合：先攻 0.99 → 100 > 70 怪物先；方式 0.85 → 9 物理；命中 0.99 → 100，53.3 > 100 不成立 → 未命中；
    //           玩家命中 0.99 → 未命中。
    // 第 2 回合：先攻 0.99 怪物先；方式 0.75 → 8 魔法；暴擊 0.99 不中；傷害 rand(0,0) 吃掉 0.5 → (100·2 − 50) = 150，玩家 100 血倒下
    [Fact]
    public void Battle_MonsterWithHigherMat_RollsKindEachRound()
    {
        var outcome = WogBattle.Run(Player(), Monster(mat: 100), null, new ScriptedRandom(0.99, 0.99, 0.85, 0.99, 0.99, 0.99, 0.75, 0.99, 0.5));

        var monsterAttacks = outcome.Events.OfType<AttackEvent>().Where(attack => attack.Attacker == CombatSide.Monster).ToList();
        Assert.Equal(
            new[]
            {
                new AttackEvent(0, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0, TargetHealthPointsAfter: 100),
                new AttackEvent(0.8, CombatSide.Monster, AttackKind.Magic, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0),
            },
            monsterAttacks);
        Assert.Equal(new DefeatEvent(0.8, CombatSide.Player), outcome.Events[^1]);
    }

    // 地 1 打水 2 → ×1.2。擲骰：先攻 0.0 玩家先；命中 0.0；暴擊 0.99 不中；傷害 rand(0,0) 吃掉 0.5。
    // (100·2 − 50)·1.2 = 180，怪物 1000 → 820
    [Fact]
    public void Battle_HitAppliesElementMultiplier()
    {
        var outcome = WogBattle.Run(Player(element: 1), Monster(hp: 1000, element: 2), null, new ScriptedRandom(0.99, 0.0, 0.0, 0.99, 0.5));

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 180, ElementMultiplier: 1.2, TargetHealthPointsAfter: 820),
            outcome.Events.OfType<AttackEvent>().First());
    }

    // 命中：agi/(agi+敵agi)×100 + 20 > rand(1,100)。擲骰：先攻 0.0 → 1 玩家先；命中依參數；暴擊 0.99 → 100 不中；傷害 rand(0,0) 吃掉 0.5。
    // agi 20 對 10：20/30×100 + 20 = 86.67 → 80（0.795）、86（0.855）命中，87（0.865）未命中（拿掉 +20 只剩 66.67，80 會落空）。
    // agi 30 對 10：30/40×100 + 20 = 95 整數 → 94（0.935）命中，95（0.945）因為是 > 而非 >= 未命中。
    // 命中傷害 (100·2 − 50) = 150（agi 30 時暴擊率 20、100 仍不中），怪物 1000 → 850
    [Theory]
    [InlineData(20, 0.795, true)]
    [InlineData(20, 0.855, true)]
    [InlineData(20, 0.865, false)]
    [InlineData(30, 0.935, true)]
    [InlineData(30, 0.945, false)]
    public void Battle_PhysicalHit_UsesAgiRatioPlusTwenty(int playerAgi, double hitRoll, bool expectedHit)
    {
        var outcome = WogBattle.Run(Player(agi: playerAgi), Monster(hp: 1000, agi: 10), null, new ScriptedRandom(0.99, 0.0, hitRoll, 0.99, 0.5));

        var damage = expectedHit ? 150 : 0;
        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Physical, expectedHit, Critical: false, damage, ElementMultiplier: 1.0, TargetHealthPointsAfter: 1000 - damage),
            outcome.Events.OfType<AttackEvent>().First());
    }

    // agi 相等（10 對 10）不算玩家較高 → 先攻門檻 30。先攻 0.305 → 31 > 30 → 怪物先出手
    [Fact]
    public void Battle_MonsterStrikesFirst_WhenAgiEqualAndRollAboveThirty()
    {
        var outcome = WogBattle.Run(Player(agi: 10), Monster(agi: 10), null, new ScriptedRandom(0.99, 0.305));

        var first = outcome.Events.OfType<AttackEvent>().First();
        Assert.Equal(CombatSide.Monster, first.Attacker);
        Assert.Equal(0, first.AtSeconds);
    }

    // agi 相等，先攻 0.295 → 30 ≤ 30 → 玩家先出手
    [Fact]
    public void Battle_PlayerStrikesFirst_WhenAgiEqualAndRollAtThirty()
    {
        var outcome = WogBattle.Run(Player(agi: 10), Monster(agi: 10), null, new ScriptedRandom(0.99, 0.295));

        var first = outcome.Events.OfType<AttackEvent>().First();
        Assert.Equal(CombatSide.Player, first.Attacker);
        Assert.Equal(0, first.AtSeconds);
    }

    // 魔法暴擊比的是 au：agi 10 對 10（若誤用 agi → 機率 10），au 90 對 10 → 比值 9，機率 20、倍率 2。
    // 擲骰：先攻 0.0 → 1 ≤ 30 玩家先；暴擊 0.185 → 19 < 20；傷害擲骰 0.0。
    // A = 200：(200·4 − 50) = 750，rand(⌊3.75/3⌋, ⌊3.75/2⌋) = rand(1,1) = 1 → 751，怪物 1000 → 249
    [Fact]
    public void Battle_MagicCritUsesAuRatio()
    {
        var outcome = WogBattle.Run(Player(at: 0, mat: 100, agi: 10, au: 90), Monster(hp: 1000, agi: 10, au: 10), AttackKind.Magic, new ScriptedRandom(0.99, 0.0, 0.185, 0.0));

        Assert.Equal(
            new AttackEvent(0, CombatSide.Player, AttackKind.Magic, Hit: true, Critical: true, Damage: 751, ElementMultiplier: 1.0, TargetHealthPointsAfter: 249),
            outcome.Events.OfType<AttackEvent>().First());
    }
}
