using System.Security.Claims;
using Inkbound.Api.Auth;
using Inkbound.Application.Characters;
using Inkbound.Domain.Characters;

namespace Inkbound.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var characters = app.MapGroup("/api/characters").WithTags("Characters").RequireAuthorization();

        characters.MapPost("/", async (CreateCharacterRequest request, ClaimsPrincipal user, CharacterService service, CancellationToken cancellationToken) =>
        {
            var character = await service.CreateAsync(user.GetAccountId(), request.Name ?? "", cancellationToken);
            return TypedResults.Created("/api/characters/me", ToResponse(character));
        });

        characters.MapGet("/me", async (ClaimsPrincipal user, CharacterService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken))));

        return app;
    }

    public static CharacterResponse ToResponse(Character character) =>
        new(character.Name, character.Experience, character.SpiritStones, character.CurrentStageId);
}
