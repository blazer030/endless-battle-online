using System.Text.Json.Nodes;

namespace EndlessBattle.Application.Admin;

// 內容紀錄的讀寫；有草稿時讀草稿，否則讀正式內容。寫入只寫草稿，沒有草稿時先建立
public interface IContentDraftStore
{
    Task<IReadOnlyList<JsonObject>> ReadRecordsAsync(ContentKind kind, CancellationToken cancellationToken);

    Task WriteRecordsAsync(ContentKind kind, IReadOnlyList<JsonObject> records, CancellationToken cancellationToken);

    // 用遊戲啟動時的同一套載入程式檢查草稿
    Task<DraftStatus> CheckAsync(CancellationToken cancellationToken);
}
