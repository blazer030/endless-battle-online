namespace EndlessBattle.Domain.Battle;

// 地 1 → 水 2 → 火 3 → 木 4 → 風 5 → 毒 6 → 地：前者剋後者；0 為無屬性
public static class ElementTable
{
    public const double Advantage = 1.2;
    public const double Disadvantage = 0.8;
    public const double Neutral = 1.0;

    public static double Multiplier(int attackerElement, int defenderElement)
    {
        if (attackerElement == 0 || defenderElement == 0)
        {
            return Neutral;
        }

        // 與 WOG s_check(攻方 − 守方) 相同
        return (attackerElement - defenderElement) switch
        {
            -1 or 5 => Advantage,
            1 or -5 => Disadvantage,
            _ => Neutral,
        };
    }
}
