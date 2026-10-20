using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Api;

public class ContentEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetContent_ReturnsMonsterAndStageNames_WithoutLogin()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/content", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(CancellationToken);
        Assert.DoesNotContain("\"maxHealth\"", text);

        var content = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal(
            ["test-sprite:測試雪精", "test-wolf:測試寒狼"],
            content.GetProperty("monsters").EnumerateArray().Select(item => $"{item.GetProperty("id").GetString()}:{item.GetProperty("name").GetString()}"));
        Assert.Equal(
            ["test-stage-1:測試關卡・一"],
            content.GetProperty("stages").EnumerateArray().Select(item => $"{item.GetProperty("id").GetString()}:{item.GetProperty("name").GetString()}"));
    }
}
