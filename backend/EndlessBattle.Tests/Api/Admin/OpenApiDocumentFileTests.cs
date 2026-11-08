using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

// dotnet build 寫出的文件檔；兩個前端的 schema.ts 從這裡產生
public class OpenApiDocumentFileTests
{
    private static List<string> PathsIn(string fileName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "openapi", fileName)));
        return document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
    }

    [Fact]
    public void GameDocumentFile_HasOnlyGamePaths()
    {
        var paths = PathsIn("EndlessBattle.Api.json");

        Assert.Contains("/api/health", paths);
        Assert.All(paths, path => Assert.StartsWith("/api/", path, StringComparison.Ordinal));
    }

    [Fact]
    public void AdminDocumentFile_IncludesAdminRoutesEvenThoughAdminIsOffByDefault()
    {
        var paths = PathsIn("EndlessBattle.Api_admin.json");

        Assert.Contains("/admin/draft", paths);
        Assert.Contains("/admin/reports", paths);
        Assert.All(paths, path => Assert.StartsWith("/admin/", path, StringComparison.Ordinal));
    }
}
