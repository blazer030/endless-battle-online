namespace EndlessBattle.Domain.Items;

// 背包或倉庫的一筆道具；Id 在尚未寫入資料庫時為 0
public sealed record InventoryEntry(
    long Id,
    string ItemId,
    int Quantity,
    int RefineLevel,
    StatBonus RefineBonus,
    IReadOnlyList<SocketedStone> Sockets,
    int Holes);

public sealed record SocketedStone(int Hole, string StoneId, StatBonus Rolled);
