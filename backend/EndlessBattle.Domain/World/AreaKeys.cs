using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.World;

// 禁區通行證：到期前可免費進出同一禁區（WOG wog_key_list.area_time）
public sealed record AreaPass(int PlaceId, DateTimeOffset ExpiresAt)
{
    public bool ValidAt(DateTimeOffset now) => now < ExpiresAt;
}

public static class AreaKeys
{
    // 依 Id 由小到大取第一格；內容裡已不存在的道具略過
    public static InventoryEntry? FindKey(IReadOnlyList<InventoryEntry> bag, int placeId, Func<string, ItemDefinition?> items) =>
        bag.OrderBy(entry => entry.Id).FirstOrDefault(entry => items(entry.ItemId)?.Key?.PlaceId == placeId);

    public static AreaPass Enter(AreaKeySpec key, DateTimeOffset now) => new(key.PlaceId, now.AddSeconds(key.Seconds));

    // 進入禁區時沒有有效通行證才需要扣鑰匙
    public static bool NeedsKey(PlaceDefinition place, AreaPass? pass, DateTimeOffset now) =>
        place.Restricted && !(pass?.ValidAt(now) ?? false);

    // 推進只模擬到通行證到期為止；禁區沒有通行證時回傳 MinValue（什麼都不模擬）
    public static DateTimeOffset SimulateUntil(PlaceDefinition place, AreaPass? pass, DateTimeOffset now)
    {
        if (!place.Restricted)
        {
            return now;
        }

        return pass is null ? DateTimeOffset.MinValue : pass.ExpiresAt < now ? pass.ExpiresAt : now;
    }
}
