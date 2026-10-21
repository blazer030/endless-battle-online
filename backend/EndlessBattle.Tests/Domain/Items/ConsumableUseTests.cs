using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Items;

public class ConsumableUseTests
{
    private static readonly ItemDefinition BagExpansion = TestItems.Consumable("bag", ConsumableKind.BagExpansion) with { Amount = 5 };
    private static readonly ItemDefinition DepotExpansion = TestItems.Consumable("depot", ConsumableKind.DepotExpansion) with { Amount = 5 };

    // 擴充只在加成會變大時可用；目前已 ≥ 擴充格數 → noEffect
    [Theory]
    [InlineData(4, new string[0])]
    [InlineData(5, new[] { "noEffect" })]
    [InlineData(8, new[] { "noEffect" })]
    public void CanUse_ExpansionNeedsEffect(int bonus, string[] expected)
    {
        var character = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with { BagBonus = bonus, DepotBonus = bonus };

        Assert.Equal(expected, ConsumableUse.CanUse(character, BagExpansion));
        Assert.Equal(expected, ConsumableUse.CanUse(character, DepotExpansion));
    }

    [Fact]
    public void CanUse_RejectsSupply()
    {
        var character = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);

        Assert.Equal(["notUsable"], ConsumableUse.CanUse(character, TestItems.Consumable("supply")));
    }
}
