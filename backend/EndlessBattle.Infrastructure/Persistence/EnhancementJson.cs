using System.Text.Json;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Infrastructure.Persistence;

// 精鍊加成與鑲嵌魔石存成 JSON 欄位；空值或全 0 存 NULL
internal static class EnhancementJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string? SerializeBonus(StatBonus bonus) =>
        bonus == StatBonus.Zero ? null : JsonSerializer.Serialize(bonus, Options);

    public static StatBonus DeserializeBonus(string? json) =>
        json is null ? StatBonus.Zero : JsonSerializer.Deserialize<StatBonus>(json, Options)!;

    public static string? SerializeSockets(IReadOnlyList<SocketedStone> sockets) =>
        sockets.Count == 0 ? null : JsonSerializer.Serialize(sockets, Options);

    public static IReadOnlyList<SocketedStone> DeserializeSockets(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<List<SocketedStone>>(json, Options)!;
}
