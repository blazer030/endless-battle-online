using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Items;

public class InventoryRulesTests
{
    private static readonly ItemDefinition Supply = TestItems.Consumable("supply");

    private static IEnumerable<(long Id, string ItemId, int Quantity)> Shape(IEnumerable<InventoryEntry> entries) =>
        entries.Select(entry => (entry.Id, entry.ItemId, entry.Quantity));

    // ×90 補 9 到 99、×99 不動，剩 21 開新格
    [Fact]
    public void TryAdd_StacksThenOpensSlots()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "supply", 90), TestItems.Entry(2, "supply", 99)];

        var result = InventoryRules.TryAdd(bag, Supply, 30, bagBonus: 0);

        Assert.Equal((30, 0), (result.Accepted, result.Discarded));
        Assert.Equal([(1L, "supply", 99)], Shape(result.UpdatedEntries));
        Assert.Equal([(0L, "supply", 21)], Shape(result.NewEntries));
    }

    // 13 格放滿別的道具、1 格 supply ×95：補 4、第 15 格 99，其餘 197 丟棄
    [Fact]
    public void TryAdd_DiscardsOverflow()
    {
        var bag = Enumerable.Range(1, 13).Select(index => TestItems.Entry(index, $"other-{index}", 99))
            .Append(TestItems.Entry(14, "supply", 95)).ToList();

        var result = InventoryRules.TryAdd(bag, Supply, 300, bagBonus: 0);

        Assert.Equal((103, 197), (result.Accepted, result.Discarded));
        Assert.Equal([(14L, "supply", 99)], Shape(result.UpdatedEntries));
        Assert.Equal([(0L, "supply", 99)], Shape(result.NewEntries));
    }

    [Fact]
    public void TryAdd_RespectsBagBonusForConsumablesOnly()
    {
        Assert.Equal(20, InventoryRules.Capacity(ItemCategory.Consumable, 5));
        Assert.Equal(20, InventoryRules.Capacity(ItemCategory.Stone, 5));
        Assert.Equal(15, InventoryRules.Capacity(ItemCategory.Weapon, 5));
        Assert.Equal(15, InventoryRules.Capacity(ItemCategory.Material, 5));

        var fullOfOthers = Enumerable.Range(1, 15).Select(index => TestItems.Entry(index, $"other-{index}", 99)).ToList();
        Assert.Equal(1, InventoryRules.TryAdd(fullOfOthers, Supply, 1, bagBonus: 5).Accepted);
        Assert.Equal(0, InventoryRules.TryAdd(fullOfOthers, TestItems.Item("ore", ItemCategory.Material), 1, bagBonus: 5).Accepted);
    }

    // 勳章 15 格、不能擴充、可堆疊 99
    [Fact]
    public void Capacity_MedalIgnoresBagBonus()
    {
        Assert.Equal(15, InventoryRules.Capacity(ItemCategory.Medal, 5));

        var medal = TestItems.Item("medal", ItemCategory.Medal);
        var result = InventoryRules.TryAdd([], medal, 120, bagBonus: 5);
        Assert.Equal([99, 21], result.NewEntries.Select(entry => entry.Quantity));
    }

    // 裝備每件一格、數量 1，帶著定義的孔數；不會疊到同種裝備上
    [Fact]
    public void TryAdd_EquipmentNeverStacks()
    {
        var sword = TestItems.Equipment("sword", holes: 2);

        var result = InventoryRules.TryAdd([TestItems.Entry(1, "sword")], sword, 2, bagBonus: 0);

        Assert.Empty(result.UpdatedEntries);
        Assert.Equal([(0L, "sword", 1), (0L, "sword", 1)], Shape(result.NewEntries));
        Assert.All(result.NewEntries, entry => Assert.Equal(2, entry.Holes));
    }
}
