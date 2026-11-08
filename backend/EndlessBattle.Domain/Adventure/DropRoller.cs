using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Adventure;

public static class DropRoller
{
    private const double ToprFactor = 0.9;

    // 每項獨立：rand(1, ⌊topr × 0.9⌋) ≤ 1 才掉落，傭兵分母再乘 denominatorFactor；
    // 分母不到 1 的項目一定掉落且不擲骰。依項目順序回傳道具 id
    public static IReadOnlyList<string> Roll(IReadOnlyList<DropEntry> drops, IRandomSource random, double denominatorFactor = 1)
    {
        var dropped = new List<string>();
        foreach (var drop in drops)
        {
            var denominator = (int)Math.Floor(drop.Topr * ToprFactor * denominatorFactor);
            if (denominator < 1 || PhpRandom.Between(random, 1, denominator) <= 1)
            {
                dropped.Add(drop.ItemId);
            }
        }

        return dropped;
    }
}
