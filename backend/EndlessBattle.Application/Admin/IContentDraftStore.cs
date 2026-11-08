using EndlessBattle.Application.Content;

namespace EndlessBattle.Application.Admin;

// 草稿的狀態與發布；有草稿時讀草稿，否則讀正式內容。紀錄的讀寫在 IContentRecordStore
public interface IContentDraftStore
{
    // 用遊戲啟動時的同一套載入程式檢查草稿
    Task<DraftStatus> CheckAsync(CancellationToken cancellationToken);

    // 用遊戲的載入程式讀進整份內容；草稿要先確認存在且通過檢查
    Task<IGameContent> LoadOfficialAsync(CancellationToken cancellationToken);

    Task<IGameContent> LoadDraftAsync(CancellationToken cancellationToken);

    // 與建立草稿時記下的指紋不同（含新增、刪除）的正式內容檔名
    Task<IReadOnlyList<string>> ChangedOfficialFilesAsync(CancellationToken cancellationToken);

    // 草稿的內容檔寫回正式內容，然後刪除草稿
    Task PublishAsync(CancellationToken cancellationToken);

    // 刪除草稿；沒有草稿時回傳 false
    Task<bool> DiscardAsync(CancellationToken cancellationToken);
}
