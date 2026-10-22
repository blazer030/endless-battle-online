using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(MySqlDatabaseFixture))]

namespace EndlessBattle.Tests.Support;

// 整個測試組件共用一個 MySQL 容器；測試之間靠隨機的帳號與名稱互不干擾
public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    private const int Attempts = 2;
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(3);

    private MySqlContainer? container;

    public string ConnectionString => container!.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        try
        {
            container = await ContainerStartup.StartWithRetryAsync(
                () => new MySqlBuilder("mysql:8.4")
                    .WithDatabase("endless_battle")
                    .WithUsername("ebo")
                    .WithPassword("ebo_test")
                    .WithLabel("ebo.tests", "true")
                    .Build(),
                (mySql, token) => mySql.StartAsync(token),
                mySql => mySql.DisposeAsync(),
                StartTimeout,
                Attempts);
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException(
                $"MySQL 測試容器在 {StartTimeout.TotalMinutes:0} 分鐘內沒有就緒（共 {Attempts} 次）；可先執行 scripts/clean-test-containers.sh",
                exception);
        }

        await MigrationRunner.ApplyAllAsync(ConnectionString, Path.Combine(BackendPaths.BackendDirectory, "db"));
    }

    public EndlessBattleDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<EndlessBattleDbContext>().UseMySQL(ConnectionString).Options);

    public async ValueTask DisposeAsync()
    {
        if (container is not null) await container.DisposeAsync();
    }
}
