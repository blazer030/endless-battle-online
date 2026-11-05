using System.Text.RegularExpressions;
using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 匯入的道具與配方，以及保留的自訂道具（gaps.md「保留的自訂內容」）
public partial class ItemTableTests
{
    private static readonly JsonGameContent Content =
        JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static IEnumerable<ItemDefinition> CustomItems => Content.AllItems.Where(item => CustomContent.IsCustom(item.Id));

    [GeneratedRegex(@"^recipe-\d+$")]
    private static partial Regex OriginalRecipeId();

    [Fact]
    public void Items_KeepOnlyCustomItemsWithoutCounterpart()
    {
        Assert.Equal(
            ["large-stone-box", "pet-egg", "refine-box", "safe-refine-stone", "stone-box"],
            CustomItems.Select(item => item.Id).Order());
    }

    [Fact]
    public void Recipes_UseShippedItems()
    {
        // 原版 196 筆配方，成品與材料都是匯入的原版道具
        Assert.Equal(196, Content.AllRecipes.Count);
        Assert.All(Content.AllRecipes, recipe =>
        {
            Assert.Matches(OriginalRecipeId(), recipe.Id);
            Assert.InRange(recipe.SuccessRate, 1, 100);
            Assert.All(recipe.Materials.Select(material => material.ItemId).Prepend(recipe.ResultItemId), itemId =>
                Assert.False(CustomContent.IsCustom(itemId)));
        });
    }

    // 鍛造消耗的道具：原版的分離魔石LV1（商店有賣）、劣質打孔石（勳章所兌換），安全精鍊石是保留的自訂道具
    [Fact]
    public void ForgeTools_UseOriginalItems()
    {
        Assert.Equal(
            [("item-1378", "分離魔石LV1", true), ("item-3361", "劣質打孔石", false), ("safe-refine-stone", "安全精鍊石", true)],
            new[] { ForgeTools.StoneRemover, ForgeTools.HoleDriller, ForgeTools.SafeRefineStone }
                .Select(Content.GetItem)
                .Select(item => (item.Id, item.Name, item.ShopSellable)));
        Assert.All(
            new[] { ForgeTools.StoneRemover, ForgeTools.HoleDriller, ForgeTools.SafeRefineStone }.Select(Content.GetItem),
            item => Assert.Equal((ItemCategory.Material, (RefineStoneSpec?)null), (item.Category, item.Refine)));
    }

    // 原版地區鑰匙表：禁區 17～20 各一把，86 400 秒；不在商店販賣，只能從勳章所兌換
    [Fact]
    public void Keys_OnePerRestrictedPlace()
    {
        var keys = Content.AllItems.Where(item => item.Category == ItemCategory.Key).ToList();

        Assert.Equal(
            [("item-1338", "洞窟鑰匙", 17), ("item-1339", "日末鑰匙", 18), ("item-1405", "靜止鑰匙", 19), ("item-1522", "黑曜鑰匙", 20)],
            keys.Select(item => (item.Id, item.Name, item.Key!.PlaceId)));
        Assert.Equal(
            Content.AllPlaces.Where(place => place.Restricted).Select(place => place.Id).Order(),
            keys.Select(item => item.Key!.PlaceId).Order());
        Assert.All(keys, key => Assert.Equal(
            (86_400, ConsumableKind.None, false),
            (key.Key!.Seconds, key.Consumable, key.ShopSellable)));
    }

    // 保留的自訂消耗品：寶箱、寶盒、寵物蛋不在商店販賣（從勳章所兌換）
    [Fact]
    public void KeptConsumables_MatchTable()
    {
        (string Id, string Name, ConsumableKind Kind, int Amount, long Price)[] expected =
        [
            ("stone-box", "魔石寶箱", ConsumableKind.StoneBox, 1, 1000),
            ("large-stone-box", "大魔石寶箱", ConsumableKind.StoneBox, 2, 3000),
            ("refine-box", "精鍊寶盒", ConsumableKind.RefineBox, 3, 1500),
            ("pet-egg", "寵物蛋", ConsumableKind.PetEgg, 280, 10000),
        ];

        var items = CustomItems.Where(item => item.Category == ItemCategory.Consumable).ToList();

        Assert.Equal(expected, items.Select(item => (item.Id, item.Name, item.Consumable, item.Amount, item.Price)));
        Assert.All(items, item => Assert.False(item.ShopSellable));
    }
}
