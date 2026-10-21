using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Rest;

public sealed record HotelResult(Character Character, long Cost, bool ExpPenalty, double WaitSeconds);

public static class Hotel
{
    public const int FreeLevelMax = 100;

    public static HotelResult Rest(Character c)
    {
        var stats = DerivedStats.From(c.Base);
        var wait = c.Hp <= 0 ? BattleTiming.RestWaitWhenDeadSeconds : BattleTiming.RestWaitSeconds;
        var healed = c with { Hp = stats.HpMax, Sp = stats.SpMax };

        if (c.Level <= FreeLevelMax)
        {
            return new HotelResult(healed, 0, false, wait);
        }

        // hp 夾回上限，缺血比例才不會變負（不變量：係數 ≥ 0）
        var cost = Math.Max(
            (long)Math.Round(10.0 * c.Level * Coefficient(Math.Min(c.Hp, stats.HpMax), stats.HpMax), MidpointRounding.AwayFromZero),
            10L * c.Level);

        return c.Money >= cost
            ? new HotelResult(healed with { Money = c.Money - cost }, cost, false, wait)
            : new HotelResult(healed with { Exp = (long)Math.Round(c.Exp * 0.8, MidpointRounding.AwayFromZero) }, 0, true, wait);
    }

    private static double Coefficient(int hp, int hpMax)
    {
        if (hp <= 0)
        {
            return 2.1;
        }

        return hpMax / 10.0 > hp ? 1.8 : 1.4 * (hpMax - hp) / hpMax;
    }
}
