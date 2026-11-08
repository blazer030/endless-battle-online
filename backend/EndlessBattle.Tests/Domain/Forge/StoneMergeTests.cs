using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Forge;

// 同類合成：兩顆同種類、同等級的精鍊石必成功合成一顆 +1 級。
// 異類合成：兩顆不同種類、同等級（≥ 2）的精鍊石合成一顆低一級、不是這兩種的精鍊石
public class StoneMergeTests
{
    private static readonly ItemDefinition[] Stones =
    [
        .. Enumerable.Range(1, 10).SelectMany(level => new[] { Stone(1, level), Stone(2, level), Stone(3, level) }),
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

    [Fact]
    public void CheckCrossKind_RejectsSameKind()
    {
        Assert.Equal(["cannotMerge"], StoneMerge.CheckCrossKind(Stone(1, 5), 2, Stone(1, 5), 2, 1, Stones));
    }

    [Fact]
    public void CheckCrossKind_RejectsLevelOne()
    {
        Assert.Equal(["cannotMerge"], StoneMerge.CheckCrossKind(Stone(1, 1), 2, Stone(2, 1), 2, 1, Stones));
    }

    [Fact]
    public void CheckCrossKind_RejectsDifferentLevels()
    {
        Assert.Equal(["cannotMerge"], StoneMerge.CheckCrossKind(Stone(1, 5), 2, Stone(2, 4), 2, 1, Stones));
        Assert.Equal(
            ["notRefineStone"],
            StoneMerge.CheckCrossKind(Stone(1, 5), 2, TestItems.Item("ore", ItemCategory.Material), 2, 1, Stones));
    }

    // 每組各扣一顆：組數 1～兩格數量較小者
    [Fact]
    public void CheckCrossKind_PairsLimitedBySmallerStack()
    {
        Assert.Empty(StoneMerge.CheckCrossKind(Stone(1, 5), 5, Stone(2, 5), 2, 2, Stones));
        Assert.Equal(["invalidQuantity"], StoneMerge.CheckCrossKind(Stone(1, 5), 5, Stone(2, 5), 2, 3, Stones));
        Assert.Equal(["invalidQuantity"], StoneMerge.CheckCrossKind(Stone(1, 5), 5, Stone(2, 5), 2, 0, Stones));
        Assert.Equal(["invalidQuantity"], StoneMerge.CheckCrossKind(Stone(1, 5), 5, Stone(2, 5), 2, int.MaxValue, Stones));
    }

    // 鋼鐵（1）Lv5 + 秘法（2）Lv5 → 候選只剩活力（3）Lv4：NextInt(1)
    [Fact]
    public void CrossKindPrevious_GivesTheThirdKindOneLevelLower()
    {
        var random = new ScriptedRandom(0.99);

        Assert.Equal("kind3-refine-stone-4", StoneMerge.CrossKindPrevious(Stone(1, 5), Stone(2, 5), Stones, random).Id);
        Assert.Equal(1, random.Consumed);
    }
}
