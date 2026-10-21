using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Progression;

public class RewardFormulasTests
{
    private static readonly MonsterDefinition SnowSprite = new(
        "snow-sprite", "雪精", Level: 3, Hp: 60, Sp: 0, At: 14, Mat: 8, Df: 5, Mdf: 5, Agi: 8, Au: 5, Element: 2, JobExp: 12, Meet: 50);

    // base = (14+8)/2 = 11；rand(11, 14) = NextInt(4) + 11，0.0 → 11
    // plv 1 ≤ 100：11 × 1.3 = 14.3；3 × 14.3 × (3/1) × 1 × 0.18 = 23.166
    // plv 1 ≤ 15：× 2.5 = 57.915；× 2 × fDown 1 = 115.83 → round 116
    [Fact]
    public void Exp_KilledSnowSpriteAtLevelOne()
    {
        var exp = RewardFormulas.Exp(SnowSprite, monsterHpRemaining: 0, playerLevel: 1, killed: true, fDown: 1.0, new ScriptedRandom(0.0));

        Assert.Equal(116, exp);
    }

    // 同上到 × 2.5 = 57.915；× 2 × fDown 0.5 = 57.915 → round 58
    [Fact]
    public void Exp_HalvesWhenStaminaDepleted()
    {
        var exp = RewardFormulas.Exp(SnowSprite, monsterHpRemaining: 0, playerLevel: 1, killed: true, fDown: 0.5, new ScriptedRandom(0.0));

        Assert.Equal(58, exp);
    }

    // 未擊殺時血量比 30/60 = 0.5 乘兩次（WOG get_exp 第 127、130 行；spec §4 亦同）
    // 11 × 1.3 = 14.3；3 × 14.3 × 3 × 0.5 × 0.18 = 11.583；× 0.5 = 5.7915
    // × 2.5 = 14.47875；× 2 × 1 = 28.9575 → round 29
    [Fact]
    public void Exp_UnkilledScalesByRemainingHpRatio()
    {
        var exp = RewardFormulas.Exp(SnowSprite, monsterHpRemaining: 30, playerLevel: 1, killed: false, fDown: 1.0, new ScriptedRandom(0.0));

        Assert.Equal(29, exp);
    }

    // 超過回合上限：呼叫端傳等級 3 × 0.6 = 1.8、血量 round(60/1.5) = 40，未擊殺
    // rand(11, 12.8) 截成 rand(11, 12) = NextInt(2) + 11，0.99 → 12；× 1.3 = 15.6
    // 1.8 × 15.6 × (1.8/1) × (40/60) × 0.18 = 6.06528；× (40/60) = 4.04352
    // × 2.5 = 10.1088；× 2 × 1 = 20.2176 → round 20
    [Fact]
    public void Exp_UsesLevelOverride()
    {
        var exp = RewardFormulas.Exp(SnowSprite, monsterHpRemaining: 40, playerLevel: 1, killed: false, fDown: 1.0, new ScriptedRandom(0.99), levelOverride: 1.8);

        Assert.Equal(20, exp);
    }

    // plv 200 > 100 不乘 1.3、> 15 不乘 2.5；mlv 450 ≥ 450 乘 0.6
    // base = (100+100)/2 = 100；rand(100, 550) = NextInt(451) + 100，0.0 → 100；× 0.6 = 60
    // 450 × 60 × (450/200) × 1 × 0.18 = 10935；× 2 × 1 = 21870
    [Fact]
    public void Exp_AppliesHighLevelModifiers()
    {
        var dragon = SnowSprite with { Level = 450, Hp = 1000, At = 100, Mat = 100 };

        var exp = RewardFormulas.Exp(dragon, monsterHpRemaining: 0, playerLevel: 200, killed: true, fDown: 1.0, new ScriptedRandom(0.0));

        Assert.Equal(21870, exp);
    }

    // rand(3/2.5, 3) 截成 rand(1, 3) = NextInt(3) + 1，0.99 → 3
    // rand((14+8)/3, 60/(14+8) × 0.4) = rand(7.33, 1.09) 截成 rand(7, 1)，對調成 rand(1, 7) = NextInt(7) + 1，0.99 → 7
    // 3 × 7 × 0.4 × 1 = 8.4 → round 8
    [Fact]
    public void Money_SwapsRangeWhenInverted()
    {
        var money = RewardFormulas.Money(SnowSprite, fDown: 1.0, new ScriptedRandom(0.99));

        Assert.Equal(8, money);
    }

    // 同上兩次都取 3、7：3 × 7 × 0.4 × fDown 0.5 = 4.2 → round 4
    [Fact]
    public void Money_HalvesWhenStaminaDepleted()
    {
        var money = RewardFormulas.Money(SnowSprite, fDown: 0.5, new ScriptedRandom(0.99));

        Assert.Equal(4, money);
    }

    // At + Mat = 0 時 WOG 會除以零，這裡把除數當 1：
    // rand(1, 3)，0.99 → 3；rand(0/3, 60/1 × 0.4) = rand(0, 24) = NextInt(25)，0.5 → ⌊12.5⌋ = 12
    // 3 × 12 × 0.4 × 1 = 14.4 → round 14
    [Fact]
    public void Money_TreatsZeroAttackSumAsOne()
    {
        var harmless = SnowSprite with { At = 0, Mat = 0 };

        var money = RewardFormulas.Money(harmless, fDown: 1.0, new ScriptedRandom(0.5, 0.99));

        Assert.Equal(14, money);
    }

    // ⌊13 × 0.5⌋ = ⌊6.5⌋ = 6；⌊13 × 1.0⌋ = 13
    [Theory]
    [InlineData(1.0, 13)]
    [InlineData(0.5, 6)]
    public void JobExp_FloorsAfterFDown(double fDown, long expected)
    {
        Assert.Equal(expected, RewardFormulas.JobExp(SnowSprite with { JobExp = 13 }, fDown));
    }
}
