using System.Text.RegularExpressions;
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
            ["iron-ore", "magic-dust", "beast-hide", "crystal-shard"],
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
}
