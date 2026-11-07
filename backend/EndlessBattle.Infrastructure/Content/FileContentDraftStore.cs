using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Content;

namespace EndlessBattle.Infrastructure.Content;

// 草稿是正式內容資料夾的整份副本；草稿資料夾存在時讀草稿，否則讀正式內容。
// 建立草稿時在 .base.json 記下每個正式內容檔的 SHA-256，發布前用來確認正式內容沒被改過
public sealed class FileContentDraftStore(string officialDirectory, string draftDirectory) : IContentDraftStore
{
    public const string BaseFileName = ".base.json";

    private const string TemporarySuffix = ".writing";

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

    public async Task<IReadOnlySet<string>> ReadIdsAsync(ContentKind kind, CancellationToken cancellationToken) =>
        (await ReadRecordsAsync(kind, cancellationToken))
            .Select(ContentAdminService.IdOf)
            .OfType<string>()
            .ToHashSet();

    // 檔名 → 檔案內容；有草稿時讀草稿，否則讀正式內容
    public async Task<IReadOnlyDictionary<string, string>> ReadFilesAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken)
    {
        var directory = Directory.Exists(draftDirectory) ? draftDirectory : officialDirectory;
        var files = new Dictionary<string, string>();
        foreach (var name in fileNames)
        {
            files[name] = await File.ReadAllTextAsync(Path.Combine(directory, name), cancellationToken);
        }

        return files;
    }

    // 先全部寫到暫存檔再一起換上：任何一個暫存檔寫不出來時，草稿的內容檔都不變
    public async Task WriteFilesAsync(IReadOnlyDictionary<string, string> files, CancellationToken cancellationToken)
    {
        await EnsureDraftAsync(cancellationToken);
        var temporaryFiles = files.Keys.ToDictionary(name => name, name => Path.Combine(draftDirectory, name + TemporarySuffix));
        try
        {
            foreach (var (name, text) in files)
            {
                await File.WriteAllTextAsync(temporaryFiles[name], text, cancellationToken);
            }
        }
        catch
        {
            foreach (var temporaryFile in temporaryFiles.Values.Where(File.Exists))
            {
                File.Delete(temporaryFile);
            }

            throw;
        }

        foreach (var (name, temporaryFile) in temporaryFiles)
        {
            File.Move(temporaryFile, Path.Combine(draftDirectory, name), overwrite: true);
        }
    }

    public Task<DraftStatus> CheckAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(draftDirectory))
        {
            return Task.FromResult(DraftStatus.None);
        }

        try
        {
            JsonGameContent.Load(draftDirectory);
            return Task.FromResult(new DraftStatus(Exists: true, Valid: true, Error: null));
        }
        catch (Exception exception) when (exception is GameContentException or JsonException)
        {
            return Task.FromResult(new DraftStatus(Exists: true, Valid: false, Error: exception.Message));
        }
    }

    public Task<IGameContent> LoadOfficialAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IGameContent>(JsonGameContent.Load(officialDirectory));

    public Task<IGameContent> LoadDraftAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IGameContent>(JsonGameContent.Load(draftDirectory));

    public async Task<IReadOnlyList<string>> ChangedOfficialFilesAsync(CancellationToken cancellationToken)
    {
        var recorded = JsonSerializer.Deserialize<Dictionary<string, string>>(
                await File.ReadAllTextAsync(Path.Combine(draftDirectory, BaseFileName), cancellationToken))
            ?? throw new GameContentException($"草稿的 {BaseFileName} 是空的");
        var current = await FingerprintsAsync(cancellationToken);
        return recorded.Keys.Union(current.Keys)
            .Where(name => recorded.GetValueOrDefault(name) != current.GetValueOrDefault(name))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(draftDirectory, "*.json").Where(file => Path.GetFileName(file) != BaseFileName))
        {
            await File.WriteAllBytesAsync(
                Path.Combine(officialDirectory, Path.GetFileName(file)),
                await File.ReadAllBytesAsync(file, cancellationToken),
                cancellationToken);
        }

        Directory.Delete(draftDirectory, recursive: true);
    }

    public Task<bool> DiscardAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(draftDirectory))
        {
            return Task.FromResult(false);
        }

        Directory.Delete(draftDirectory, recursive: true);
        return Task.FromResult(true);
    }

    private async Task<Dictionary<string, string>> FingerprintsAsync(CancellationToken cancellationToken)
    {
        var fingerprints = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(officialDirectory, "*.json").Order(StringComparer.Ordinal))
        {
            fingerprints[Path.GetFileName(file)] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(file, cancellationToken)));
        }

        return fingerprints;
    }

    private async Task EnsureDraftAsync(CancellationToken cancellationToken)
    {
        if (Directory.Exists(draftDirectory))
        {
            return;
        }

        Directory.CreateDirectory(draftDirectory);
        var fingerprints = await FingerprintsAsync(cancellationToken);
        foreach (var name in fingerprints.Keys)
        {
            File.Copy(Path.Combine(officialDirectory, name), Path.Combine(draftDirectory, name));
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
