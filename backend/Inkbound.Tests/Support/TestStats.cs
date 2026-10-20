using Inkbound.Domain.Combat;

namespace Inkbound.Tests.Support;

public static class TestStats
{
    public static CombatStats Make(
        int maxHealth = 100,
        int attack = 10,
        int defense = 0,
        double critChance = 0,
        double critMultiplier = 1.5,
        double attacksPerSecond = 1) =>
        new(maxHealth, attack, defense, critChance, critMultiplier, attacksPerSecond);
}
