using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Adventure;

public class DropApplierTests
{
    private static readonly ItemDefinition Supply = TestItems.Consumable("supply");
    private static readonly ItemDefinition Ore = TestItems.Item("ore", ItemCategory.Material);
    private static readonly ItemDefinition Sword = TestItems.Equipment("sword");

    // other-N 是用來塞滿消耗品分類的別種道具
    private static ItemDefinition Find(string id) =>
        id.StartsWith("other-", StringComparison.Ordinal) ? TestItems.Consumable(id) : new[] { Supply, Ore, Sword }.Single(item => item.Id == id);

    // 武器分類已滿 15 格（每格不可堆疊）→ 掉落的武器丟棄；同批的補給品仍有空位 → 收下
    [Fact]
    public void Drops_DiscardWhenBagFull()
    {
        var bag = Enumerable.Range(1, 15).Select(index => TestItems.Entry(index, "sword")).ToList();

        var result = DropApplier.Apply(bag, ["sword", "supply"], Find, bagBonus: 0);

        Assert.Equal([true, false], result.Discarded);
        Assert.Equal(["supply"], result.NewEntries.Select(entry => entry.ItemId));
        Assert.Empty(result.UpdatedEntries);
    }

    // 可堆疊的道具滿格時，仍能補進未滿的堆疊
    [Fact]
    public void Drops_MergeIntoStackWithRoom()
    {
        var bag = Enumerable.Range(1, 14).Select(index => TestItems.Entry(index, $"other-{index}", 99))
            .Append(TestItems.Entry(15, "supply", 98)).ToList();

        var result = DropApplier.Apply(bag, ["supply", "supply"], Find, bagBonus: 0);

        // 第一個補到 99；第二個沒有空格也沒有堆疊空間 → 丟棄
        Assert.Equal([false, true], result.Discarded);
        Assert.Equal([(15L, 99)], result.UpdatedEntries.Select(entry => (entry.Id, entry.Quantity)));
        Assert.Empty(result.NewEntries);
    }

    // 同一批裡連續掉同種道具：新開的格子要能被下一個合併，最後只留一筆 Id 0 的新道具
    [Fact]
    public void Drops_MergeIntoSlotsOpenedInTheSameBatch()
    {
        var result = DropApplier.Apply([], ["supply", "supply", "ore"], Find, bagBonus: 0);

        Assert.Equal([false, false, false], result.Discarded);
        Assert.Equal([("supply", 2), ("ore", 1)], result.NewEntries.Select(entry => (entry.ItemId, entry.Quantity)));
        Assert.All(result.NewEntries, entry => Assert.Equal(0, entry.Id));
    }
}
