using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Economy;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Economy;

// 先以版本鎖寫入角色（金錢變動），成功後才動背包；衝突時背包不動
public sealed class ShopService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IGameContent content,
    InventoryService inventoryService)
{
    public const int MinLevel = 1, MaxLevel = 7;

    public IReadOnlyList<ItemDefinition> List(ItemCategory category, int level) =>
        level is < MinLevel or > MaxLevel
            ? throw new InputValidationException($"商店等級必須是 {MinLevel}～{MaxLevel}", ["invalidLevel"])
            : content.ItemsForShop(category, level);

    public async Task<InventoryView> BuyAsync(long accountId, string itemId, int quantity, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var item = content.AllItems.FirstOrDefault(candidate => candidate.Id == itemId)
            ?? throw new InputValidationException("商店沒有這個品項", ["notForSale"]);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var sameCategory = state.Bag.Where(entry => content.GetItem(entry.ItemId).Category == item.Category).ToList();
        var reasons = Shop.CanBuy(character, item, quantity, sameCategory);
        if (reasons.Count > 0)
        {
            throw new InputValidationException("無法購買", reasons);
        }

        await characters.UpdateAsync(character with { Money = character.Money - Shop.BuyCost(item, quantity) }, cancellationToken);
        var added = InventoryRules.TryAdd(sameCategory, item, quantity, character.BagBonus);
        foreach (var updated in added.UpdatedEntries)
        {
            await inventory.UpdateQuantityAsync(character.Id, updated.Id, updated.Quantity, cancellationToken);
        }

        foreach (var created in added.NewEntries)
        {
            await inventory.AddAsync(character.Id, created, cancellationToken);
        }

        return await inventoryService.GetAsync(accountId, cancellationToken);
    }

    public async Task<InventoryView> SellAsync(long accountId, long inventoryId, int quantity, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var entry = state.Bag.FirstOrDefault(candidate => candidate.Id == inventoryId)
            ?? throw (state.Loadout.Equipped.Values.Any(equipped => equipped.Id == inventoryId)
                ? new InputValidationException("請先卸下這件裝備", ["equipped"])
                : new InputValidationException("背包裡沒有這件道具", ["itemNotFound"]));
        var item = content.GetItem(entry.ItemId);
        var reasons = Shop.CanSell(entry, item, quantity);
        if (reasons.Count > 0)
        {
            throw new InputValidationException("無法出售", reasons);
        }

        await characters.UpdateAsync(character with { Money = character.Money + Shop.SellPrice(item, quantity) }, cancellationToken);
        if (quantity < entry.Quantity)
        {
            await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity - quantity, cancellationToken);
        }
        else
        {
            await inventory.RemoveAsync(character.Id, entry.Id, cancellationToken);
        }

        return await inventoryService.GetAsync(accountId, cancellationToken);
    }

    private async Task<Domain.Characters.Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
