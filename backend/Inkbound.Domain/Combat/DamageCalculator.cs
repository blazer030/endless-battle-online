using Inkbound.Domain.Randomness;

namespace Inkbound.Domain.Combat;

public static class DamageCalculator
{
    public static (int Damage, bool IsCritical) Calculate(CombatStats attacker, CombatStats defender, IRandomSource random)
    {
        // 防禦的效果遞減：攻防相等時剛好減半
        var rawDamage = (double)attacker.Attack * attacker.Attack / (attacker.Attack + defender.Defense);

        // 每次出手都擲一次會心，讓亂數的消耗順序固定，模擬才能重現
        var isCritical = random.NextDouble() < attacker.CritChance;
        if (isCritical)
        {
            rawDamage *= attacker.CritMultiplier;
        }

        var damage = Math.Max(1, (int)Math.Round(rawDamage, MidpointRounding.AwayFromZero));
        return (damage, isCritical);
    }
}
