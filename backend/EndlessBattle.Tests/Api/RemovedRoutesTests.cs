using System.Net;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class RemovedRoutesTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Champion_RoutesAreGone()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        // 沒有角色時，路由還在也會回 404，所以要先建角色
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"移除{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/champion", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/champion/challenge", null, CancellationToken)).StatusCode);
    }
}
