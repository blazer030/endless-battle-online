using System.Net;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// 名稱改由戰鬥事件與 /api/public/ 提供，舊的內容總表已移除
public class ContentEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetContent_RespondsNotFound()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/content", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
