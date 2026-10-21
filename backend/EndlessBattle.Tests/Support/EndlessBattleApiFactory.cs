using EndlessBattle.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EndlessBattle.Tests.Support;

// 每個測試建立自己的 API 主機：共用 MySQL 容器，但時鐘與亂數各自獨立
public sealed class EndlessBattleApiFactory(MySqlDatabaseFixture database) : WebApplicationFactory<Program>
{
    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:EndlessBattle", database.ConnectionString);
        builder.UseSetting("Content:Directory", Path.Combine(AppContext.BaseDirectory, "TestContent"));
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-0123456789abcdef0123");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
            services.RemoveAll<IRandomSourceFactory>();
            services.AddSingleton<IRandomSourceFactory>(new FixedSeedRandomFactory(42));
        });
    }
}
