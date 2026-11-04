using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

public class ApiHostStartupTests(MySqlDatabaseFixture database)
{
    private const int ConcurrentHosts = 48;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // 回歸：測試主機監看設定檔變更時，同時啟動大量主機偶爾會卡滿 5 分鐘逾時（見 HostConfigurationSetup）
    [Fact]
    public async Task Hosts_ManyStartedAtOnceAllServeRequests()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, ConcurrentHosts).Select(_ => Task.Run(async () =>
        {
            await using var factory = new EndlessBattleApiFactory(database);
            return (await factory.CreateClient().GetAsync("/api/content", CancellationToken)).IsSuccessStatusCode;
        }, CancellationToken)));

        Assert.All(responses, Assert.True);
    }
}
