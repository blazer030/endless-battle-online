namespace EndlessBattle.Domain.Battle;

// 地 1 → 水 2 → 火 3 → 木 4 → 風 5 → 毒 6 → 地：前者剋後者；0 為無屬性
public static class ElementTable
{
    public const double Advantage = 1.2;
    public const double Disadvantage = 0.8;
    public const double Neutral = 1.0;

    public const int ElementCount = 6;

    // 這個屬性攻擊時佔優勢的對象
    public static int StrongAgainst(int element) =>
        Elements().Single(other => Multiplier(element, other) > Neutral);

    // 攻擊這個屬性時佔優勢的對象
    public static int WeakAgainst(int element) =>
        Elements().Single(other => Multiplier(other, element) > Neutral);

    private static IEnumerable<int> Elements() => Enumerable.Range(1, ElementCount);

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
