using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Forge;

public static class StoneMerge
{
    public const int MaxLevel = 10, StonesPerPair = 2;

    // 從同一格 quantity 顆的精鍊石合 pairs 組。原因代碼依序為 notRefineStone、cannotMerge、invalidQuantity，只回第一個
    public static IReadOnlyList<string> Check(ItemDefinition stone, int quantity, int pairs, Func<int, int, ItemDefinition?> find)
    {
        if (stone.Refine is null)
        {
            return ["notRefineStone"];
        }

        if (SameKindNext(stone, stone, find) is null)
        {
            return ["cannotMerge"];
        }

        // 以除法比較，避免組數極大時乘法溢位
        return pairs < 1 || pairs > quantity / StonesPerPair ? ["invalidQuantity"] : [];
    }

    // 同類合成必成功：兩顆同種類、同等級的精鍊石 → 同種類 +1 級。find(kind, level) 找不到或已是 LV10 時回 null
    public static ItemDefinition? SameKindNext(ItemDefinition a, ItemDefinition b, Func<int, int, ItemDefinition?> find)
    {
        if (a.Refine is not { } first || b.Refine is not { } second)
        {
            return null;
        }

        if (first.Kind != second.Kind || first.Level != second.Level || first.Level >= MaxLevel)
        {
            return null;
        }

        return find(first.Kind, first.Level + 1);
    }
}
