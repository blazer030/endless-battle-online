using EndlessBattle.Domain.Economy;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Economy;

public class DepotRulesTests
{
    private static readonly ItemDefinition Supply = TestItems.Consumable("supply");
    private static readonly ItemDefinition Sword = TestItems.Equipment("sword");

    private static List<InventoryEntry> Filled(int count) =>
        Enumerable.Range(1, count).Select(index => TestItems.Entry(index, $"other-{index}")).ToList();

    [Fact]
    public void Capacity_AddsBonus() => Assert.Equal(25, DepotRules.Capacity(5));

    [Fact]
    public void TryStore_MergesStackables()
    {
        var depot = new[] { TestItems.Entry(1, "supply", 200) };

        var plan = DepotRules.TryStore(depot, Supply, TestItems.Entry(9, "supply", 50), 50, 0);

        Assert.Empty(plan.Reasons);
        Assert.Equal([(1L, 250)], plan.Updated.Select(entry => (entry.Id, entry.Quantity)));
        Assert.Empty(plan.Created);
    }

    [Fact]
    public void TryStore_RejectsOverStackLimit()
    {
        var depot = new[] { TestItems.Entry(1, "supply", 250) };

        Assert.Equal(["stackLimit"], DepotRules.TryStore(depot, Supply, TestItems.Entry(9, "supply", 10), 10, 0).Reasons);
        Assert.Empty(DepotRules.TryStore(depot, Supply, TestItems.Entry(9, "supply", 10), 4, 0).Reasons);
    }

    [Fact]
    public void TryStore_RejectsWhenFull()
    {
        var plan = DepotRules.TryStore(Filled(20), Sword, TestItems.Entry(9, "sword"), 1, 0);

        Assert.Equal(["depotFull"], plan.Reasons);
        Assert.Empty(DepotRules.TryStore(Filled(20), Sword, TestItems.Entry(9, "sword"), 1, 1).Reasons);
    }

    [Fact]
    public void TryStore_MergesIntoExistingStackWhenDepotFull()
    {
        var depot = Filled(19).Append(TestItems.Entry(20, "supply", 10)).ToList();

        var plan = DepotRules.TryStore(depot, Supply, TestItems.Entry(9, "supply", 5), 5, 0);

        Assert.Empty(plan.Reasons);
        Assert.Equal([(20L, 15)], plan.Updated.Select(entry => (entry.Id, entry.Quantity)));
        Assert.Empty(plan.Created);
    }

    [Fact]
    public void TryStore_EquipmentKeepsEnhancements()
    {
        var refined = TestItems.Entry(9, "sword") with { RefineLevel = 2, RefineBonus = StatBonus.Zero with { Attack = 3 }, Holes = 1 };

        var plan = DepotRules.TryStore([], Sword, refined, 1, 0);

        var created = Assert.Single(plan.Created);
        Assert.Equal((0L, 2, 3, 1), (created.Id, created.RefineLevel, created.RefineBonus.Attack, created.Holes));
    }

    [Fact]
    public void TryStore_RejectsBadQuantity()
    {
        Assert.Equal(["invalidQuantity"], DepotRules.TryStore([], Sword, TestItems.Entry(9, "sword"), 2, 0).Reasons);
        Assert.Equal(["invalidQuantity"], DepotRules.TryStore([], Supply, TestItems.Entry(9, "supply", 5), 6, 0).Reasons);
    }

    [Fact]
    public void TryTake_KeepsEnhancementsAndChecksBag()
    {
        var refined = TestItems.Entry(5, "sword") with { RefineLevel = 2, Holes = 1 };

        var plan = DepotRules.TryTake([], Sword, refined, 1, 0);

        Assert.Equal((2, 1), (Assert.Single(plan.Created).RefineLevel, plan.Created[0].Holes));
        Assert.Equal(["bagFull"], DepotRules.TryTake(Filled(15), Sword, refined, 1, 0).Reasons);
    }

    [Fact]
    public void TryTake_StackablesRespectBagLimit()
    {
        var bag = new[] { TestItems.Entry(1, "supply", 90) };

        var plan = DepotRules.TryTake(bag, Supply, TestItems.Entry(5, "supply", 200), 20, 0);

        Assert.Equal([(1L, 99)], plan.Updated.Select(entry => (entry.Id, entry.Quantity)));
        Assert.Equal([11], plan.Created.Select(entry => entry.Quantity));
        Assert.Equal(["bagFull"], DepotRules.TryTake(Filled(15), Supply, TestItems.Entry(5, "supply", 200), 5, 0).Reasons);
        Assert.Equal(["invalidQuantity"], DepotRules.TryTake(bag, Supply, TestItems.Entry(5, "supply", 20), 21, 0).Reasons);
    }
}
