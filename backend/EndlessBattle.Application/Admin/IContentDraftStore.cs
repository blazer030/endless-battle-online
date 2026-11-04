using System.Text.Json.Nodes;

namespace EndlessBattle.Application.Admin;

// 內容紀錄的讀寫；有草稿時讀草稿，否則讀正式內容
public interface IContentDraftStore
{
    Task<IReadOnlyList<JsonObject>> ReadRecordsAsync(ContentKind kind, CancellationToken cancellationToken);
}
