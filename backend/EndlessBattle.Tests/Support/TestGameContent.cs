using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Support;

// 資料庫測試直接建 repository 時用的內容（TestContent）
public static class TestGameContent
{
    public static readonly JsonGameContent Instance = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
}
