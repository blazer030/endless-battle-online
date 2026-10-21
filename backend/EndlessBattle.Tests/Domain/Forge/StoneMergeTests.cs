using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Forge;

// 同類合成：兩顆同種類、同等級的精鍊石必成功合成一顆 +1 級；異類合成留到計畫 10
public class StoneMergeTests
{
    private static readonly ItemDefinition[] Stones =
    [
        .. Enumerable.Range(1, 10).SelectMany(level => new[] { Stone(1, level), Stone(2, level) }),
    ];

    private static ItemDefinition Stone(int kind, int level) =>
        TestItems.Item($"kind{kind}-refine-stone-{level}", ItemCategory.RefineStone) with
        {
            Refine = new RefineStoneSpec(level, kind, null, null, null, null, null, null, null, null, null, null, null),
        };

    private static ItemDefinition? Find(int kind, int level) =>
        Stones.SingleOrDefault(stone => stone.Refine!.Kind == kind && stone.Refine.Level == level);

    [Fact]
    public void Merge_SameKindRaisesLevel()
    {
        Assert.Equal("kind1-refine-stone-4", StoneMerge.SameKindNext(Stone(1, 3), Stone(1, 3), Find)?.Id);
        Assert.Equal("kind2-refine-stone-10", StoneMerge.SameKindNext(Stone(2, 9), Stone(2, 9), Find)?.Id);
        Assert.Null(StoneMerge.SameKindNext(Stone(1, 10), Stone(1, 10), Find));
    }

    [Fact]
    public void Merge_RejectsDifferentKind()
    {
        Assert.Null(StoneMerge.SameKindNext(Stone(1, 3), Stone(2, 3), Find));
        Assert.Null(StoneMerge.SameKindNext(Stone(1, 3), Stone(1, 4), Find));
        Assert.Null(StoneMerge.SameKindNext(Stone(1, 3), TestItems.Item("ore", ItemCategory.Material), Find));
    }

    // 每組 2 顆，組數 1～⌊數量 / 2⌋
    [Fact]
    public void Merge_CheckRejectsBadQuantityAndUnmergeableStones()
    {
        Assert.Empty(StoneMerge.Check(Stone(1, 3), 4, 2, Find));
        Assert.Equal(["invalidQuantity"], StoneMerge.Check(Stone(1, 3), 4, 3, Find));
        Assert.Equal(["invalidQuantity"], StoneMerge.Check(Stone(1, 3), 4, 0, Find));
        Assert.Equal(["invalidQuantity"], StoneMerge.Check(Stone(1, 3), 4, int.MaxValue, Find));
        Assert.Equal(["cannotMerge"], StoneMerge.Check(Stone(1, 10), 2, 1, Find));
        Assert.Equal(["notRefineStone"], StoneMerge.Check(TestItems.Item("ore", ItemCategory.Material), 4, 1, Find));
    }

    // 先判斷這種石頭能不能合成，再看數量：只有 1 顆 Lv10 也回 cannotMerge
    [Fact]
    public void Merge_SingleMaxLevelStoneReportsCannotMerge()
    {
        Assert.Equal(["cannotMerge"], StoneMerge.Check(Stone(1, 10), 1, 1, Find));
        Assert.Equal(["cannotMerge"], StoneMerge.Check(Stone(1, 10), 2, 0, Find));
    }
}
