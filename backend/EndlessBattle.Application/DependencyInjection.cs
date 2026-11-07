using EndlessBattle.Application.Admin.Content;
using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Balance;
using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Economy;
using EndlessBattle.Application.Forge;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Jobs;
using EndlessBattle.Application.Honour;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Rest;
using EndlessBattle.Application.Skills;
using EndlessBattle.Application.World;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AccountService>();
        services.AddScoped<ContentAdminService>();
        services.AddScoped<ContentRecordService<MonsterRecord>>();
        services.AddScoped<ContentRecordService<ItemRecord>>();
        services.AddScoped<ContentRecordService<JobRecord>>();
        services.AddScoped<ContentRecordService<AreaRecord>>();
        services.AddScoped<ContentRecordService<PlaceRecord>>();
        services.AddScoped<ContentRecordService<SkillRecord>>();
        services.AddScoped<IContentRecordCheck<SkillRecord>, SkillJobCheck>();
        services.AddScoped<ContentRecordService<MissionSummaryRecord>>();
        services.AddScoped<IContentRecordCheck<MonsterRecord>, MonsterSkillCheck>();
        services.AddSingleton<BalanceReportService>();
        services.AddScoped<ContentReportService>();
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
        services.AddScoped<MercenaryService>();
        services.AddScoped<HonourService>();
        services.AddScoped<InnService>();
        return services;
    }
}
