using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Economy;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Economy;

public sealed record DepotView(IReadOnlyList<InventoryItem> Entries, int Capacity);

public sealed record DepotMove(InventoryView Inventory, DepotView Depot);

// 存取都先以版本鎖寫入角色（內容不變、只遞增版本），成功後才動倉庫與背包
public sealed class DepotService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IDepotRepository depot,
    IGameContent content,
    InventoryService inventoryService)
{
    public async Task<DepotView> GetAsync(long accountId, CancellationToken cancellationToken) =>
        await ViewAsync(await FindCharacterAsync(accountId, cancellationToken), cancellationToken);

    public async Task<DepotMove> StoreAsync(long accountId, long inventoryId, int quantity, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var entry = state.Bag.FirstOrDefault(candidate => candidate.Id == inventoryId)
            ?? throw (state.Loadout.Equipped.Values.Any(equipped => equipped.Id == inventoryId)
                ? new InputValidationException("請先卸下這件裝備", ["equipped"])
                : new InputValidationException("背包裡沒有這件道具", ["itemNotFound"]));
        var item = content.GetItem(entry.ItemId);
        var stored = await depot.ListAsync(character.Id, cancellationToken);
        var plan = DepotRules.TryStore(stored, item, entry, quantity, character.DepotBonus);
        Check(plan, "無法存入倉庫");

        await characters.UpdateAsync(character, cancellationToken);
        foreach (var updated in plan.Updated)
        {
            await depot.UpdateQuantityAsync(character.Id, updated.Id, updated.Quantity, cancellationToken);
        }

        foreach (var created in plan.Created)
        {
            await depot.AddAsync(character.Id, created, cancellationToken);
        }

        if (quantity < entry.Quantity)
        {
            await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity - quantity, cancellationToken);
        }
        else
        {
            await inventory.RemoveAsync(character.Id, entry.Id, cancellationToken);
        }

        return await MoveResultAsync(accountId, character, cancellationToken);
    }

    public async Task<DepotMove> TakeAsync(long accountId, long depotId, int quantity, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var stored = await depot.ListAsync(character.Id, cancellationToken);
        var entry = stored.FirstOrDefault(candidate => candidate.Id == depotId)
            ?? throw new InputValidationException("倉庫裡沒有這件道具", ["itemNotFound"]);
        var item = content.GetItem(entry.ItemId);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var sameCategory = state.Bag.Where(other => content.GetItem(other.ItemId).Category == item.Category).ToList();
        var plan = DepotRules.TryTake(sameCategory, item, entry, quantity, character.BagBonus);
        Check(plan, "無法取出");

        await characters.UpdateAsync(character, cancellationToken);
        foreach (var updated in plan.Updated)
        {
            await inventory.UpdateQuantityAsync(character.Id, updated.Id, updated.Quantity, cancellationToken);
        }

        foreach (var created in plan.Created)
        {
            await inventory.AddAsync(character.Id, created, cancellationToken);
        }

        if (quantity < entry.Quantity)
        {
            await depot.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity - quantity, cancellationToken);
        }
        else
        {
            await depot.RemoveAsync(character.Id, entry.Id, cancellationToken);
        }

        return await MoveResultAsync(accountId, character, cancellationToken);
    }

    private static void Check(DepotPlan plan, string message)
    {
        if (plan.Reasons.Count > 0)
        {
            throw new InputValidationException(message, plan.Reasons);
        }
    }

    private async Task<DepotMove> MoveResultAsync(long accountId, Character character, CancellationToken cancellationToken) =>
        new(await inventoryService.GetAsync(accountId, cancellationToken), await ViewAsync(character, cancellationToken));

    private async Task<DepotView> ViewAsync(Character character, CancellationToken cancellationToken)
    {
        var entries = await depot.ListAsync(character.Id, cancellationToken);
        return new DepotView(
            entries.Select(entry => InventoryItem.From(entry, content.GetItem)).ToList(),
            DepotRules.Capacity(character.DepotBonus));
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
