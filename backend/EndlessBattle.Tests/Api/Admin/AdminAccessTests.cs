using System.Net;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminAccessTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Admin_DisabledReturnsNotFound()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting("Admin:Enabled", "false")).CreateClient();

        var response = await client.GetAsync("/admin/content/items", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_EnabledServesAdminRoutes()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.WithWebHostBuilder(builder => builder.UseSetting("Admin:Enabled", "true")).CreateClient();

        var response = await client.GetAsync("/admin/content/items", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
