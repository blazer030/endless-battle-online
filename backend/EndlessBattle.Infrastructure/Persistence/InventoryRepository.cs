using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class InventoryRepository(EndlessBattleDbContext context, IGameContent content) : IInventoryRepository
{
    public async Task<IReadOnlyList<InventoryEntry>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterInventories.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(row => ToEntry(row, content)).OfType<InventoryEntry>().ToList();
    }

    public async Task<long> AddAsync(long characterId, InventoryEntry entry, CancellationToken cancellationToken)
    {
        var row = new CharacterInventory
        {
            CharacterId = characterId,
            ItemId = entry.ItemId,
            Quantity = entry.Quantity,
            RefineLevel = entry.RefineLevel,
            RefineBonus = EnhancementJson.SerializeBonus(entry.RefineBonus),
            Sockets = EnhancementJson.SerializeSockets(entry.Sockets),
            Holes = entry.Holes,
        };
        context.CharacterInventories.Add(row);
        await context.SaveChangesAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateQuantityAsync(long characterId, long id, int quantity, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, id, cancellationToken);
        row.Quantity = quantity;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEnhancementAsync(
        long characterId, long id, int refineLevel, StatBonus refineBonus, IReadOnlyList<SocketedStone> sockets, int holes,
        CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, id, cancellationToken);
        row.RefineLevel = refineLevel;
        row.RefineBonus = EnhancementJson.SerializeBonus(refineBonus);
        row.Sockets = EnhancementJson.SerializeSockets(sockets);
        row.Holes = holes;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(long characterId, long id, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, id, cancellationToken);
        context.CharacterInventories.Remove(row);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CharacterInventory> FindAsync(long characterId, long id, CancellationToken cancellationToken) =>
        await context.CharacterInventories.SingleOrDefaultAsync(row => row.Id == id && row.CharacterId == characterId, cancellationToken)
        ?? throw new InvalidOperationException("道具不屬於此角色");

    // 道具 id 已不在內容裡的列回 null（資料庫保留，內容加回來就恢復）；鑲嵌的魔石不在內容裡時那個孔不回傳
    internal static InventoryEntry? ToEntry(CharacterInventory row, IGameContent content) =>
        ToEntry(row.Id, row.ItemId, row.Quantity, row.RefineLevel, row.RefineBonus, row.Sockets, row.Holes, content);

    internal static InventoryEntry? ToEntry(
        long id, string itemId, int quantity, int refineLevel, string? refineBonus, string? sockets, int holes, IGameContent content) =>
        content.FindItem(itemId) is null
            ? null
            : new InventoryEntry(
                id, itemId, quantity, refineLevel, EnhancementJson.DeserializeBonus(refineBonus),
                EnhancementJson.DeserializeSockets(sockets).Where(socket => content.FindItem(socket.StoneId) is not null).ToList(),
                holes);
}
