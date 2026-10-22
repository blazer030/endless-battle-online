using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content 的勳章、捕捉道具、傭兵、勳章所與冠軍種子：plan 9 Task 1（docs/research/companion-table.md）
public class CompanionTableTests
{
    private const string Continent = "continent-medal", Kingdom = "kingdom-medal", Rainforest = "rainforest-medal",
        RoyalCity = "royal-city-medal", Silver = "silver-medal";

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    [Fact]
    public void Medals_AreTheFiveWogMedals()
    {
        var medals = Content.AllItems.Where(item => item.Category == ItemCategory.Medal).ToList();

        Assert.Equal(
            [(Continent, "大陸勳章"), (Kingdom, "王國勳章"), (Rainforest, "雨林勳章"), (RoyalCity, "王城勳章"), (Silver, "白銀勳章")],
            medals.Select(item => (item.Id, item.Name)));
        Assert.All(medals, medal => Assert.Equal(
            (ConsumableKind.None, 0L, 1, false),
            (medal.Consumable, medal.Price, medal.ShopLevel, medal.ShopSellable)));
    }

    // WOG 258 捕獸網、1837 捕獸籠、2236 封印水晶；封印水晶只能從勳章所兌換
    [Fact]
    public void CaptureItems_UseWogMultipliers()
    {
        var captures = Content.AllItems.Where(item => item.Consumable == ConsumableKind.Capture).ToList();

        Assert.Equal(
            [
                ("capture-net", "捕獸網", 1, 200L, 1, true),
                ("capture-cage", "捕獸籠", 8, 3000L, 4, true),
                ("capture-crystal", "封印水晶", 13, 5000L, 6, false),
            ],
            captures.Select(item => (item.Id, item.Name, item.Amount, item.Price, item.ShopLevel, item.ShopSellable)));
        Assert.All(captures, item => Assert.Equal(ItemCategory.Consumable, item.Category));
        Assert.Contains(Content.ItemsForShop(ItemCategory.Consumable, 1), item => item.Id == "capture-net");
        Assert.Contains(Content.ItemsForShop(ItemCategory.Consumable, 4), item => item.Id == "capture-cage");
        Assert.DoesNotContain(Content.ItemsForShop(ItemCategory.Consumable, 6), item => item.Id == "capture-crystal");
    }

    [Fact]
    public void Mercenaries_MatchTable()
    {
        Assert.Equal(
            [
                new MercenaryDefinition("mercenary-f", "F級傭兵", 3000, 100, 30, 30, 65, 5),
                new MercenaryDefinition("mercenary-e", "E級傭兵", 12000, 150, 40, 40, 70, 4),
                new MercenaryDefinition("mercenary-d", "D級傭兵", 40000, 200, 50, 50, 75, 3),
                new MercenaryDefinition("mercenary-c", "C級傭兵", 120000, 300, 60, 60, 80, 2),
                new MercenaryDefinition("mercenary-b", "B級傭兵", null, 400, 70, 70, 85, 1),
                new MercenaryDefinition("mercenary-a", "A級傭兵", null, 500, 80, 80, 90, 1),
            ],
            Content.AllMercenaries);
    }

    private static ItemQuantity[] Cost(params (string MedalId, int Quantity)[] medals) =>
        medals.Select(medal => new ItemQuantity(medal.MedalId, medal.Quantity)).ToArray();

    [Fact]
    public void HonourShop_CostsOnlyMedals()
    {
        var fourMedals = Cost((Continent, 1), (Kingdom, 1), (Rainforest, 1), (RoyalCity, 1));
        (string Id, string? ItemId, string? MercenaryId, ItemQuantity[] Cost)[] expected =
        [
            ("honour-mercenary-b", null, "mercenary-b", Cost((Continent, 3), (Kingdom, 3), (Rainforest, 3), (RoyalCity, 3))),
            ("honour-mercenary-a", null, "mercenary-a", Cost((Silver, 5), (Continent, 3), (Kingdom, 3), (Rainforest, 3), (RoyalCity, 3))),
            ("honour-capture-crystal", "capture-crystal", null, Cost((Silver, 1), (Rainforest, 2))),
            ("honour-attack-stone", "attack-stone", null, Cost((Continent, 2))),
            ("honour-sorcery-stone", "sorcery-stone", null, Cost((Kingdom, 2))),
            ("honour-guard-stone", "guard-stone", null, Cost((Rainforest, 2))),
            ("honour-ward-stone", "ward-stone", null, Cost((RoyalCity, 2))),
            ("honour-vitality-stone", "vitality-stone", null, Cost((Continent, 1), (Rainforest, 1))),
            ("honour-gale-stone", "gale-stone", null, Cost((Kingdom, 1), (RoyalCity, 1))),
            ("honour-chaos-stone", "chaos-stone", null, Cost((Silver, 2))),
            ("honour-prism-stone", "prism-stone", null, Cost((Silver, 2))),
            ("honour-iron-refine-5", "iron-refine-stone-5", null, fourMedals),
            ("honour-arcane-refine-5", "arcane-refine-stone-5", null, fourMedals),
            ("honour-vigor-refine-5", "vigor-refine-stone-5", null, fourMedals),
            ("honour-key-abandoned-cave", "abandoned-cave-key", null, fourMedals),
            ("honour-key-sunset-fort", "sunset-fort-key", null, Cost((Continent, 2), (Kingdom, 2), (Rainforest, 2), (RoyalCity, 2))),
            ("honour-key-still-city", "still-city-key", null, Cost((Silver, 1), (Continent, 2), (Kingdom, 2), (Rainforest, 2), (RoyalCity, 2))),
            ("honour-key-obsidian-temple", "obsidian-temple-key", null, Cost((Silver, 3), (Continent, 2), (Kingdom, 2), (Rainforest, 2), (RoyalCity, 2))),
            .. NewConsumableExchanges.Select(row => (row.Id, (string?)row.ItemId, (string?)null, row.Cost)),
        ];

        Assert.Equal(25, Content.HonourShop.Count);
        Assert.Equal(expected.Length, Content.HonourShop.Count);
        foreach (var (row, exchange) in expected.Zip(Content.HonourShop))
        {
            Assert.Equal((row.Id, row.ItemId, row.MercenaryId, 1), (exchange.Id, exchange.ItemId, exchange.MercenaryId, exchange.Quantity));
            Assert.Equal(row.Cost, exchange.Cost);
        }

        Assert.All(
            Content.HonourShop.SelectMany(exchange => exchange.Cost),
            cost => Assert.Equal(ItemCategory.Medal, Content.GetItem(cost.ItemId).Category));

        // B／A 級傭兵只能從勳章所取得
        Assert.Equal(
            Content.AllMercenaries.Where(mercenary => mercenary.Price is null).Select(mercenary => mercenary.Id),
            Content.HonourShop.Where(exchange => exchange.MercenaryId is not null).Select(exchange => exchange.MercenaryId!));
    }

    // 計畫 10：四把禁區鑰匙各一筆兌換、數量 1
    [Fact]
    public void HonourShop_SellsTheFourKeys()
    {
        var keyExchanges = Content.HonourShop
            .Where(exchange => exchange.ItemId is { } itemId && Content.GetItem(itemId).Category == ItemCategory.Key)
            .ToList();

        Assert.Equal(
            [
                ("honour-key-abandoned-cave", 17), ("honour-key-sunset-fort", 18),
                ("honour-key-still-city", 19), ("honour-key-obsidian-temple", 20),
            ],
            keyExchanges.Select(exchange => (exchange.Id, Content.GetItem(exchange.ItemId!).Key!.PlaceId)));
        Assert.All(keyExchanges, exchange => Assert.Equal(1, exchange.Quantity));
    }

    // 計畫 10 Task 5：寶箱、寵物蛋、心得書、經驗膠囊（大＝大陸、國＝王國、雨＝雨林、城＝王城、銀＝白銀）
    private static readonly (string Id, string ItemId, ItemQuantity[] Cost)[] NewConsumableExchanges =
    [
        ("honour-stone-box", "stone-box", Cost((Continent, 1))),
        ("honour-large-stone-box", "large-stone-box", Cost((Silver, 1))),
        ("honour-refine-box", "refine-box", Cost((Kingdom, 1), (Rainforest, 1))),
        ("honour-pet-egg", "pet-egg", Cost((Silver, 3))),
        ("honour-job-notes", "job-notes", Cost((Continent, 1), (RoyalCity, 1))),
        ("honour-advanced-job-notes", "advanced-job-notes", Cost((Silver, 2))),
        ("honour-exp-capsule", "exp-capsule", Cost((Rainforest, 1), (RoyalCity, 1))),
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

    // 戰士 Lv30 期望值＋商店 4 級裝備估算
    [Fact]
    public void Champion_SeedMatchesTable()
    {
        Assert.Equivalent(
            new ChampionSeat("王都冠軍 格雷德", Level: 30, Element: 1, SpecialAttackName: "王者之劍", Hp: 1400, HpMax: 1400, Sp: 175, SpMax: 175,
                At: 180, Mat: 25, Df: 150, Mdf: 40, Agi: 30, Au: 25),
            Content.SeedChampion,
            strict: true);
    }
}
