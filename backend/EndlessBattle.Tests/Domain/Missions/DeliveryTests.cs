using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Missions;

public class DeliveryTests
{
    [Fact]
    public void Plan_TakesSmallerStacksFirst()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(2, "test-ore", 2), TestItems.Entry(1, "test-ore", 2)];

        var plan = Delivery.Plan(bag, [new ItemQuantity("test-ore", 3)]);

        Assert.Equal([1L], plan.Removed);
        Assert.Equal([TestItems.Entry(2, "test-ore", 1)], plan.Updated);
        Assert.Empty(plan.Missing);
    }

    [Fact]
    public void Plan_TakesSmallerQuantityBeforeLowerId()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "test-ore", 5), TestItems.Entry(2, "test-ore", 1)];

        var plan = Delivery.Plan(bag, [new ItemQuantity("test-ore", 1)]);

        Assert.Equal([2L], plan.Removed);
        Assert.Empty(plan.Updated);
    }

    // 交出的裝備連同精鍊與鑲嵌一起消失，所以先拿未強化的
    [Fact]
    public void Plan_PrefersPlainEquipment()
    {
        var refined = TestItems.Entry(5, "test-sword") with { RefineLevel = 3 };
        IReadOnlyList<InventoryEntry> bag = [refined, TestItems.Entry(9, "test-sword")];

        var plan = Delivery.Plan(bag, [new ItemQuantity("test-sword", 1)]);

        Assert.Equal([9L], plan.Removed);
        Assert.Empty(plan.Updated);
    }

    [Fact]
    public void Plan_ReportsMissing()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "test-ore", 2), TestItems.Entry(2, "test-sword")];

        var plan = Delivery.Plan(bag, [new ItemQuantity("test-ore", 3), new ItemQuantity("test-sword", 1)]);

        Assert.Equal(["test-ore"], plan.Missing);
    }

    [Fact]
    public void After_AppliesPlanToBag()
    {
        IReadOnlyList<InventoryEntry> bag =
            [TestItems.Entry(1, "test-ore", 2), TestItems.Entry(2, "test-ore", 4), TestItems.Entry(3, "test-sword")];

        var after = Delivery.After(bag, Delivery.Plan(bag, [new ItemQuantity("test-ore", 3)]));

        Assert.Equal([TestItems.Entry(2, "test-ore", 3), TestItems.Entry(3, "test-sword")], after);
    }
}
