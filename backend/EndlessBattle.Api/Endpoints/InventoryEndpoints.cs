using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Inventory;

namespace EndlessBattle.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var inventory = app.MapGroup("/api/inventory").WithTags("Inventory").RequireAuthorization();

        inventory.MapGet("/", async (ClaimsPrincipal user, InventoryService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken))));

        inventory.MapPost("/equip", async (EquipRequest request, ClaimsPrincipal user, InventoryService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            return TypedResults.Ok(ToResponse(await service.EquipAsync(user.GetAccountId(), inventoryId, request.Quantity, cancellationToken)));
        });

        inventory.MapPost("/unequip", async (UnequipRequest request, ClaimsPrincipal user, InventoryService service, CancellationToken cancellationToken) =>
        {
            var slot = request.Slot ?? throw new InputValidationException("缺少裝備欄位");
            return TypedResults.Ok(ToResponse(await service.UnequipAsync(user.GetAccountId(), slot, cancellationToken)));
        });

        inventory.MapPost("/use", async (UseItemRequest request, ClaimsPrincipal user, InventoryService service, CancellationToken cancellationToken) =>
        {
            var inventoryId = request.InventoryId ?? throw new InputValidationException("缺少道具 id");
            return TypedResults.Ok(ToResponse(await service.UseAsync(user.GetAccountId(), inventoryId, cancellationToken)));
        });

        return app;
    }

    internal static InventoryResponse ToResponse(InventoryView view) => new(
        view.Bag.Select(category => new BagCategoryResponse(category.Category, category.Capacity, category.Items.Select(ToItem).ToList())).ToList(),
        view.Equipped.Select(equipped => new EquippedItemResponse(equipped.Slot, equipped.Quantity, ToItem(equipped.Item))).ToList(),
        view.Bonus.Total);

    internal static InventoryItemResponse ToItem(InventoryItem item) => new(
        item.Entry.Id,
        item.Item.Id,
        item.Item.Name,
        item.Item.Category,
        item.Item.Consumable,
        item.Entry.Quantity,
        item.Item.Bonus,
        item.Item.Requires,
        item.Item.JobId,
        item.Item.SupplyHp,
        item.Item.SupplySp,
        item.Item.Amount,
        item.Entry.RefineLevel,
        item.Item.RefineMax,
        item.Entry.RefineBonus,
        item.Entry.Holes,
        item.Entry.Sockets.Select(socket => new SocketResponse(socket.Hole, socket.StoneId, socket.Rolled)).ToList(),
        item.Item.Price,
        item.Item.Bound);
}
