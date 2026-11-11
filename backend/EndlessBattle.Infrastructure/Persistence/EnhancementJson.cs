using System.Text.Json;
using System.Text.Json.Serialization;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Infrastructure.Persistence;

// 精鍊加成與鑲嵌魔石存成 JSON 欄位；空值或全 0 存 NULL
// 儲存格式的鍵固定，與 Domain 的命名脫鉤
internal static class EnhancementJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string? SerializeBonus(StatBonus bonus) =>
        bonus == StatBonus.Zero ? null : JsonSerializer.Serialize(StoredStatBonus.From(bonus), Options);

    public static StatBonus DeserializeBonus(string? json) =>
        json is null ? StatBonus.Zero : JsonSerializer.Deserialize<StoredStatBonus>(json, Options)!.ToDomain();

    public static string? SerializeSockets(IReadOnlyList<SocketedStone> sockets) =>
        sockets.Count == 0 ? null : JsonSerializer.Serialize(sockets.Select(StoredSocket.From).ToList(), Options);

    public static IReadOnlyList<SocketedStone> DeserializeSockets(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<List<StoredSocket>>(json, Options)!.Select(socket => socket.ToDomain()).ToList();

    private sealed record StoredStatBonus(
        [property: JsonPropertyName("str")] int Str,
        [property: JsonPropertyName("smart")] int Smart,
        [property: JsonPropertyName("life")] int Life,
        [property: JsonPropertyName("vit")] int Vit,
        [property: JsonPropertyName("agi")] int Agi,
        [property: JsonPropertyName("au")] int Au,
        [property: JsonPropertyName("be")] int Be,
        [property: JsonPropertyName("at")] int At,
        [property: JsonPropertyName("mat")] int Mat,
        [property: JsonPropertyName("df")] int Df,
        [property: JsonPropertyName("mdf")] int Mdf,
        [property: JsonPropertyName("hpMax")] int HpMax)
    {
        public static StoredStatBonus From(StatBonus bonus) => new(
            bonus.Str, bonus.Smart, bonus.Life, bonus.Vit, bonus.Agi, bonus.Au, bonus.Be, bonus.At, bonus.Mat, bonus.Df, bonus.Mdf, bonus.HpMax);

        public StatBonus ToDomain() => new(Str, Smart, Life, Vit, Agi, Au, Be, At, Mat, Df, Mdf, HpMax);
    }

    private sealed record StoredSocket(
        [property: JsonPropertyName("hole")] int Hole,
        [property: JsonPropertyName("stoneId")] string StoneId,
        [property: JsonPropertyName("rolled")] StoredStatBonus Rolled)
    {
        public static StoredSocket From(SocketedStone socket) => new(socket.Hole, socket.StoneId, StoredStatBonus.From(socket.Rolled));

        public SocketedStone ToDomain() => new(Hole, StoneId, Rolled.ToDomain());
    }
}
