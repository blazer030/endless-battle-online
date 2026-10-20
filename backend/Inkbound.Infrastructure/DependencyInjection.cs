using Inkbound.Application.Accounts;
using Inkbound.Application.Common;
using Inkbound.Application.Content;
using Inkbound.Infrastructure.Content;
using Inkbound.Infrastructure.Persistence;
using Inkbound.Infrastructure.Randomness;
using Inkbound.Infrastructure.Security;
using Inkbound.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkbound.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("Inkbound")
            ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:Inkbound");
        services.AddDbContext<InkboundDbContext>(options => options.UseMySQL(connectionString));

        var contentDirectory = configuration["Content:Directory"]
            ?? throw new InvalidOperationException("缺少設定 Content:Directory");
        var gameContent = JsonGameContent.Load(Path.GetFullPath(Path.Combine(contentRootPath, contentDirectory)));
        services.AddSingleton<IGameContent>(gameContent);

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddSingleton<IPasswordHashing, IdentityPasswordHashing>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomSourceFactory, SystemRandomSourceFactory>();
        return services;
    }
}
