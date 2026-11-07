using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminOpenApiTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<IReadOnlyList<string>> PathsOf(string document)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting("Admin:Enabled", "true")).CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>($"/openapi/{document}.json", CancellationToken);
        return body.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
    }

    [Fact]
    public async Task GameDocument_HasNoAdminPaths()
    {
        var paths = await PathsOf("v1");

        Assert.Contains("/api/health", paths);
        Assert.DoesNotContain(paths, path => path.StartsWith("/admin/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdminDocument_HasOnlyAdminPaths()
    {
        var paths = await PathsOf("admin");

        foreach (var kind in new[] { "items", "monsters", "jobs", "skills", "areas", "places", "missions" })
        {
            Assert.Contains($"/admin/content/{kind}", paths);
        }

        Assert.DoesNotContain("/admin/content/{kind}", paths);
        Assert.Contains("/admin/draft", paths);
        Assert.Contains("/admin/reports", paths);
        Assert.All(paths, path => Assert.StartsWith("/admin/", path, StringComparison.Ordinal));
    }
}
