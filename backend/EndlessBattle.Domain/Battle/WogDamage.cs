using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public static class WogDamage
{
    // WOG fight()：attack 已乘上暴擊倍率，所以是小數
    public static int Fight(
        double attack,
        int defense,
        double elementMultiplier,
        IRandomSource random,
        double defenseMultiplier = 1,
        int bonusDamage = 0)
    {
        var attackValue = Math.Max(attack, 1);
        var defenseValue = Math.Max(defense, 1);

        var damage = (attackValue * (attackValue / defenseValue) - defenseValue) * elementMultiplier;
        if (damage <= 0)
        {
            damage = 1;
        }

        var ratio = damage / attackValue;
        damage += PhpRandom.Between(random, (int)(ratio / 3), (int)(ratio / 2));
        return (int)Math.Round(damage * defenseMultiplier + bonusDamage, MidpointRounding.AwayFromZero);
    }
}
