using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Equipment;

// 全部卸下：裝備列原樣回到背包（各佔一格），補給品依補給數量併回同種道具
public class UnequippingTests
{
    private static readonly InventoryEntry Sword = TestItems.Entry(100, "sword");
    private static readonly InventoryEntry Supply = TestItems.Entry(101, "supply", 5);

    [Fact]
    public void Unequipping_ReturnAllMergesSupply()
    {
        var bag = new[] { TestItems.Entry(1, "supply", 10) };
        var loadout = new Loadout(new Dictionary<EquipmentSlot, InventoryEntry> { [EquipmentSlot.Weapon] = Sword, [EquipmentSlot.Supply] = Supply }, 5);

        var result = Unequipping.TryReturnAll(bag, loadout, bagBonus: 0, GetItem);

        Assert.NotNull(result);
        Assert.Equal([TestItems.Entry(1, "supply", 15)], result.Supply!.UpdatedEntries);
        Assert.Empty(result.Supply.NewEntries);
    }

    [Fact]
    public void Unequipping_ReturnAllWithoutSupplyHasNothingToMerge()
    {
        var loadout = new Loadout(new Dictionary<EquipmentSlot, InventoryEntry> { [EquipmentSlot.Weapon] = Sword }, 0);

        var result = Unequipping.TryReturnAll([], loadout, bagBonus: 0, GetItem);

        Assert.NotNull(result);
        Assert.Null(result.Supply);
    }

    [Fact]
    public void Unequipping_ReturnAllFailsWhenAnyCategoryFull()
    {
        var swords = Enumerable.Range(1, InventoryRules.SlotsPerCategory).Select(id => TestItems.Entry(id, "sword")).ToList();
        var supplies = Enumerable.Range(1, InventoryRules.SlotsPerCategory).Select(id => TestItems.Entry(id, "supply", InventoryRules.StackLimit)).ToList();
        var both = new Loadout(new Dictionary<EquipmentSlot, InventoryEntry> { [EquipmentSlot.Weapon] = Sword, [EquipmentSlot.Supply] = Supply }, 5);

        Assert.Null(Unequipping.TryReturnAll(swords, both, bagBonus: 0, GetItem));
        Assert.Null(Unequipping.TryReturnAll(supplies, both, bagBonus: 0, GetItem));
        Assert.NotNull(Unequipping.TryReturnAll(supplies, both, bagBonus: 1, GetItem));
    }

    private static ItemDefinition GetItem(string itemId) =>
        itemId == "sword" ? TestItems.Equipment("sword") : TestItems.Consumable("supply");
}
