using System.Security.Claims;
using Inkbound.Api.Auth;
using Inkbound.Application.Adventure;
using Inkbound.Application.Content;

namespace Inkbound.Api.Endpoints;

public static class AdventureEndpoints
{
    public static IEndpointRouteBuilder MapAdventureEndpoints(this IEndpointRouteBuilder app)
    {
        var adventure = app.MapGroup("/api/adventure").WithTags("Adventure").RequireAuthorization();

        adventure.MapPost("/advance", async (ClaimsPrincipal user, AdventureService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var result = await service.AdvanceAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new AdvanceResponse(
                result.Events,
                result.ExperienceGained,
                result.SpiritStonesGained,
                CharacterEndpoints.ToResponse(result.Character, content)));
        });

        return app;
    }
}
