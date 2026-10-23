using EndlessBattle.Domain.Battle;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

// 補給門檻（預設）：HP < 15% 且 SP 未滿、本場已用 < 9 次；喝藥不擲骰，所以腳本與 WogBattleTests 相同
public class WogBattlePotionTests
{
    private static CombatantState Player(int hp, int sp, int hpMax = 100, int spMax = 100, int at = 100, int df = 50) =>
        new(hp, hpMax, sp, spMax, at, Mat: 0, df, Mdf: 50, Agi: 20, Au: 10, Element: 0, "烈焰斬", Str: 100, Smart: 0);

    private static SupplyState Supply(int hp = 50, int sp = 25, int quantity = 5, int hpPercent = 15, int spPercent = 100) =>
        new("test-supply", "測試補給藥劑", hp, sp, quantity) { HpPercent = hpPercent, SpPercent = spPercent };

    // 門檻 100 × 15% = 15：14 才喝，剛好 15 不喝
    [Theory]
    [InlineData(14, true)]
    [InlineData(15, false)]
    public void ShouldUse_FalseWhenHpAtThreshold(int hp, bool expected)
    {
        Assert.Equal(expected, PotionRules.ShouldUse(Player(hp, sp: 50), Supply(hpPercent: 15, spPercent: 100), usedThisFight: 0));
    }

    // SP 填 -1 略過 SP 條件：SP 全滿、HP 10% 也會喝
    [Fact]
    public void ShouldUse_SkipsSpConditionWhenMinusOne()
    {
        Assert.True(PotionRules.ShouldUse(Player(hp: 10, sp: 100), Supply(spPercent: -1), usedThisFight: 0));
    }

    // HP 填 0：hp × 100 < 0 永遠不成立
    [Fact]
    public void ShouldUse_ZeroHpPercentNeverDrinks()
    {
        Assert.False(PotionRules.ShouldUse(Player(hp: 1, sp: 50), Supply(hpPercent: 0), usedThisFight: 0));
    }

    // 門檻都略過時，補給要補得到東西才喝：只補 HP 的補給在滿血時不喝；雙補給在血魔全滿時不喝、SP 沒滿時照喝
    [Theory]
    [InlineData(100, 50, 50, 0, false)]
    [InlineData(100, 100, 50, 25, false)]
    [InlineData(100, 90, 50, 25, true)]
    [InlineData(99, 100, 50, 0, true)]
    public void ShouldUse_SkipsWhenNothingToRecover(int hp, int sp, int supplyHp, int supplySp, bool expected)
    {
        var supply = Supply(hp: supplyHp, sp: supplySp, hpPercent: -1, spPercent: -1);

        Assert.Equal(expected, PotionRules.ShouldUse(Player(hp, sp), supply, usedThisFight: 0));
    }

    [Theory]
    [InlineData(49, true)]
    [InlineData(50, false)]
    public void ShouldUse_HonoursCustomHpPercent(int hp, bool expected)
    {
        Assert.Equal(expected, PotionRules.ShouldUse(Player(hp, sp: 50), Supply(hpPercent: 50), usedThisFight: 0));
    }

    // 擲骰同 WogBattleTests.Battle_PlayerStrikesFirst_WhenRollBelowSeventy：玩家先攻、150 傷害第 1 回合擊倒。
    // 開打 HP 14／100、SP 50／100 → 回合開頭先喝：64／75，剩 4
    [Fact]
    public void Battle_UsesPotionBelowThreshold()
    {
        var outcome = Run(Player(hp: 14, sp: 50), Supply(quantity: 5), new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Equal(
            new BattleEvent[]
            {
                new RoundEvent(0, 1),
                new PotionEvent(0, "test-supply", "測試補給藥劑", Hp: 50, Sp: 25, Remaining: 4),
                new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: false, Damage: 150, ElementMultiplier: 1.0, TargetHpAfter: 0, SpecialAttackName: null),
                new DefeatEvent(0, CombatSide.Monster),
            },
            outcome.Events);
        Assert.Equal((64, 75), (outcome.Player.Hp, outcome.Player.Sp));
        Assert.Equal(Supply(quantity: 4), outcome.Supply);
    }

    // SP 門檻 100%：SP 滿了就不喝，即使 HP 已低於 15%
    [Fact]
    public void Battle_NoPotionWhenSpFull()
    {
        var outcome = Run(Player(hp: 14, sp: 100), Supply(quantity: 5), new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Empty(outcome.Events.OfType<PotionEvent>());
        Assert.Equal(14, outcome.Player.Hp);
        Assert.Equal(Supply(quantity: 5), outcome.Supply);
    }

    [Fact]
    public void Battle_NoPotionWhenQuantityZero()
    {
        var outcome = Run(Player(hp: 14, sp: 50), Supply(quantity: 0), new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Empty(outcome.Events.OfType<PotionEvent>());
        Assert.Equal((14, 50), (outcome.Player.Hp, outcome.Player.Sp));
        Assert.Equal(Supply(quantity: 0), outcome.Supply);
    }

    // 大補給 800／400 喝在 14／100、90／100：各自補到上限
    [Fact]
    public void Battle_PotionCapsAtMax()
    {
        var outcome = Run(Player(hp: 14, sp: 90), Supply(hp: 800, sp: 400), new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Equal((100, 100), (outcome.Player.Hp, outcome.Player.Sp));
    }

    // 事件記實際回復量：HP 100 − 14 = 86、SP 100 − 90 = 10
    [Fact]
    public void Battle_PotionAmountsCappedInEvent()
    {
        var outcome = Run(Player(hp: 14, sp: 90), Supply(hp: 800, sp: 400, quantity: 2), new ScriptedRandom(0.99, 0.695, 0.0, 0.99, 0.5));

        Assert.Equal(new PotionEvent(0, "test-supply", "測試補給藥劑", Hp: 86, Sp: 10, Remaining: 1), outcome.Events.OfType<PotionEvent>().Single());
    }

    // 雙方 df 1000、每下 1 點（擲骰一律 0.0，玩家先攻）：HP 14 → 喝 1 → 15 → 被打 → 14，每回合開頭都符合門檻。
    // 前 9 回合各喝一次；第 10 回合開頭 HP 仍是 14、SP 0，但本場已用滿 9 次 → 不喝
    [Fact]
    public void Battle_PotionStopsAfterNineUses()
    {
        var outcome = Run(Player(hp: 14, sp: 0, at: 1, df: 1000), Supply(hp: 1, sp: 0, quantity: 99), new ScriptedRandom(0.0),
            Monster(hp: 1000, at: 1, df: 1000));

        var potions = outcome.Events.OfType<PotionEvent>().ToList();
        Assert.Equal(Enumerable.Range(0, 9).Select(round => round * BattleTiming.SecondsPerRound), potions.Select(potion => potion.AtSeconds));
        Assert.All(potions, potion => Assert.Equal(1, potion.Hp));
        Assert.Equal(90, potions[^1].Remaining);
        Assert.Equal(90, outcome.Supply!.Quantity);

        var roundTen = outcome.Events.ToList().IndexOf(new RoundEvent(9 * BattleTiming.SecondsPerRound, 10));
        var hpAtRoundTen = outcome.Events.Take(roundTen).OfType<AttackEvent>().Last(attack => attack.Attacker == CombatSide.Monster).TargetHpAfter;
        Assert.Equal(14, hpAtRoundTen);
        Assert.IsNotType<PotionEvent>(outcome.Events[roundTen + 1]);
    }

    // 預設門檻（HP 15／SP 100）的多回合戰鬥：期望值是修正前（計畫 9）的序列，寫死喝藥時刻、回復量、擲骰次數與事件數。
    // 第 2、3 回合開頭 HP 低於 52.5 各喝一瓶，第二瓶後 SP 補滿，第 3 回合倒下
    [Fact]
    public void Potion_DefaultSettingsKeepPlan9Sequence()
    {
        var random = new ScriptedRandom(0.0);

        var outcome = Run(Player(hp: 60, sp: 25, hpMax: 350, spMax: 58, at: 1), Supply(quantity: 20), random, Monster(hp: 1000, at: 60, df: 1000));

        Assert.Equal(
            new (double, int, int, int)[] { (1 * BattleTiming.SecondsPerRound, 50, 25, 19), (2 * BattleTiming.SecondsPerRound, 50, 8, 18) },
            outcome.Events.OfType<PotionEvent>().Select(potion => (potion.AtSeconds, potion.Hp, potion.Sp, potion.Remaining)));
        Assert.Equal((3, 12, 21, 18), (outcome.Rounds, outcome.Events.Count, random.Consumed, outcome.Supply!.Quantity));
    }

    private static CombatantState Monster(int hp = 100, int at = 0, int df = 50) =>
        new(hp, hp, Sp: 0, SpMax: 0, at, Mat: 0, df, Mdf: 50, Agi: 10, Au: 10, Element: 0, SpecialAttackName: null, Str: at, Smart: 0);

    private static BattleOutcome Run(CombatantState player, SupplyState? supply, ScriptedRandom random, CombatantState? monster = null) =>
        WogBattle.Run(player, monster ?? Monster(), playerPreferred: null, skills: [], supply, random);
}
