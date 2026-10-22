using System.Text.RegularExpressions;
using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 backend/content/items.json、recipes.json：plan 7 Global Constraints 的內容數值
public partial class ItemTableTests
{
    private const int ShopLevels = 7;

    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static readonly ItemCategory[] EquipmentCategories =
    [
        ItemCategory.Weapon, ItemCategory.Head, ItemCategory.Body, ItemCategory.Hand, ItemCategory.Foot, ItemCategory.Accessory,
    ];

    // 武器 1、頭 1.5、身 4、手 2、腳 2
    private static readonly Dictionary<ItemCategory, double> MainStatFactor = new()
    {
        [ItemCategory.Weapon] = 8,
        [ItemCategory.Head] = 1.5,
        [ItemCategory.Body] = 4,
        [ItemCategory.Hand] = 2,
        [ItemCategory.Foot] = 2,
    };

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();

    [GeneratedRegex(@"\p{IsCJKUnifiedIdeographs}")]
    private static partial Regex ChineseCharacter();

    private static IEnumerable<ItemDefinition> Equipment => Content.AllItems.Where(item => item.IsEquipment);

    private static int MainStat(ItemCategory category, int level) =>
        (int)Math.Round(MainStatFactor[category] * Math.Pow(level, 1.8), MidpointRounding.AwayFromZero);

    private static int[] BaseStats(StatBonus bonus) =>
        [bonus.Str, bonus.Smart, bonus.Life, bonus.Vit, bonus.Agi, bonus.Au, bonus.Be];

    private static int[] Requirements(StatRequirement7 requires) => [requires.Str, requires.Smart, requires.Agi, requires.Au];

    [Fact]
    public void Items_LoadTable()
    {
        Assert.All(Content.AllItems, item =>
        {
            Assert.Matches(KebabCase(), item.Id);
            Assert.Matches(ChineseCharacter(), item.Name);
        });

        Assert.InRange(Equipment.Count(), 70, int.MaxValue);
        Assert.Equal(84, Equipment.Count());
        foreach (var category in EquipmentCategories)
        {
            var previousRequirement = 0;
            for (var level = 1; level <= ShopLevels; level++)
            {
                var onShelf = Content.ItemsForShop(category, level);
                Assert.True(onShelf.Count >= 2, $"{category} 第 {level} 級只有 {onShelf.Count} 件");
                Assert.All(onShelf, item => Assert.Equal(200L * level * level * level, item.Price));

                // 需求單調遞增：這一級最低的需求高於上一級最高的需求
                var requirements = onShelf.Select(item => Requirements(item.Requires).Sum()).ToList();
                Assert.True(requirements.Min() > previousRequirement, $"{category} 第 {level} 級需求沒有高於前一級");
                previousRequirement = requirements.Max();
            }
        }

        Assert.All(Equipment, item => Assert.True(item.ShopSellable, $"{item.Id} 應在商店販賣"));

        var supplies = Content.AllItems.Where(item => item.Consumable == ConsumableKind.Supply).ToList();
        Assert.Equal(
            [(50, 25, 20L), (200, 100, 80L), (800, 400, 300L)],
            supplies.Select(item => (item.SupplyHp, item.SupplySp, item.Price)));

        var statPotions = Content.AllItems.Where(item => item.Consumable == ConsumableKind.StatPotion).ToList();
        Assert.Equal(7, statPotions.Count);
        Assert.All(statPotions, potion => Assert.Equal(1, BaseStats(potion.Bonus).Sum()));
        Assert.Equal(Enumerable.Repeat(1, 7), Enumerable.Range(0, 7).Select(index => statPotions.Count(potion => BaseStats(potion.Bonus)[index] == 1)));

        Assert.Equal(20, Content.AllItems.Single(item => item.Consumable == ConsumableKind.StaminaPotion).Amount);
        Assert.Equal(5, Content.AllItems.Single(item => item.Consumable == ConsumableKind.BagExpansion).Amount);
        Assert.Equal(5, Content.AllItems.Single(item => item.Consumable == ConsumableKind.DepotExpansion).Amount);

        var refineStones = Content.AllItems.Where(item => item.Category == ItemCategory.RefineStone).Select(item => item.Refine!).ToList();
        Assert.Equal(30, refineStones.Count);
        Assert.Equal(
            Enumerable.Range(1, 10).SelectMany(level => new[] { (level, 1), (level, 2), (level, 3) }).Order(),
            refineStones.Select(stone => (stone.Level, stone.Kind)).Order());

        var stones = Content.AllItems.Where(item => item.Category == ItemCategory.Stone).Select(item => item.Stone!).ToList();
        Assert.Equal(6, stones.Count(stone => !stone.Random));
        Assert.Equal(2, stones.Count(stone => stone.Random));

        Assert.Equal(
            ["iron-ore", "magic-dust", "beast-hide", "crystal-shard", "stone-remover", "hole-driller", "safe-refine-stone"],
            Content.AllItems.Where(item => item.Category == ItemCategory.Material).Select(item => item.Id));

        Assert.Equal(6, Content.AllRecipes.Count);
    }

    [Fact]
    public void ItemTable_MatchesFormula()
    {
        // round(8·L^1.8)（.5 遠離零）：L4 = 8 × 12.1257 = 97.006 → 97；L7 = 8 × 33.2029 = 265.62 → 266
        Assert.Equal((8, 200L), (Content.GetItem("iron-sword").Bonus.At, Content.GetItem("iron-sword").Price));
        Assert.Equal((97, 12800L), (Content.GetItem("mithril-longsword").Bonus.At, Content.GetItem("mithril-longsword").Price));
        Assert.Equal((266, 68600L), (Content.GetItem("dragonscale-greatsword").Bonus.At, Content.GetItem("dragonscale-greatsword").Price));
        Assert.Equal(266, Content.GetItem("dragontongue-staff").Bonus.Mat);
        Assert.Equal(2, Content.GetItem("iron-helm").Bonus.Df);

        foreach (var item in Equipment)
        {
            var level = item.ShopLevel;
            Assert.Equal(3 + level, item.RefineMax);
            Assert.InRange(item.Holes, 0, 2);

            // 需求只有一項：10 + 8(L−1)
            Assert.Equal([10 + 8 * (level - 1)], Requirements(item.Requires).Where(value => value > 0));

            var bonus = item.Bonus;
            if (item.Category == ItemCategory.Accessory)
            {
                Assert.Equal([2 * level], BaseStats(bonus).Where(value => value > 0));
                Assert.Equal(StatBonus.Zero, bonus with { Str = 0, Smart = 0, Life = 0, Vit = 0, Agi = 0, Au = 0, Be = 0 });
                continue;
            }

            // 物理向給 at／df、需求 str 或 agi；法系向給 mat／mdf、需求 smart 或 au
            var main = MainStat(item.Category, level);
            var (physical, magical) = item.Category == ItemCategory.Weapon ? (bonus.At, bonus.Mat) : (bonus.Df, bonus.Mdf);
            Assert.Contains((physical, magical), new[] { (main, 0), (0, main) });
            Assert.Equal(StatBonus.Zero, bonus with { At = 0, Mat = 0, Df = 0, Mdf = 0 });
            var requiresPhysical = item.Requires.Str + item.Requires.Agi > 0;
            Assert.Equal(physical > 0, requiresPhysical);
        }

        foreach (var category in MainStatFactor.Keys)
        {
            for (var level = 1; level <= ShopLevels; level++)
            {
                var items = Content.ItemsForShop(category, level);
                Assert.Contains(items, item => item.Requires.Str + item.Requires.Agi > 0);
                Assert.Contains(items, item => item.Requires.Smart + item.Requires.Au > 0);
            }
        }
    }

    [Fact]
    public void Recipes_UseShippedItems()
    {
        Assert.All(Content.AllRecipes, recipe =>
        {
            Assert.Matches(KebabCase(), recipe.Id);
            Assert.InRange(recipe.SuccessRate, 1, 100);
        });
        var forge = Content.GetRecipe("forge-iron-sword");
        Assert.Equal(("iron-sword", 1, 70), (forge.ResultItemId, forge.ResultQuantity, forge.SuccessRate));
        Assert.Equal([("iron-ore", 3)], forge.Materials);
    }

    // 鍛造消耗的道具：材料分類、商店販賣
    [Fact]
    public void ForgeTools_AreSoldInShop()
    {
        Assert.Equal(
            [("stone-remover", 500L, 3), ("hole-driller", 3000L, 5), ("safe-refine-stone", 2000L, 4)],
            new[] { ForgeTools.StoneRemover, ForgeTools.HoleDriller, ForgeTools.SafeRefineStone }
                .Select(Content.GetItem)
                .Select(item => (item.Id, item.Price, item.ShopLevel)));
        Assert.All(
            new[] { ForgeTools.StoneRemover, ForgeTools.HoleDriller, ForgeTools.SafeRefineStone }.Select(Content.GetItem),
            item => Assert.Equal((ItemCategory.Material, true, (RefineStoneSpec?)null), (item.Category, item.ShopSellable, item.Refine)));
        Assert.Contains(Content.ItemsForShop(ItemCategory.Material, 3), item => item.Id == ForgeTools.StoneRemover);
    }

    // 計畫 10：禁區 17～20 各一把鑰匙，21 600 秒；不在商店販賣，只能從勳章所兌換
    [Fact]
    public void Keys_OnePerRestrictedPlace()
    {
        var keys = Content.AllItems.Where(item => item.Category == ItemCategory.Key).ToList();

        Assert.Equal(
            [
                ("abandoned-cave-key", "廢棄洞窟鑰匙", 17),
                ("sunset-fort-key", "日沒碉堡鑰匙", 18),
                ("still-city-key", "靜止之城鑰匙", 19),
                ("obsidian-temple-key", "黑曜神廟鑰匙", 20),
            ],
            keys.Select(item => (item.Id, item.Name, item.Key!.PlaceId)));
        Assert.Equal(
            Content.AllPlaces.Where(place => place.Restricted).Select(place => place.Id).Order(),
            keys.Select(item => item.Key!.PlaceId).Order());
        Assert.All(keys, key => Assert.Equal(
            (21_600, ConsumableKind.None, 0L, 1, false),
            (key.Key!.Seconds, key.Consumable, key.Price, key.ShopLevel, key.ShopSellable)));
    }

    // 計畫 10 Task 5：寶箱、寵物蛋、心得書、經驗膠囊都不在商店販賣（從勳章所兌換）
    [Fact]
    public void NewConsumables_MatchTable()
    {
        (string Id, string Name, ConsumableKind Kind, int Amount, long Price)[] expected =
        [
            ("stone-box", "魔石寶箱", ConsumableKind.StoneBox, 1, 1000),
            ("large-stone-box", "大魔石寶箱", ConsumableKind.StoneBox, 2, 3000),
            ("refine-box", "精鍊寶盒", ConsumableKind.RefineBox, 3, 1500),
            ("pet-egg", "寵物蛋", ConsumableKind.PetEgg, 280, 10000),
            ("job-notes", "職業心得書", ConsumableKind.ProficiencyBook, 300, 2000),
            ("advanced-job-notes", "高級心得書", ConsumableKind.ProficiencyBook, 1500, 8000),
            ("exp-capsule", "經驗膠囊", ConsumableKind.ExperienceCapsule, 20000, 5000),
        ];
        ConsumableKind[] newKinds =
            [ConsumableKind.StoneBox, ConsumableKind.RefineBox, ConsumableKind.PetEgg, ConsumableKind.ProficiencyBook, ConsumableKind.ExperienceCapsule];

        var items = Content.AllItems.Where(item => newKinds.Contains(item.Consumable)).ToList();

        Assert.Equal(expected, items.Select(item => (item.Id, item.Name, item.Consumable, item.Amount, item.Price)));
        Assert.All(items, item => Assert.Equal((ItemCategory.Consumable, false), (item.Category, item.ShopSellable)));

        // 魔石寶箱等級：攻擊／魔力／守護／結界 1，生命／疾風 2，混沌／虹彩 3
        Assert.Equal(
            [
                ("attack-stone", 1), ("sorcery-stone", 1), ("guard-stone", 1), ("ward-stone", 1),
                ("vitality-stone", 2), ("gale-stone", 2), ("chaos-stone", 3), ("prism-stone", 3),
            ],
            Content.AllItems.Where(item => item.Category == ItemCategory.Stone).Select(item => (item.Id, item.Stone!.BoxClass)));
    }

    // 計畫 10 Task 6：密藥在商店 6 級販售，效果持續 1 小時
    [Fact]
    public void Elixirs_MatchTable()
    {
        (string Id, string Name, ElixirSpec Elixir, long Price)[] expected =
        [
            ("exp-elixir", "經驗密藥", new(BuffKind.Exp, 1.5, 3600), 20000),
            ("proficiency-elixir", "熟練密藥", new(BuffKind.JobExp, 1.5, 3600), 20000),
            ("might-elixir", "力量密藥", new(BuffKind.Attack, 1.2, 3600), 10000),
            ("sorcery-elixir", "魔力密藥", new(BuffKind.Magic, 1.2, 3600), 10000),
            ("guard-elixir", "守護密藥", new(BuffKind.Defense, 1.2, 3600), 10000),
            ("ward-elixir", "結界密藥", new(BuffKind.MagicDefense, 1.2, 3600), 10000),
        ];

        var elixirs = Content.AllItems.Where(item => item.Consumable == ConsumableKind.Elixir).ToList();

        Assert.Equal(expected, elixirs.Select(item => (item.Id, item.Name, item.Elixir!, item.Price)));
        Assert.All(elixirs, item => Assert.Equal((ItemCategory.Consumable, 6, true), (item.Category, item.ShopLevel, item.ShopSellable)));
        Assert.Equal(elixirs, Content.ItemsForShop(ItemCategory.Consumable, 6).Where(item => item.Consumable == ConsumableKind.Elixir));
    }
}
