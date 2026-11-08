namespace EndlessBattle.Application.Admin;

// Valid：草稿能被遊戲的內容載入程式讀進去；Error 是載入程式回報的第一個錯誤。沒有草稿時視為有效
public sealed record DraftStatus(bool Exists, bool Valid, string? Error)
{
    public static DraftStatus None { get; } = new(Exists: false, Valid: true, Error: null);
}
