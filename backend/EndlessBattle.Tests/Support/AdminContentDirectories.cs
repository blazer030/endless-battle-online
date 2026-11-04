namespace EndlessBattle.Tests.Support;

// admin 測試用的正式內容與草稿資料夾：TestContent 的暫存副本，測試結束刪除，不會改到 TestContent 本身
public sealed class AdminContentDirectories : IDisposable
{
    public AdminContentDirectories()
    {
        Root = Path.Combine(Path.GetTempPath(), $"ebo-admin-{Guid.NewGuid():N}");
        Official = Path.Combine(Root, "content");
        Draft = Path.Combine(Root, "content-draft");
        Directory.CreateDirectory(Official);
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), "*.json"))
        {
            File.Copy(file, Path.Combine(Official, Path.GetFileName(file)));
        }
    }

    public string Root { get; }

    public string Official { get; }

    public string Draft { get; }

    public HttpClient CreateClient(EndlessBattleApiFactory factory) =>
        factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Admin:Enabled", "true");
                builder.UseSetting("Content:Directory", Official);
                builder.UseSetting("Admin:DraftDirectory", Draft);
            })
            .CreateClient();

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
