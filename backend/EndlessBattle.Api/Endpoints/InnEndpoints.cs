using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Rest;
using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Api.Endpoints;

public static class InnEndpoints
{
    public static IEndpointRouteBuilder MapInnEndpoints(this IEndpointRouteBuilder app)
    {
        var inn = app.MapGroup("/api/inn").WithTags("Inn").RequireAuthorization();

        inn.MapGet("/", async (ClaimsPrincipal user, InnService service, CancellationToken cancellationToken) =>
        {
            var view = await service.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new InnResponse(
                view.Inn.Rooms.Select(room => new InnRoomResponse(
                    room.Id, room.Name, room.Kind, room.Multiplier, view.Inn.Seconds, Inn.Price(view.Inn, room, view.Level))).ToList(),
                view.Lodging is { } lodging ? ToResponse(lodging) : null));
        });

        inn.MapPost("/book", async (
            InnBookRequest request, ClaimsPrincipal user, InnService service, CharacterService characters, IGameContent content,
            CancellationToken cancellationToken) =>
        {
            var roomId = request.RoomId ?? throw new InputValidationException("缺少房型");
            var result = await service.BookAsync(user.GetAccountId(), roomId, cancellationToken);
            var profile = await characters.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new InnBookResponse(
                CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content),
                ToResponse(result.Lodging),
                result.Cost));
        });

        return app;
    }

    private static LodgingResponse ToResponse(Lodging lodging) => new(lodging.RoomId, lodging.StartsAt, lodging.EndsAt);
}
