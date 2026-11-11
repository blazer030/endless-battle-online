using System.Globalization;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Api.Endpoints;

public static class ContentEndpoints
{
    private static readonly NamedContent[] Elements = ToNamedContent(CharacterOptions.Elements);

    private static readonly NamedContent[] Births = ToNamedContent(CharacterOptions.Births);

    private static readonly NamedContent[] Sexes = ToNamedContent(CharacterOptions.Sexes);

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
                    job.StatRequirements.Select(requirement => new StatRequirementContent(BaseStatCode.Of(requirement.Stat), requirement.Minimum)).ToList(),
                    ToGrowthContent(job.Growth))).ToList(),
                Elements,
                Births,
                Sexes)))
            .WithTags("Content");

        return app;
    }

    private static NamedContent[] ToNamedContent(IReadOnlyList<CharacterOption> options) =>
        options.Select(option => new NamedContent(option.Id.ToString(CultureInfo.InvariantCulture), option.Name)).ToArray();

    internal static JobGrowthContent ToGrowthContent(JobGrowth growth) => new(
        [growth.Strength.Minimum, growth.Strength.Maximum],
        [growth.Intelligence.Minimum, growth.Intelligence.Maximum],
        [growth.Life.Minimum, growth.Life.Maximum],
        [growth.Vitality.Minimum, growth.Vitality.Maximum],
        [growth.Agility.Minimum, growth.Agility.Maximum],
        [growth.Charisma.Minimum, growth.Charisma.Maximum],
        [growth.Faith.Minimum, growth.Faith.Maximum]);
}
