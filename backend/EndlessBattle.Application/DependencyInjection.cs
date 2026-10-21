using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Skills;
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
        return services;
    }
}
