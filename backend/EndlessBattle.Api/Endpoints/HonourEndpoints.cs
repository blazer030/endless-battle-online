using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Honour;

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

    private static HonourShopResponse ToResponse(HonourOverview overview, IGameContent content) => new(
        overview.Medals.Select(medal => new HonourMedalResponse(medal.Key, content.GetItem(medal.Key).Name, medal.Value)).ToList(),
        overview.Offers.Select(offer =>
        {
            var exchange = offer.Exchange;
            var (kind, rewardId, rewardName) = exchange.MercenaryId is { } mercenaryId
                ? ("mercenary", mercenaryId, content.GetMercenary(mercenaryId).Name)
                : ("item", exchange.ItemId!, content.GetItem(exchange.ItemId!).Name);
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
                offer.Affordable);
        }).ToList());
}
