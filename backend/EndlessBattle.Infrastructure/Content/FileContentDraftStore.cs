using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin;

namespace EndlessBattle.Infrastructure.Content;

// 草稿是正式內容資料夾的整份副本；草稿資料夾存在時讀草稿，否則讀正式內容。
// 建立草稿時在 .base.json 記下每個正式內容檔的 SHA-256，發布前用來確認正式內容沒被改過
public sealed class FileContentDraftStore(string officialDirectory, string draftDirectory) : IContentDraftStore
{
    public const string BaseFileName = ".base.json";

    public async Task<IReadOnlyList<JsonObject>> ReadRecordsAsync(ContentKind kind, CancellationToken cancellationToken)
    {
        var directory = Directory.Exists(draftDirectory) ? draftDirectory : officialDirectory;
        var root = await ReadRootAsync(Path.Combine(directory, kind.FileName), cancellationToken);
        return RecordsOf(root, kind).Select(record => record!.AsObject()).ToList();
    }

    public async Task WriteRecordsAsync(ContentKind kind, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken)
    {
        await EnsureDraftAsync(cancellationToken);
        var path = Path.Combine(draftDirectory, kind.FileName);
        var root = await ReadRootAsync(path, cancellationToken);
        RecordsOf(root, kind);
        // 紀錄可能還掛在讀取時的陣列上，先複製再放進新陣列
        root[kind.ArrayProperty] = new JsonArray(records.Select(record => (JsonNode)record.DeepClone().AsObject()).ToArray());
        await File.WriteAllTextAsync(path, ContentFileFormatter.Format(root), cancellationToken);
    }

    private async Task EnsureDraftAsync(CancellationToken cancellationToken)
    {
        if (Directory.Exists(draftDirectory))
        {
            return;
        }

        Directory.CreateDirectory(draftDirectory);
        var fingerprints = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(officialDirectory, "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(file);
            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            fingerprints[name] = Convert.ToHexStringLower(SHA256.HashData(bytes));
            await File.WriteAllBytesAsync(Path.Combine(draftDirectory, name), bytes, cancellationToken);
        }

        await File.WriteAllTextAsync(Path.Combine(draftDirectory, BaseFileName), JsonSerializer.Serialize(fingerprints), cancellationToken);
    }

    private static async Task<JsonObject> ReadRootAsync(string path, CancellationToken cancellationToken) =>
        JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken)) as JsonObject
            ?? throw new GameContentException($"內容檔的最外層必須是物件：{path}");

    private static JsonArray RecordsOf(JsonObject root, ContentKind kind) =>
        root[kind.ArrayProperty] as JsonArray
            ?? throw new GameContentException($"{kind.FileName} 沒有 {kind.ArrayProperty} 陣列");
}
