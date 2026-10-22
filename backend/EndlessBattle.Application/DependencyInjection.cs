using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Economy;
using EndlessBattle.Application.Forge;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Jobs;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Skills;
using EndlessBattle.Application.World;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<CharacterService>();
        services.AddScoped<AdventureService>();
        services.AddScoped<SkillService>();
        services.AddScoped<JobService>();
        services.AddScoped<WorldService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<ShopService>();
        services.AddScoped<BankService>();
        services.AddScoped<DepotService>();
        services.AddScoped<ForgeService>();
        services.AddScoped<MissionService>();
        services.AddScoped<PetService>();
        return services;
    }
}
