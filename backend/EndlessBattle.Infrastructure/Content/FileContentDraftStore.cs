using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin;

namespace EndlessBattle.Infrastructure.Content;

// 草稿是正式內容資料夾的整份副本；草稿資料夾存在時讀草稿，否則讀正式內容
public sealed class FileContentDraftStore(string officialDirectory, string draftDirectory) : IContentDraftStore
{
    public async Task<IReadOnlyList<JsonObject>> ReadRecordsAsync(ContentKind kind, CancellationToken cancellationToken)
    {
        var directory = Directory.Exists(draftDirectory) ? draftDirectory : officialDirectory;
        var text = await File.ReadAllTextAsync(Path.Combine(directory, kind.FileName), cancellationToken);
        var records = JsonNode.Parse(text)?[kind.ArrayProperty] as JsonArray
            ?? throw new GameContentException($"{kind.FileName} 沒有 {kind.ArrayProperty} 陣列");
        return records.Select(record => record!.AsObject()).ToList();
    }
}
