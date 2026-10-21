using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Economy;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Api.Endpoints;

public static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        var shop = app.MapGroup("/api/shop").WithTags("Shop").RequireAuthorization();

        shop.MapGet("/", (ItemCategory? category, int? level, ShopService service) =>
        {
            var items = service.List(
                category ?? throw new InputValidationException("缺少分類"),
                level ?? throw new InputValidationException("缺少商店等級"));
            return TypedResults.Ok(items.Select(item => new ShopItemResponse(
                item.Id, item.Name, item.Category, item.Price, item.Bonus, item.Requires, item.Holes, item.RefineMax)).ToList());
        });

        shop.MapPost("/buy", async (BuyRequest request, ClaimsPrincipal user, ShopService service, CancellationToken cancellationToken) =>
        {
            var itemId = request.ItemId ?? throw new InputValidationException("缺少品項 id");
            var quantity = request.Quantity ?? throw new InputValidationException("缺少數量");
            return TypedResults.Ok(InventoryEndpoints.ToResponse(await service.BuyAsync(user.GetAccountId(), itemId, quantity, cancellationToken)));
        });

        shop.MapPost("/sell", async (SellRequest request, ClaimsPrincipal user, ShopService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            var quantity = request.Quantity ?? throw new InputValidationException("缺少數量");
            return TypedResults.Ok(InventoryEndpoints.ToResponse(await service.SellAsync(user.GetAccountId(), inventoryId, quantity, cancellationToken)));
        });

        return app;
    }
}
