namespace EndlessBattle.Domain.Items;

public static class InventoryEnhancement
{
    // 精鍊過或有鑲嵌的道具不與同種道具堆疊，交付時最後才拿
    public static bool IsEnhanced(this InventoryEntry entry) =>
        entry.RefineLevel != 0 || entry.Sockets.Count != 0 || entry.RefineBonus != StatBonus.Zero;

    // 對玩家而言的「強化過」：再加上比道具原本多打的孔；賣出或交出前要提醒
    public static bool IsEnhanced(this InventoryEntry entry, ItemDefinition item) => entry.IsEnhanced() || entry.Holes > item.Holes;
}
