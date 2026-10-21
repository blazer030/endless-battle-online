using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Forge;

// 每次獨立擲 r = rand(1, 100)，r ≤ 成功率即成功；材料不論成敗都扣，金錢只算成功的次數
public class SynthesisTests
{
    private static readonly Recipe OreSword = new("forge-sword", "sword", 1, [("ore", 3)], 70);

    private static double RollOf(int value) => (value - 0.5) / 100;

    [Fact]
    public void Synthesis_ConsumesMaterialsEvenOnFailure()
    {
        var bag = new[] { TestItems.Entry(1, "ore", 6) };

        var outcome = Synthesis.Attempt(OreSword, 2, bag, new ScriptedRandom(RollOf(100)));

        Assert.Equal(0, outcome.Successes);
        Assert.Equal([("ore", 6)], outcome.MaterialsConsumed);
        Assert.Equal(0, outcome.Cost);
    }

    // 70 剛好等於成功率也算成功
    [Fact]
    public void Synthesis_ChargesOnlySuccesses()
    {
        var bag = new[] { TestItems.Entry(1, "ore", 9) };

        var outcome = Synthesis.Attempt(OreSword, 3, bag, new ScriptedRandom(0.0, RollOf(1), RollOf(71), RollOf(70)));

        Assert.Equal(2, outcome.Successes);
        Assert.Equal([("ore", 9)], outcome.MaterialsConsumed);
        Assert.Equal(2 * Synthesis.CostPerAttempt, outcome.Cost);
    }

    [Fact]
    public void Synthesis_RollsOncePerAttempt()
    {
        var random = new ScriptedRandom(RollOf(1));

        var outcome = Synthesis.Attempt(OreSword, 4, [TestItems.Entry(1, "ore", 12)], random);

        Assert.Equal(4, outcome.Successes);
        Assert.Equal(4, random.Consumed);
    }

    // 材料可以分散在多格；不夠時 Check 回 missingMaterials，Attempt 直接丟例外
    [Fact]
    public void Synthesis_RejectsMissingMaterials()
    {
        var bag = new[] { TestItems.Entry(1, "ore", 2), TestItems.Entry(2, "ore", 3) };

        Assert.Empty(Check(OreSword, 1, bag));
        Assert.Equal(["missingMaterials"], Check(OreSword, 2, bag));
        Assert.Equal(["missingMaterials"], Check(OreSword, int.MaxValue, bag));
        Assert.Equal(["missingMaterials"], Check(OreSword, 1, [TestItems.Entry(1, "dust", 9)]));
        Assert.Throws<InvalidOperationException>(() => Synthesis.Attempt(OreSword, 2, bag, new ScriptedRandom(0.0)));
    }

    [Fact]
    public void Synthesis_RejectsZeroAttempts()
    {
        var bag = new[] { TestItems.Entry(1, "ore", 9) };

        Assert.Equal(["invalidQuantity"], Check(OreSword, 0, bag));
        Assert.Equal(["invalidQuantity"], Check(OreSword, -1, bag));
        Assert.Throws<InvalidOperationException>(() => Synthesis.Attempt(OreSword, 0, bag, new ScriptedRandom(0.0)));
    }

    // 金錢要夠全部成功的花費；背包以全部成功估算、扣掉材料之後的格數
    [Fact]
    public void Synthesis_CheckRequiresMoneyAndRoomForAllSuccesses()
    {
        var ore = new[] { TestItems.Entry(1, "ore", 6) };
        var swords = Enumerable.Range(10, InventoryRules.SlotsPerCategory - 1).Select(id => TestItems.Entry(id, "sword")).ToList();

        Assert.Equal(["notEnoughMoney"], Check(OreSword, 2, ore, money: 1999));
        Assert.Empty(Check(OreSword, 1, [.. ore, .. swords]));
        Assert.Equal(["bagFull"], Check(OreSword, 2, [.. ore, .. swords]));
    }

    [Fact]
    public void Synthesis_TakesMaterialsInBagOrder()
    {
        var bag = new[] { TestItems.Entry(1, "ore", 2), TestItems.Entry(2, "dust", 5), TestItems.Entry(3, "ore", 9) };

        var changed = Synthesis.TakeMaterials(bag, [("ore", 6), ("dust", 1)]);

        Assert.Equal([TestItems.Entry(1, "ore", 0), TestItems.Entry(3, "ore", 5), TestItems.Entry(2, "dust", 4)], changed);
    }

    private static IReadOnlyList<string> Check(Recipe recipe, int attempts, IReadOnlyList<InventoryEntry> bag, long money = 100_000) =>
        Synthesis.Check(recipe, attempts, bag, money, bagBonus: 0, GetItem);

    private static ItemDefinition GetItem(string itemId) =>
        itemId == "sword" ? TestItems.Equipment("sword") : TestItems.Item(itemId, ItemCategory.Material);
}
