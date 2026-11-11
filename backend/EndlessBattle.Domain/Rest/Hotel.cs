using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Rest;

public sealed record HotelResult(Character Character, long Cost, bool ExperiencePenalty, double WaitSeconds);

public static class Hotel
{
    public const int FreeLevelMax = 100;

    public static HotelResult Rest(Character c) => Rest(c, EquipmentBonus.Zero);

    // 回滿到含裝備的上限
    public static HotelResult Rest(Character c, EquipmentBonus equipment)
    {
        var stats = DerivedStats.From(c.Base, equipment);
        var wait = c.HealthPoints <= 0 ? BattleTiming.RestWaitWhenDeadSeconds : BattleTiming.RestWaitSeconds;
        var healed = c with { HealthPoints = stats.MaxHealthPoints, ManaPoints = stats.MaxManaPoints };

        if (c.Level <= FreeLevelMax)
        {
            return new HotelResult(healed, 0, false, wait);
        }

        // healthPoints 夾回上限，缺血比例才不會變負（不變量：係數 ≥ 0）
        var cost = Math.Max(
            (long)Math.Round(10.0 * c.Level * Coefficient(Math.Min(c.HealthPoints, stats.MaxHealthPoints), stats.MaxHealthPoints), MidpointRounding.AwayFromZero),
            10L * c.Level);

        return c.Money >= cost
            ? new HotelResult(healed with { Money = c.Money - cost }, cost, false, wait)
            : new HotelResult(healed with { Experience = (long)Math.Round(c.Experience * 0.8, MidpointRounding.AwayFromZero) }, 0, true, wait);
    }

    private static double Coefficient(int healthPoints, int maxHealthPoints)
    {
        if (healthPoints <= 0)
        {
            return 2.1;
        }

        return maxHealthPoints / 10.0 > healthPoints ? 1.8 : 1.4 * (maxHealthPoints - healthPoints) / maxHealthPoints;
    }
}
