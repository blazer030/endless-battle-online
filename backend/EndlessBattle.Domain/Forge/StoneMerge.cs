using EndlessBattle.Domain.Randomness;
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

    // 異類合成：兩格不同種類、同等級（≥ 2）的精鍊石，各扣 pairs 顆。原因代碼依序為 notRefineStone、cannotMerge、invalidQuantity，只回第一個
    public static IReadOnlyList<string> CheckCrossKind(
        ItemDefinition first, int firstQuantity, ItemDefinition second, int secondQuantity, int pairs, IReadOnlyList<ItemDefinition> refineStones)
    {
        if (first.Refine is null || second.Refine is null)
        {
            return ["notRefineStone"];
        }

        if (CrossKindCandidates(first, second, refineStones) is not { Count: > 0 })
        {
            return ["cannotMerge"];
        }

        return pairs < 1 || pairs > Math.Min(firstQuantity, secondQuantity) ? ["invalidQuantity"] : [];
    }

    // 每組獨立擲：候選＝等級 −1、種類不是兩個來源的精鍊石（依內容順序），random.NextInt(候選數)。呼叫前先以 CheckCrossKind 確認
    public static ItemDefinition CrossKindPrevious(
        ItemDefinition first, ItemDefinition second, IReadOnlyList<ItemDefinition> refineStones, IRandomSource random)
    {
        var candidates = CrossKindCandidates(first, second, refineStones)
            ?? throw new InvalidOperationException("CheckCrossKind 已確認可合成");
        return candidates[random.NextInt(candidates.Count)];
    }

    // 不能異類合成時回 null
    private static List<ItemDefinition>? CrossKindCandidates(ItemDefinition first, ItemDefinition second, IReadOnlyList<ItemDefinition> refineStones)
    {
        if (first.Refine is not { } a || second.Refine is not { } b || a.Kind == b.Kind || a.Level != b.Level || a.Level <= 1)
        {
            return null;
        }

        return refineStones
            .Where(stone => stone.Refine is { } refine && refine.Level == a.Level - 1 && refine.Kind != a.Kind && refine.Kind != b.Kind)
            .ToList();
    }
}
