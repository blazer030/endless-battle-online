using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Forge;

namespace EndlessBattle.Api.Endpoints;

public static class ForgeEndpoints
{
    public static IEndpointRouteBuilder MapForgeEndpoints(this IEndpointRouteBuilder app)
    {
        var forge = app.MapGroup("/api/forge").WithTags("Forge").RequireAuthorization();

        forge.MapPost("/refine", async (RefineRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            var stoneInventoryId = request.StoneInventoryId ?? throw new InputValidationException("缺少精鍊石 id");
            return TypedResults.Ok(ToResponse(await service.RefineAsync(
                user.GetAccountId(), inventoryId, stoneInventoryId, request.UseSafeStone ?? false, cancellationToken)));
        });

        forge.MapPost("/socket", async (SocketRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            var hole = request.Hole ?? throw new InputValidationException("缺少孔位");
            var stoneInventoryId = request.StoneInventoryId ?? throw new InputValidationException("缺少魔石 id");
            return TypedResults.Ok(ToResponse(await service.SocketAsync(user.GetAccountId(), inventoryId, hole, stoneInventoryId, cancellationToken)));
        });

        forge.MapPost("/unsocket", async (UnsocketRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            var hole = request.Hole ?? throw new InputValidationException("缺少孔位");
            var result = await service.UnsocketAsync(user.GetAccountId(), inventoryId, hole, cancellationToken);
            return TypedResults.Ok(new UnsocketResponse(true, result.Recovered, InventoryEndpoints.ToResponse(result.Inventory)));
        });

        forge.MapPost("/drill", async (DrillRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            return TypedResults.Ok(ToResponse(await service.DrillAsync(user.GetAccountId(), inventoryId, cancellationToken)));
        });

        forge.MapPost("/merge-stones", async (MergeStonesRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var stoneInventoryId = request.StoneInventoryId ?? throw new InputValidationException("缺少精鍊石 id");
            var quantityPairs = request.QuantityPairs ?? throw new InputValidationException("缺少合成組數");
            return TypedResults.Ok(ToResponse(await service.MergeStonesAsync(user.GetAccountId(), stoneInventoryId, quantityPairs, cancellationToken)));
        });

        return app;
    }

    private static ForgeResponse ToResponse(ForgeResult result) => new(result.Success, InventoryEndpoints.ToResponse(result.Inventory));
}
