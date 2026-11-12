using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Items;

// 給畫面看的密藥：最後喝的道具查不到（舊資料、內容已移除）時 ItemId、ItemName 都是 null
public sealed record BuffView(BuffKind Kind, double Multiplier, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? ItemId, string? ItemName)
{
    public static BuffView Of(CharacterBuff buff, IGameContent content)
    {
        var item = buff.ItemId is null ? null : content.FindItem(buff.ItemId);
        return new BuffView(buff.Kind, buff.Multiplier, buff.StartsAt, buff.EndsAt, item?.Id, item?.Name);
    }
}
