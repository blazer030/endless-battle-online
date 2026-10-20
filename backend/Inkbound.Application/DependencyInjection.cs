using Inkbound.Application.Accounts;
using Inkbound.Application.Adventure;
using Inkbound.Application.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Inkbound.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<CharacterService>();
        services.AddScoped<AdventureService>();
        return services;
    }
}
