using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.World;

namespace EndlessBattle.Api.Endpoints;

public static class WorldEndpoints
{
    public static IEndpointRouteBuilder MapWorldEndpoints(this IEndpointRouteBuilder app)
    {
        var world = app.MapGroup("/api/world").WithTags("World").RequireAuthorization();

        world.MapGet("/", async (ClaimsPrincipal user, WorldService service, CancellationToken cancellationToken) =>
        {
            var areas = await service.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(areas.Select(ToResponse).ToList());
        });

        world.MapPut("/location", async (MoveRequest request, ClaimsPrincipal user, WorldService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            if (request.PlaceId is not { } placeId)
            {
                throw new InputValidationException("請指定地區");
            }

            var profile = await service.MoveAsync(user.GetAccountId(), placeId, request.SubPlace, cancellationToken);
            return TypedResults.Ok(CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content));
        });

        return app;
    }

    private static WorldAreaResponse ToResponse(WorldAreaView area) => new(
        area.Id,
        area.Name,
        area.Places.Select(place => new WorldPlaceResponse(
            place.Id,
            place.Name,
            place.SubPlaces.Select(subPlace => new SubPlaceResponse(subPlace.Index, subPlace.Name)).ToList(),
            new LevelRangeResponse(place.Recommended.Min, place.Recommended.Max),
            place.Restricted,
            place.Current,
            place.Recommendation)).ToList());
}
