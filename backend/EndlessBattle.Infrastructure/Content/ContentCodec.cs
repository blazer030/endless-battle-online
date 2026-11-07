using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// 內容檔與 admin 紀錄之間的雙向轉換。內容檔的欄位名稱、省略規則與欄位順序只存在這裡；讀成紀錄再寫回必須逐字相同
public interface IContentCodec<TRecord>
{
    IReadOnlyList<string> FileNames { get; }

    IReadOnlyList<TRecord> Read(IReadOnlyDictionary<string, string> files);

    // files 是目前的檔案內容，供只有部分屬於紀錄的檔案保留其他欄位
    IReadOnlyDictionary<string, string> Write(IReadOnlyList<TRecord> records, IReadOnlyDictionary<string, string> files);
}

public sealed class CodecContentRecordStore<TRecord>(FileContentDraftStore files, IContentCodec<TRecord> codec) : IContentRecordStore<TRecord>
{
    public async Task<IReadOnlyList<TRecord>> ReadAsync(CancellationToken cancellationToken) =>
        codec.Read(await files.ReadFilesAsync(codec.FileNames, cancellationToken));

    public async Task WriteAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken) =>
        await files.WriteFilesAsync(codec.Write(records, await files.ReadFilesAsync(codec.FileNames, cancellationToken)), cancellationToken);
}

// codec 共用的讀寫：內容檔有紀錄沒涵蓋的欄位時讀取失敗並指出檔案，而不是存檔時默默丟掉
public static class ContentFileJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        // 寫出後由 ContentFileFormatter 保留原文，中文不能變成 \uXXXX
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static IReadOnlyList<TEntry> ReadArray<TEntry>(string fileText, string fileName, string arrayProperty)
    {
        var root = ParseRoot(fileText, fileName);
        if (root.Count != 1 || root[arrayProperty] is not JsonArray entries)
        {
            throw new GameContentException($"{fileName} 應該只有 {arrayProperty} 陣列");
        }

        return ReadEntries<TEntry>(entries, fileName);
    }

    public static JsonObject ParseRoot(string fileText, string fileName) =>
        JsonNode.Parse(fileText) as JsonObject ?? throw new GameContentException($"內容檔的最外層必須是物件：{fileName}");

    public static IReadOnlyList<TEntry> ReadEntries<TEntry>(JsonArray entries, string fileName)
    {
        try
        {
            return entries.Deserialize<List<TEntry>>(Options) ?? throw new GameContentException($"{fileName} 的陣列不可為 null");
        }
        catch (JsonException exception)
        {
            throw new GameContentException($"{fileName} 無法轉換成紀錄（{exception.Path}）：{exception.Message}");
        }
    }

    public static string WriteArray<TEntry>(string arrayProperty, IEnumerable<TEntry> entries) =>
        ContentFileFormatter.Format(new JsonObject { [arrayProperty] = ToNode(entries) });

    public static JsonArray ToNode<TEntry>(IEnumerable<TEntry> entries) =>
        JsonSerializer.SerializeToNode(entries.ToList(), Options)!.AsArray();

    private static readonly Dictionary<string, ContentStat> StatsByFileName = new()
    {
        ["str"] = ContentStat.Strength,
        ["smart"] = ContentStat.Intelligence,
        ["life"] = ContentStat.Life,
        ["vit"] = ContentStat.Vitality,
        ["agi"] = ContentStat.Agility,
        ["au"] = ContentStat.Charisma,
        ["be"] = ContentStat.Faith,
    };

    public static ContentStat ToStat(string fileName, string owner) =>
        StatsByFileName.TryGetValue(fileName, out var stat)
            ? stat
            : throw new GameContentException($"{owner} 引用了不存在的能力值：{fileName}");

    public static string FromStat(ContentStat stat) => StatsByFileName.First(pair => pair.Value == stat).Key;

    // 內容檔的範圍寫成 [最小, 最大]
    public static ContentRange ToRange(int[] range, string owner) =>
        range.Length == 2
            ? new ContentRange { Minimum = range[0], Maximum = range[1] }
            : throw new GameContentException($"{owner} 的範圍必須是 [最小, 最大]");

    public static int[] FromRange(ContentRange range) => [range.Minimum, range.Maximum];

    public static ContentElement ToElement(int value, string owner) =>
        Enum.IsDefined((ContentElement)value)
            ? (ContentElement)value
            : throw new GameContentException($"{owner} 的 element {value} 不是有效的屬性");
}
