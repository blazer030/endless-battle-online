using Microsoft.AspNetCore.Mvc.Testing;

namespace EndlessBattle.Tests.Support;

// admin 測試用的正式內容與草稿資料夾：內容的暫存副本（預設 TestContent），測試結束刪除，不會改到來源
public sealed class AdminContentDirectories : IDisposable
{
    public AdminContentDirectories(string? sourceDirectory = null)
    {
        Root = Path.Combine(Path.GetTempPath(), $"ebo-admin-{Guid.NewGuid():N}");
        Official = Path.Combine(Root, "content");
        Draft = Path.Combine(Root, "content-draft");
        Directory.CreateDirectory(Official);
        foreach (var file in Directory.GetFiles(sourceDirectory ?? Path.Combine(AppContext.BaseDirectory, "TestContent"), "*.json"))
        {
            File.Copy(file, Path.Combine(Official, Path.GetFileName(file)));
        }
    }

    public string Root { get; }

    public string Official { get; }

    public string Draft { get; }

    // 指向這組資料夾、開啟 admin 的 API 主機；要讀主機內的服務時用它，只要送請求就用 CreateClient
    public WebApplicationFactory<Program> Configure(EndlessBattleApiFactory factory) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Admin:Enabled", "true");
            builder.UseSetting("Content:Directory", Official);
            builder.UseSetting("Admin:DraftDirectory", Draft);
        });

    public HttpClient CreateClient(EndlessBattleApiFactory factory) => Configure(factory).CreateClient();

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
