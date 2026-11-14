using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Honour;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;

namespace EndlessBattle.Api.Endpoints;

public static class HonourEndpoints
{
    public static IEndpointRouteBuilder MapHonourEndpoints(this IEndpointRouteBuilder app)
    {
        var honour = app.MapGroup("/api/honour").WithTags("Honour").RequireAuthorization();

        honour.MapGet("/", async (ClaimsPrincipal user, HonourService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken), content)));

        honour.MapPost("/exchange", async (
            HonourExchangeRequest request, ClaimsPrincipal user, HonourService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var exchangeId = string.IsNullOrWhiteSpace(request.ExchangeId)
                ? throw new InputValidationException("缺少兌換 id")
                : request.ExchangeId;
            return TypedResults.Ok(ToResponse(await service.ExchangeAsync(user.GetAccountId(), exchangeId, cancellationToken), content));
        });

        return app;
    }

    public static HonourShopResponse ToResponse(HonourOverview overview, IGameContent content) => new(
        overview.Medals.Select(medal => new HonourMedalResponse(medal.Key, content.GetItem(medal.Key).Name, medal.Value)).ToList(),
        overview.Offers.Select(offer =>
        {
            var exchange = offer.Exchange;
            var (kind, rewardId, rewardName) = exchange.MercenaryId is { } mercenaryId
                ? ("mercenary", mercenaryId, content.GetMercenary(mercenaryId).Name)
                : exchange.ItemId is { } itemId
                    ? ("item", itemId, content.GetItem(itemId).Name)
                    : throw new InvalidOperationException($"兌換 {exchange.Id} 沒有道具也沒有傭兵");
            return new HonourExchangeResponse(
                exchange.Id,
                kind,
                rewardId,
                rewardName,
                exchange.Quantity,
                exchange.Cost
                    .Select(cost => new HonourCostResponse(
                        cost.ItemId, content.GetItem(cost.ItemId).Name, cost.Quantity, overview.Medals.GetValueOrDefault(cost.ItemId)))
                    .ToList(),
                offer.Affordable,
                exchange.ItemId is { } detailId ? ItemDetail(content.GetItem(detailId), offer.Requirements, content) : null,
                exchange.MercenaryId is { } contractId ? MercenaryDetail(content.GetMercenary(contractId)) : null);
        }).ToList(),
        overview.HiredMercenaryName);

    private static HonourItemResponse ItemDetail(ItemDefinition item, IReadOnlyList<HonourRequirement> requirements, IGameContent content) => new(
        item.Category,
        item.Consumable,
        StatBonusResponse.From(item.Bonus),
        requirements.Select(requirement => new HonourRequirementResponse(BaseStatCode.Of(requirement.Stat), requirement.Minimum, requirement.Met)).ToList(),
        item.Holes,
        item.RefineMax,
        item.Bound,
        item.SupplyHealthPoints,
        item.SupplyManaPoints,
        item.Amount,
        item.Elixir is { } elixir ? new HonourElixirResponse(elixir.Kind, elixir.Multiplier, elixir.Seconds) : null,
        item.Key is { } key ? new HonourKeyResponse(content.GetPlace(key.PlaceId).Name, key.Seconds) : null);

    private static HonourMercenaryResponse MercenaryDetail(MercenaryDefinition mercenary) => new(
        mercenary.Fights, mercenary.MoneyPercent, mercenary.DropPercent, mercenary.ExperiencePercent, mercenary.DeathCost);
}
