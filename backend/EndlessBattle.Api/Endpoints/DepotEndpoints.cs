using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Economy;

namespace EndlessBattle.Api.Endpoints;

public static class DepotEndpoints
{
    public static IEndpointRouteBuilder MapDepotEndpoints(this IEndpointRouteBuilder app)
    {
        var depot = app.MapGroup("/api/depot").WithTags("Depot").RequireAuthorization();

        depot.MapGet("/", async (ClaimsPrincipal user, DepotService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken))));

        depot.MapPost("/store", async (StoreRequest request, ClaimsPrincipal user, DepotService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            var quantity = request.Quantity ?? throw new InputValidationException("缺少數量");
            return TypedResults.Ok(ToResponse(await service.StoreAsync(user.GetAccountId(), inventoryId, quantity, cancellationToken)));
        });

        depot.MapPost("/take", async (TakeRequest request, ClaimsPrincipal user, DepotService service, CancellationToken cancellationToken) =>
        {
            var depotId = request.DepotId ?? throw new InputValidationException("缺少倉庫道具 id");
            var quantity = request.Quantity ?? throw new InputValidationException("缺少數量");
            return TypedResults.Ok(ToResponse(await service.TakeAsync(user.GetAccountId(), depotId, quantity, cancellationToken)));
        });

        return app;
    }

    private static DepotResponse ToResponse(DepotView view) =>
        new(view.Entries.Select(InventoryEndpoints.ToItem).ToList(), view.Capacity);

    private static DepotMoveResponse ToResponse(DepotMove move) =>
        new(InventoryEndpoints.ToResponse(move.Inventory), ToResponse(move.Depot));
}
