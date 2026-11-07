using EndlessBattle.Application.Admin.Content;
using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Items;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Rest;
using EndlessBattle.Application.Skills;
using EndlessBattle.Application.World;
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

        var draftDirectory = configuration["Admin:DraftDirectory"]
            ?? throw new InvalidOperationException("缺少設定 Admin:DraftDirectory");
        var draftStore = new FileContentDraftStore(
            Path.GetFullPath(Path.Combine(contentRootPath, contentDirectory)),
            Path.GetFullPath(Path.Combine(contentRootPath, draftDirectory)));
        services.AddSingleton<IContentDraftStore>(draftStore);
        services.AddSingleton<IContentRecordStore<MonsterRecord>>(new CodecContentRecordStore<MonsterRecord>(draftStore, new MonsterCodec()));
        services.AddSingleton<IContentRecordStore<JobRecord>>(new CodecContentRecordStore<JobRecord>(draftStore, new JobCodec()));
        services.AddSingleton<IContentRecordStore<SkillRecord>>(new CodecContentRecordStore<SkillRecord>(draftStore, new SkillCodec()));
        services.AddSingleton<IContentRecordStore<AreaRecord>>(new CodecContentRecordStore<AreaRecord>(draftStore, new AreaCodec()));
        services.AddSingleton<IContentRecordStore<PlaceRecord>>(new CodecContentRecordStore<PlaceRecord>(draftStore, new PlaceCodec()));
        services.AddSingleton<IContentRecordStore<MissionSummaryRecord>>(new CodecContentRecordStore<MissionSummaryRecord>(draftStore, new MissionSummaryCodec()));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<IJobProficiencyRepository, JobProficiencyRepository>();
        services.AddScoped<ISkillBookRepository, SkillBookRepository>();
        services.AddScoped<ISkillLoadoutRepository, SkillLoadoutRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IDepotRepository, DepotRepository>();
        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IMercenaryRepository, MercenaryRepository>();
        services.AddScoped<IAreaPassRepository, AreaPassRepository>();
        services.AddScoped<IBuffRepository, BuffRepository>();
        services.AddScoped<ILodgingRepository, LodgingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDatabaseStatus, EfDatabaseStatus>();
        services.AddSingleton<IPasswordHashing, IdentityPasswordHashing>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomSourceFactory, SystemRandomSourceFactory>();
        return services;
    }
}
