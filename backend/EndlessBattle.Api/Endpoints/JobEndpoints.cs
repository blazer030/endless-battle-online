using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Api.Errors;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Jobs;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Api.Endpoints;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs").WithTags("Jobs").RequireAuthorization();

        jobs.MapGet("/", async (ClaimsPrincipal user, JobService service, CancellationToken cancellationToken) =>
        {
            var options = await service.ListAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(options.Select(ToResponse).ToList());
        });

        jobs.MapPost("/change", async (ChangeJobRequest request, ClaimsPrincipal user, JobService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.JobId))
            {
                throw RequestErrors.Invalid("請指定職業");
            }

            var profile = await service.ChangeAsync(user.GetAccountId(), request.JobId, cancellationToken);
            return TypedResults.Ok(CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content));
        });

        return app;
    }

    private static JobOptionResponse ToResponse(JobOption option) => new(
        option.Id,
        option.Name,
        option.Tier,
        option.ParentId,
        option.ParentName,
        option.UnlockLevel,
        option.LevelMet,
        option.ProficiencyRequired,
        option.ParentProficiencyMet,
        option.StatRequirements.Select(requirement => new JobStatCheckResponse(BaseStatCode.Of(requirement.Stat), requirement.Minimum, requirement.Met)).ToList(),
        ToResponse(option.Growth),
        option.Current,
        option.ProficiencyPercent,
        option.Eligible);

    private static JobGrowthResponse ToResponse(JobGrowth growth) => new(
        ToResponse(growth.Strength),
        ToResponse(growth.Intelligence),
        ToResponse(growth.Life),
        ToResponse(growth.Vitality),
        ToResponse(growth.Agility),
        ToResponse(growth.Charisma),
        ToResponse(growth.Faith));

    private static GrowthRangeResponse ToResponse(StatRange range) => new(range.Minimum, range.Maximum);
}
