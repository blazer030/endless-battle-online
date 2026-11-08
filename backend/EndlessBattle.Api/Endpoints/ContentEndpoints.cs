using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Api.Endpoints;

public static class ContentEndpoints
{
    private static readonly NamedContent[] Elements = [new("1", "地"), new("2", "水"), new("3", "火"), new("4", "木"), new("5", "風"), new("6", "毒")];

    private static readonly NamedContent[] Births = [new("1", "中央大陸"), new("2", "魔法王國"), new("3", "熱帶雨林"), new("4", "末日王城")];

    private static readonly NamedContent[] Sexes = [new("1", "男"), new("2", "女")];

    // 公開端點：給前端顯示名稱與創角選項用，不回傳戰鬥數值
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/content", (IGameContent content) =>
            TypedResults.Ok(new ContentResponse(
                content.AllMonsters.Select(monster => new NamedContent(monster.Id, monster.Name)).ToList(),
                content.AllJobs.Select(job => new JobContent(
                    job.Id,
                    job.Name,
                    job.Tier,
                    job.ParentId,
                    job.UnlockLevel,
                    job.ProficiencyRequired,
                    job.StatRequirements.Select(requirement => new StatRequirementContent(requirement.Stat, requirement.Minimum)).ToList(),
                    ToGrowthContent(job.Growth))).ToList(),
                Elements,
                Births,
                Sexes)))
            .WithTags("Content");

        return app;
    }

    internal static JobGrowthContent ToGrowthContent(JobGrowth growth) => new(
        [growth.Str.Min, growth.Str.Max],
        [growth.Smart.Min, growth.Smart.Max],
        [growth.Life.Min, growth.Life.Max],
        [growth.Vit.Min, growth.Vit.Max],
        [growth.Agi.Min, growth.Agi.Max],
        [growth.Au.Min, growth.Au.Max],
        [growth.Be.Min, growth.Be.Max]);
}
