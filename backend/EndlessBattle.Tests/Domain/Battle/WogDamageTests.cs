using EndlessBattle.Domain.Battle;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Battle;

public class WogDamageTests
{
    // d = (100·(100/50) − 50)·1.2 = 180；浮動 rand(⌊180/100/3⌋, ⌊180/100/2⌋) = rand(0, 0) = 0 → 180
    [Fact]
    public void Fight_AttackTwiceDefense_AppliesElementMultiplier()
    {
        Assert.Equal(180, WogDamage.Fight(100, 50, 1.2, new ScriptedRandom(0.5)));
    }

    // d = 10·(10/50) − 50 = −48 ≤ 0 → 1；浮動 rand(0, 0) = 0 → 1
    [Fact]
    public void Fight_AttackBelowDefense_DealsOne()
    {
        Assert.Equal(1, WogDamage.Fight(10, 50, 1, new ScriptedRandom(0.5)));
    }

    // d = 100·(100/10) − 10 = 990；浮動 rand(⌊9.9/3⌋, ⌊9.9/2⌋) = rand(3, 4)，擲骰 0.5 → NextInt(2) = 1 → 4；
    // round(994 + 7) = 1001
    [Fact]
    public void Fight_AddsBonusDamageAfterVariance()
    {
        Assert.Equal(1001, WogDamage.Fight(100, 10, 1, new ScriptedRandom(0.5), bonusDamage: 7));
    }

    // d = 990；rand(3, 4)，擲骰 0.0 → 3 → 993；993 × 0.5 = 496.5，PHP round 遠離零 → 497
    [Fact]
    public void Fight_ScalesByDefenseMultiplier_RoundingHalfAwayFromZero()
    {
        Assert.Equal(497, WogDamage.Fight(100, 10, 1, new ScriptedRandom(0.0), defenseMultiplier: 0.5));
    }

    // D = max(0, 1) = 1：d = 10·10 − 1 = 99；rand(⌊9.9/3⌋, ⌊9.9/2⌋) = rand(3, 4)，擲骰 0.0 → 3 → 102
    [Fact]
    public void Fight_TreatsZeroDefenseAsOne()
    {
        Assert.Equal(102, WogDamage.Fight(10, 0, 1, new ScriptedRandom(0.0)));
    }
}
