namespace EndlessBattle.Domain.Items;

public static class InventoryEnhancement
{
    // 精鍊過或有鑲嵌的道具不與同種道具堆疊，交付時最後才拿
    public static bool IsEnhanced(this InventoryEntry entry) =>
        entry.RefineLevel != 0 || entry.Sockets.Count != 0 || entry.RefineBonus != StatBonus.Zero;
}
