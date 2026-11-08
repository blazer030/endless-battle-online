using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Tests.Domain.Battle;

public class ElementTableTests
{
    // 列是攻方、欄是守方；0 無屬性，1 地、2 水、3 火、4 木、5 風、6 毒。
    // 地→水→火→木→風→毒→地：攻方剋守方 ×1.2，被守方剋 ×0.8
    private static readonly double[,] Expected =
    {
        //        0    1    2    3    4    5    6
        /* 0 */ { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 },
        /* 1 */ { 1.0, 1.0, 1.2, 1.0, 1.0, 1.0, 0.8 },
        /* 2 */ { 1.0, 0.8, 1.0, 1.2, 1.0, 1.0, 1.0 },
        /* 3 */ { 1.0, 1.0, 0.8, 1.0, 1.2, 1.0, 1.0 },
        /* 4 */ { 1.0, 1.0, 1.0, 0.8, 1.0, 1.2, 1.0 },
        /* 5 */ { 1.0, 1.0, 1.0, 1.0, 0.8, 1.0, 1.2 },
        /* 6 */ { 1.0, 1.2, 1.0, 1.0, 1.0, 0.8, 1.0 },
    };

    public static TheoryData<int, int, double> AllPairs()
    {
        var data = new TheoryData<int, int, double>();
        for (var attacker = 0; attacker <= 6; attacker++)
        {
            for (var defender = 0; defender <= 6; defender++)
            {
                data.Add(attacker, defender, Expected[attacker, defender]);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Multiplier_FollowsElementCycle(int attackerElement, int defenderElement, double expected)
    {
        Assert.Equal(expected, ElementTable.Multiplier(attackerElement, defenderElement));
    }
}
