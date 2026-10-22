using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Honour;

// 同 TestContent 的 honour-shop.json：礦石 1 個花 test-medal-a ×2；榮譽傭兵花 test-medal-a ×1 ＋ test-medal-b ×1
public class HonourShopTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static readonly Character Hero = TestCharacters.Default(1, "測試者", 1, 1, Now) with { Id = 7 };

    private static readonly HonourExchange Ore = new("test-exchange-ore", "test-ore", null, 1, [new ItemQuantity("test-medal-a", 2)]);

    private static readonly HonourExchange Mercenary = new(
        "test-exchange-mercenary", null, "test-honour-mercenary", 1, [new ItemQuantity("test-medal-a", 1), new ItemQuantity("test-medal-b", 1)]);

    private static readonly Dictionary<string, ItemDefinition> Items = new[]
    {
        TestItems.Item("test-ore", ItemCategory.Material),
        TestItems.Item("stone-remover", ItemCategory.Material),
        TestItems.Item("test-medal-a", ItemCategory.Medal),
        TestItems.Item("test-medal-b", ItemCategory.Medal),
    }.ToDictionary(item => item.Id);

    private static readonly MercenaryDefinition HonourMercenary = new("test-honour-mercenary", "測試榮譽傭兵", null, 5, 100, 100, 100, 1);

    private static HonourOutcome Exchange(HonourExchange? exchange, IReadOnlyList<InventoryEntry> bag, MercenaryContract? current = null) =>
        HonourShop.Exchange(Hero, exchange, bag, current, id => Items[id], _ => HonourMercenary, Now);

    private static List<InventoryEntry> FullMaterials() =>
        Enumerable.Range(100, 15).Select(id => TestItems.Entry(id, "stone-remover", 99)).ToList();

    [Fact]
    public void Exchange_PaysMedalsAndAddsItem()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "test-medal-a", 3)];

        var outcome = Exchange(Ore, bag);

        Assert.Empty(outcome.Reasons);
        Assert.Equal([TestItems.Entry(1, "test-medal-a", 1)], outcome.Payment.Updated);
        Assert.Empty(outcome.Payment.Removed);
        Assert.Equal([TestItems.Entry(0, "test-ore", 1)], outcome.Item!.NewEntries);
        Assert.Equal(ItemCategory.Material, Items[outcome.Item.NewEntries[0].ItemId].Category);
        Assert.Null(outcome.Mercenary);
    }

    // 付掉勳章後才判斷背包：勳章不足時沒有付款，材料分類滿了照樣回報 bagFull
    [Fact]
    public void Exchange_ReasonsInOrder()
    {
        Assert.Equal(["honourUnknown"], Exchange(null, [TestItems.Entry(1, "test-medal-a", 3)]).Reasons);

        var bag = FullMaterials();
        bag.Add(TestItems.Entry(1, "test-medal-a", 1));
        Assert.Equal(["medalsMissing", "bagFull"], Exchange(Ore, bag).Reasons);

        var hired = MercenaryRules.NewContract(new MercenaryDefinition("test-mercenary", "測試傭兵", 100, 3, 50, 100, 50, 2), Now);
        IReadOnlyList<InventoryEntry> medals = [TestItems.Entry(1, "test-medal-a", 1), TestItems.Entry(2, "test-medal-b", 1)];
        Assert.Equal(["mercenaryHired"], Exchange(Mercenary, medals, hired).Reasons);
    }

    // 勳章不足又已有傭兵：兩個原因都回報，勳章不足在前
    [Fact]
    public void Exchange_ReportsMedalsMissingBeforeMercenaryHired()
    {
        var hired = MercenaryRules.NewContract(new MercenaryDefinition("test-mercenary", "測試傭兵", 100, 3, 50, 100, 50, 2), Now);

        var outcome = Exchange(Mercenary, [TestItems.Entry(1, "test-medal-a", 1)], hired);

        Assert.Equal(["medalsMissing", "mercenaryHired"], outcome.Reasons);
        Assert.Null(outcome.Mercenary);
    }

    [Fact]
    public void Exchange_MercenaryStartsResting()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "test-medal-a", 1), TestItems.Entry(2, "test-medal-b", 4)];

        var outcome = Exchange(Mercenary, bag);

        Assert.Empty(outcome.Reasons);
        var contract = outcome.Mercenary!;
        Assert.Equal(("test-honour-mercenary", "測試榮譽傭兵"), (contract.MercenaryId, contract.Name));
        Assert.Equal(5, contract.Remaining);
        Assert.Null(contract.PlaceId);
        Assert.Null(contract.NextFightAt);
        Assert.Equal(Now, contract.HiredAt);
        Assert.Equal([1L], outcome.Payment.Removed);
        Assert.Equal([TestItems.Entry(2, "test-medal-b", 3)], outcome.Payment.Updated);
        Assert.Null(outcome.Item);
    }

    [Fact]
    public void Affordable_NeedsEveryMedal()
    {
        Assert.True(HonourShop.Affordable(Mercenary, new Dictionary<string, int> { ["test-medal-a"] = 1, ["test-medal-b"] = 1 }));
        Assert.False(HonourShop.Affordable(Mercenary, new Dictionary<string, int> { ["test-medal-a"] = 5 }));
        Assert.False(HonourShop.Affordable(Ore, new Dictionary<string, int> { ["test-medal-a"] = 1 }));
    }
}
