using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Economy;

public static class Shop
{
    public const int MaxBuyQuantity = 99;

    public static long BuyCost(ItemDefinition item, int quantity) => item.Price * quantity;

    // 鑲嵌與精鍊不計入，也不補償
    public static long SellPrice(ItemDefinition item, int quantity) => (long)Math.Floor(item.Price * 0.5 * quantity);

    // 原因代碼依序為 notForSale、invalidQuantity、notEnoughMoney、bagFull；
    // sameCategoryBag 是背包中與 item 同分類、沒有裝備上的道具
    public static IReadOnlyList<string> CanBuy(
        Character character, ItemDefinition item, int quantity, IReadOnlyList<InventoryEntry> sameCategoryBag)
    {
        if (!item.ShopSellable)
        {
            return ["notForSale"];
        }

        var validQuantity = item.IsEquipment ? quantity == 1 : quantity is >= 1 and <= MaxBuyQuantity;
        if (!validQuantity)
        {
            return ["invalidQuantity"];
        }

        if (BuyCost(item, quantity) > character.Money)
        {
            return ["notEnoughMoney"];
        }

        return InventoryRules.TryAdd(sameCategoryBag, item, quantity, character.BagBonus).Discarded > 0 ? ["bagFull"] : [];
    }

    // 綁定的道具也可以賣給商店（WOG 的綁定只擋贈送、轉倉、交易）
    public static IReadOnlyList<string> CanSell(InventoryEntry entry, ItemDefinition item, int quantity)
    {
        var valid = item.IsEquipment ? quantity == 1 : quantity >= 1 && quantity <= entry.Quantity;
        return valid ? [] : ["invalidQuantity"];
    }
}
