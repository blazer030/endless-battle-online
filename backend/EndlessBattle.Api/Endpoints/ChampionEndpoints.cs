using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Champion;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Champion;

namespace EndlessBattle.Api.Endpoints;

public static class ChampionEndpoints
{
    public static IEndpointRouteBuilder MapChampionEndpoints(this IEndpointRouteBuilder app)
    {
        var champion = app.MapGroup("/api/champion").WithTags("Champion").RequireAuthorization();

        champion.MapGet("/", async (ClaimsPrincipal user, ChampionService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken))));

        champion.MapPost("/challenge", async (
            ClaimsPrincipal user, ChampionService service, CharacterService characters, IGameContent content, CancellationToken cancellationToken) =>
        {
            var result = await service.ChallengeAsync(user.GetAccountId(), cancellationToken);
            var profile = await characters.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new ChampionChallengeResponse(
                ToResponse(result.View),
                result.Events,
                result.Won,
                CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content)));
        });

        return app;
    }

    private static ChampionResponse ToResponse(ChampionView view)
    {
        var seat = view.Seat;
        return new ChampionResponse(seat.Name, seat.Level, seat.Element, seat.Hp, seat.HpMax, seat.Sp, seat.SpMax, seat.At, seat.Mat, seat.Df, seat.Mdf,
            seat.Agi, seat.Au, seat.IsPlayer, seat.Defenses, seat.CrownedAt, seat.LockedUntil, ChampionRules.ChallengeFee, view.Reasons);
    }
}
