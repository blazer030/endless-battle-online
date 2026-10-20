using Inkbound.Application.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace Inkbound.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        return services;
    }
}
