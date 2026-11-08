using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Forge;
using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;

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

        forge.MapPost("/merge-cross", async (MergeCrossRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var firstInventoryId = request.FirstInventoryId ?? throw new InputValidationException("缺少第一格精鍊石 id");
            var secondInventoryId = request.SecondInventoryId ?? throw new InputValidationException("缺少第二格精鍊石 id");
            var quantityPairs = request.QuantityPairs ?? throw new InputValidationException("缺少合成組數");
            var inventory = await service.MergeCrossKindAsync(user.GetAccountId(), firstInventoryId, secondInventoryId, quantityPairs, cancellationToken);
            return TypedResults.Ok(new ForgeResponse(true, InventoryEndpoints.ToResponse(inventory)));
        });

        forge.MapGet("/recipes", async (ClaimsPrincipal user, ForgeService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var completed = await service.CompletedMissionIdsAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(content.AllRecipes.Select(recipe => ToResponse(recipe, completed, content)).ToList());
        });

        forge.MapPost("/synthesize", async (SynthesizeRequest request, ClaimsPrincipal user, ForgeService service, CancellationToken cancellationToken) =>
        {
            var recipeId = string.IsNullOrWhiteSpace(request.RecipeId) ? throw new InputValidationException("缺少配方 id") : request.RecipeId;
            var attempts = request.Attempts ?? throw new InputValidationException("缺少合成次數");
            var result = await service.SynthesizeAsync(user.GetAccountId(), recipeId, attempts, cancellationToken);
            return TypedResults.Ok(new SynthesisResponse(result.Successes, result.Attempts, result.Cost, InventoryEndpoints.ToResponse(result.Inventory)));
        });

        return app;
    }

    private static RecipeResponse ToResponse(Recipe recipe, IReadOnlySet<string> completedMissionIds, IGameContent content) => new(
        recipe.Id,
        recipe.ResultItemId,
        content.GetItem(recipe.ResultItemId).Name,
        recipe.ResultQuantity,
        recipe.Materials.Select(material => new RecipeMaterialResponse(material.ItemId, content.GetItem(material.ItemId).Name, material.Quantity)).ToList(),
        recipe.SuccessRate,
        Synthesis.CostPerAttempt,
        recipe.RequiredMissionId is { } missionId
            ? new RecipeRequiredMissionResponse(missionId, content.GetMission(missionId).Title, completedMissionIds.Contains(missionId))
            : null);

    private static ForgeResponse ToResponse(ForgeResult result) => new(result.Success, InventoryEndpoints.ToResponse(result.Inventory));
}
