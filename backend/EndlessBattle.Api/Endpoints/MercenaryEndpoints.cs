using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Mercenaries;

namespace EndlessBattle.Api.Endpoints;

public static class MercenaryEndpoints
{
    public static IEndpointRouteBuilder MapMercenaryEndpoints(this IEndpointRouteBuilder app)
    {
        var mercenaries = app.MapGroup("/api/mercenaries").WithTags("Mercenaries").RequireAuthorization();

        mercenaries.MapGet("/", async (ClaimsPrincipal user, MercenaryService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken), content)));

        mercenaries.MapPost("/hire", async (
            MercenaryRequest request, ClaimsPrincipal user, MercenaryService service, CharacterService characters, IGameContent content,
            CancellationToken cancellationToken) =>
        {
            var mercenaryId = request.MercenaryId ?? throw new InputValidationException("缺少傭兵 id");
            var overview = await service.HireAsync(user.GetAccountId(), mercenaryId, cancellationToken);
            var profile = await characters.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new MercenaryHireResponse(
                ToResponse(overview, content),
                CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content)));
        });

        mercenaries.MapPost("/deploy", async (
            MercenaryRequest request, ClaimsPrincipal user, MercenaryService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var placeId = request.PlaceId ?? throw new InputValidationException("缺少地區 id");
            return TypedResults.Ok(ToResponse(await service.DeployAsync(user.GetAccountId(), placeId, cancellationToken), content));
        });

        mercenaries.MapPost("/rest", async (ClaimsPrincipal user, MercenaryService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.RestAsync(user.GetAccountId(), cancellationToken), content)));

        mercenaries.MapPost("/rename", async (
            MercenaryRequest request, ClaimsPrincipal user, MercenaryService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.RenameAsync(user.GetAccountId(), request.Name, cancellationToken), content)));

        mercenaries.MapPost("/dismiss", async (ClaimsPrincipal user, MercenaryService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.DismissAsync(user.GetAccountId(), cancellationToken), content)));

        return app;
    }

    // 內容檔移除的地區以 null 地名代替
    private static MercenaryOverviewResponse ToResponse(MercenaryOverview overview, IGameContent content) => new(
        overview.Catalogue.Select(mercenary => new MercenaryCatalogueResponse(
            mercenary.Id, mercenary.Name, mercenary.Price, mercenary.Fights, mercenary.MoneyPercent, mercenary.DropPercent,
            mercenary.ExperiencePercent, mercenary.DeathCost)).ToList(),
        overview.Contract is not { } contract
            ? null
            : new MercenaryContractResponse(
                contract.MercenaryId,
                contract.Name,
                contract.Remaining,
                contract.PlaceId,
                content.AllPlaces.FirstOrDefault(place => place.Id == contract.PlaceId)?.Name,
                contract.Fighting,
                contract.NextFightAt));
}
