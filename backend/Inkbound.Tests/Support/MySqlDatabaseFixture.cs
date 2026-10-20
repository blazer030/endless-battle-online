using Inkbound.Infrastructure.Persistence;
using Inkbound.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(MySqlDatabaseFixture))]

namespace Inkbound.Tests.Support;

// 整個測試組件共用一個 MySQL 容器；測試之間靠隨機的帳號與名稱互不干擾
public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    private readonly MySqlContainer container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("inkbound")
        .WithUsername("inkbound")
        .WithPassword("inkbound_test")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        await MigrationRunner.ApplyAllAsync(ConnectionString, Path.Combine(BackendPaths.BackendDirectory, "db"));
    }

    public InkboundDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<InkboundDbContext>().UseMySQL(ConnectionString).Options);

    public async ValueTask DisposeAsync() => await container.DisposeAsync();
}
