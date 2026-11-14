using EndlessBattle.Api.Endpoints;
using EndlessBattle.Application.Honour;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// 規格 honour「Exchange details in the listing」的 details 表：用正式內容轉成回應
public class HonourResponseTests
{
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"));

    private static HonourExchangeResponse Respond(string exchangeId, params HonourRequirement[] requirements)
    {
        var exchange = Content.HonourShop.Single(entry => entry.Id == exchangeId);
        var overview = new HonourOverview([new HonourOffer(exchange, true, requirements)], new Dictionary<string, int>(), null);
        return Assert.Single(HonourEndpoints.ToResponse(overview, Content).Exchanges);
    }

    [Fact]
    public void Weapon_HasBonusRequirementsHolesAndRefine()
    {
        var response = Respond("honour-9", new HonourRequirement(BaseStat.Strength, 145, true));

        Assert.Null(response.Mercenary);
        var item = response.Item!;
        Assert.Equal((ItemCategory.Weapon, 2, 5, true), (item.Category, item.Holes, item.RefineMax, item.Bound));
        Assert.Equal(new StatBonusResponse(5, 13, 0, 14, 0, 15, 16, 225, 0, 0, 0, 0), item.Bonus);
        Assert.Equal([new HonourRequirementResponse("strength", 145, true)], item.Requirements);
    }

    [Fact]
    public void Supply_HasSupplyHealthPoints()
    {
        var item = Respond("honour-133").Item!;

        Assert.Equal((ItemCategory.Consumable, ConsumableKind.Supply, 90000, 10), (item.Category, item.Consumable, item.SupplyHealthPoints, Respond("honour-133").Quantity));
        Assert.Empty(item.Requirements);
        Assert.Null(item.Key);
        Assert.Null(item.Elixir);
    }

    [Fact]
    public void Key_HasPlaceNameAndSeconds()
    {
        var item = Respond("honour-29").Item!;

        Assert.Equal((ItemCategory.Key, true), (item.Category, item.Bound));
        Assert.Equal(new HonourKeyResponse("靜止之城", 86400), item.Key);
    }

    [Fact]
    public void Elixir_HasKindMultiplierAndSeconds()
    {
        var exchange = Content.HonourShop.First(entry => entry.ItemId is { } id && Content.GetItem(id).Consumable == ConsumableKind.Elixir);
        var item = Respond(exchange.Id).Item!;

        Assert.Equal(new HonourElixirResponse(BuffKind.Attack, 1.05, 7200), item.Elixir);
    }

    [Fact]
    public void Mercenary_HasContractTerms()
    {
        var response = Respond("honour-1");

        Assert.Null(response.Item);
        Assert.Equal(new HonourMercenaryResponse(900, 70, 70, 100, 2), response.Mercenary);
    }
}
