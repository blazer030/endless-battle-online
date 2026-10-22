using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Infrastructure.Randomness;
using EndlessBattle.Infrastructure.Security;
using EndlessBattle.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("EndlessBattle")
            ?? throw new InvalidOperationException("缺少連線字串 ConnectionStrings:EndlessBattle");
        services.AddDbContext<EndlessBattleDbContext>(options => options.UseMySQL(connectionString));

        var contentDirectory = configuration["Content:Directory"]
            ?? throw new InvalidOperationException("缺少設定 Content:Directory");
        var gameContent = JsonGameContent.Load(Path.GetFullPath(Path.Combine(contentRootPath, contentDirectory)));
        services.AddSingleton<IGameContent>(gameContent);

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<IJobProficiencyRepository, JobProficiencyRepository>();
        services.AddScoped<ISkillBookRepository, SkillBookRepository>();
        services.AddScoped<ISkillLoadoutRepository, SkillLoadoutRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IDepotRepository, DepotRepository>();
        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHashing, IdentityPasswordHashing>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomSourceFactory, SystemRandomSourceFactory>();
        return services;
    }
}
