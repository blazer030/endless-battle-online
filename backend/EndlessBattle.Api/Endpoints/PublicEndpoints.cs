using EndlessBattle.Application.Jobs;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Api.Endpoints;

// 不用登入就能讀的遊戲資料；帶過期或無效的 token 也照樣回應
public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public").AllowAnonymous().WithTags("Public");

        group.MapGet("/jobs", (PublicJobService service) => TypedResults.Ok(service.GetAll().Select(ToResponse).ToList()));

        group.MapGet("/settings", () => TypedResults.Ok(new PublicSettingsResponse(
            CharacterOptions.Sexes.Select(ToResponse).ToList(),
            CharacterOptions.Elements.Select(element => new ElementOptionResponse(
                element.Id,
                element.Name,
                ElementTable.StrongAgainst(element.Id),
                ElementTable.WeakAgainst(element.Id))).ToList(),
            CharacterOptions.Births.Select(ToResponse).ToList())));

        return app;
    }

    private static OptionResponse ToResponse(CharacterOption option) => new(option.Id, option.Name);

    private static PublicJobResponse ToResponse(PublicJobView job) => new(
        job.Id,
        job.Name,
        job.Tier,
        job.ParentId,
        job.UnlockLevel,
        job.ProficiencyRequired,
        job.StatRequirements.Select(requirement => new JobStatRequirementResponse(requirement.Stat, requirement.Minimum)).ToList(),
        new JobGrowthResponse(
            ToResponse(job.Growth.Strength),
            ToResponse(job.Growth.Intelligence),
            ToResponse(job.Growth.Life),
            ToResponse(job.Growth.Vitality),
            ToResponse(job.Growth.Agility),
            ToResponse(job.Growth.Charisma),
            ToResponse(job.Growth.Faith)),
        job.Skills.Select(skill => new JobSkillResponse(
            skill.Id,
            skill.Name,
            skill.Description,
            skill.MaxLevel,
            skill.Prerequisite is { } prerequisite ? new SkillPrerequisiteResponse(prerequisite.SkillId, prerequisite.Name, prerequisite.Level) : null,
            skill.Levels.Select(level => new SkillLevelSummaryResponse(level.Level, level.Summary)).ToList())).ToList());

    private static GrowthRangeResponse ToResponse(GrowthRangeView range) => new(range.Minimum, range.Maximum);
}
