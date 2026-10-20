using Inkbound.Domain.Combat;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Domain;

public class DamageCalculatorTests
{
    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 100, 50)]
    [InlineData(12, 2, 10)]
    [InlineData(1, 1000, 1)]
    public void NonCriticalDamage_FollowsArmorFormula(int attack, int defense, int expected)
    {
        var (damage, isCritical) = DamageCalculator.Calculate(TestStats.Make(attack: attack), TestStats.Make(defense: defense), new ScriptedRandom(0.5));
        Assert.Equal(expected, damage);
        Assert.False(isCritical);
    }

    [Fact]
    public void CriticalHit_MultipliesDamage()
    {
        var attacker = TestStats.Make(attack: 100, critChance: 0.2, critMultiplier: 1.5);
        var (damage, isCritical) = DamageCalculator.Calculate(attacker, TestStats.Make(defense: 100), new ScriptedRandom(0.99, 0.1));
        Assert.Equal(75, damage);
        Assert.True(isCritical);
    }

    [Fact]
    public void CritRoll_IsAlwaysConsumed()
    {
        var random = new ScriptedRandom(0.99, 0.0);
        DamageCalculator.Calculate(TestStats.Make(critChance: 0), TestStats.Make(), random);
        Assert.Equal(1, random.Consumed);
    }
}
