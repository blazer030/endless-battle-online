using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class HealthEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_ReturnsOkWithoutLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/health", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_ReportsUnavailable_WhenDatabaseIsUnreachable()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        // 連到沒有服務的埠，連線會立刻被拒
        var client = factory
            .WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:EndlessBattle",
                "Server=127.0.0.1;Port=1;Database=endless_battle;User ID=ebo;Password=ebo_dev;Connection Timeout=2;"))
            .CreateClient();

        var response = await client.GetAsync("/api/health", CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("unavailable", body.GetProperty("status").GetString());
    }
}
