using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content 的勳章、捕捉道具、傭兵與勳章所：plan 9 Task 1（docs/research/companion-table.md）
public class CompanionTableTests
{
    private const string Continent = "item-1304", Kingdom = "item-1305", Rainforest = "item-1306", RoyalCity = "item-1307", Silver = "item-2222";

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    [Fact]
    public void Medals_AreTheFiveWogMedals()
    {
        var medals = Content.AllItems.Where(item => item.Category == ItemCategory.Medal).ToList();

        Assert.Equal(
            [(Continent, "大陸勳章"), (Kingdom, "王國勳章"), (Rainforest, "雨林勳章"), (RoyalCity, "王城勳章"), (Silver, "白銀勳章")],
            medals.Select(item => (item.Id, item.Name)));
        Assert.All(medals, medal => Assert.Equal((ConsumableKind.None, false), (medal.Consumable, medal.ShopSellable)));
    }

    // 原版 258 捕捉器、1837 強力捕捉器、2236 魔法捕捉器（倍率見 wog-research-world.md）；魔法捕捉器只能從勳章所兌換
    [Fact]
    public void CaptureItems_UseWogMultipliers()
    {
        var captures = Content.AllItems.Where(item => item.Consumable == ConsumableKind.Capture).ToList();

        Assert.Equal(
            [("item-258", "捕捉器", 1, true), ("item-1837", "強力捕捉器", 8, true), ("item-2236", "魔法捕捉器", 13, false)],
            captures.Select(item => (item.Id, item.Name, item.Amount, item.ShopSellable)));
        Assert.All(captures, item => Assert.Equal(ItemCategory.Consumable, item.Category));
    }

    // 原版 wog_mercenary_main：me_status 1 的 C、B 級不在商店賣，只能從勳章所兌換
    [Fact]
    public void Mercenaries_MatchTable()
    {
        Assert.Equal(
            [
                new MercenaryDefinition("mercenary-1", "F級傭兵", 500, 850, 30, 30, 65, 5),
                new MercenaryDefinition("mercenary-2", "E級傭兵", 100000, 900, 40, 40, 75, 4),
                new MercenaryDefinition("mercenary-3", "D級傭兵", 1000000, 950, 50, 50, 85, 3),
                new MercenaryDefinition("mercenary-4", "C級傭兵", null, 900, 70, 70, 100, 2),
                new MercenaryDefinition("mercenary-5", "B級傭兵", null, 900, 80, 80, 100, 1),
            ],
            Content.AllMercenaries);
    }

    private static ItemQuantity[] Cost(params (string MedalId, int Quantity)[] medals) =>
        medals.Select(medal => new ItemQuantity(medal.MedalId, medal.Quantity)).ToArray();

    // 原版勳章所 142 筆，類型 0 的 2 筆兌換不到；h_1～h_5 是大陸、王國、雨林、王城、白銀勳章
    [Fact]
    public void HonourShop_CostsOnlyMedals()
    {
        var original = Content.HonourShop.Where(exchange => !CustomContent.IsCustom(exchange.Id)).ToList();

        Assert.Equal(140, original.Count);
        Assert.Equal(("honour-2", "mercenary-5"), (original[1].Id, original[1].MercenaryId!));
        Assert.Equal(Cost(("item-1304", 23), ("item-1305", 23)), original[1].Cost);
        Assert.Equal(("honour-142", "item-3361", 3), (original[^1].Id, original[^1].ItemId!, original[^1].Quantity));
        Assert.Equal(
            Cost(("item-1304", 1), ("item-1305", 1), ("item-1306", 1), ("item-1307", 1), ("item-2222", 1)),
            original[^1].Cost);
        Assert.Equal(
            ["item-1304", "item-1305", "item-1306", "item-1307", "item-2222"],
            original.SelectMany(exchange => exchange.Cost).Select(cost => cost.ItemId).Distinct().Order());

        Assert.All(
            Content.HonourShop.SelectMany(exchange => exchange.Cost),
            cost => Assert.Equal(ItemCategory.Medal, Content.GetItem(cost.ItemId).Category));

        // 不在商店賣的傭兵只能從勳章所取得
        Assert.Equal(
            Content.AllMercenaries.Where(mercenary => mercenary.Price is null).Select(mercenary => mercenary.Id),
            Content.HonourShop.Where(exchange => exchange.MercenaryId is not null).Select(exchange => exchange.MercenaryId!));
    }

    // 四把禁區鑰匙各一筆兌換、數量 1
    [Fact]
    public void HonourShop_SellsTheFourKeys()
    {
        var keyExchanges = Content.HonourShop
            .Where(exchange => exchange.ItemId is { } itemId && Content.GetItem(itemId).Category == ItemCategory.Key)
            .ToList();

        Assert.Equal(
            [("honour-3", 17), ("honour-4", 18), ("honour-29", 19), ("honour-101", 20)],
            keyExchanges.Select(exchange => (exchange.Id, Content.GetItem(exchange.ItemId!).Key!.PlaceId)));
        Assert.All(keyExchanges, exchange => Assert.Equal(1, exchange.Quantity));
    }

    // 保留的自訂道具的兌換，改付原版勳章（大＝大陸、國＝王國、雨＝雨林、銀＝白銀）
    private static readonly (string Id, string ItemId, ItemQuantity[] Cost)[] NewConsumableExchanges =
    [
        ("honour-stone-box", "stone-box", Cost((Continent, 1))),
        ("honour-large-stone-box", "large-stone-box", Cost((Silver, 1))),
        ("honour-refine-box", "refine-box", Cost((Kingdom, 1), (Rainforest, 1))),
        ("honour-pet-egg", "pet-egg", Cost((Silver, 3))),
    ];

    [Fact]
    public void HonourShop_SellsTheNewConsumables()
    {
        var exchanges = Content.HonourShop.Where(exchange => NewConsumableExchanges.Any(row => row.Id == exchange.Id)).ToList();

        Assert.Equal(NewConsumableExchanges.Select(row => (row.Id, row.ItemId, 1)), exchanges.Select(exchange => (exchange.Id, exchange.ItemId!, exchange.Quantity)));
        foreach (var (row, exchange) in NewConsumableExchanges.Zip(exchanges))
        {
            Assert.Equal(row.Cost, exchange.Cost);
        }
    }
}
