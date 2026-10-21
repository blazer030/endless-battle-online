using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Forge;

// 成敗先擲 r = rand(1, 100)，10n > r 失敗；成功後各能力依 Str、Smart、Life、Vit、Agi、Au、Be、At、Mat、Df、Mdf 的順序擲 rand(min, max)
public class RefineTests
{
    private static readonly ItemDefinition Sword = TestItems.Equipment("sword") with { RefineMax = 10 };

    private static ItemDefinition Stone(int level, StatRange? at = null, StatRange? df = null) =>
        TestItems.Item($"refine-stone-{level}", ItemCategory.RefineStone) with
        {
            Refine = new RefineStoneSpec(level, 1, at ?? new StatRange(1, 2), null, df, null, null, null, null, null, null, null, null),
        };

    private static InventoryEntry Equipment(int refineLevel) => TestItems.Entry(1, "sword") with { RefineLevel = refineLevel };

    // ScriptedRandom 的值 v 對 Between(low, high) 取 ⌊v × (high − low + 1)⌋ + low
    private static double RollOf(int value, int low = 1, int high = 100) => (value - low + 0.5) / (high - low + 1);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void Refine_SuccessRateIs101Minus10n(int n)
    {
        var succeeded = Refine.Attempt(Equipment(n - 1), Sword, Stone(n), useSafeStone: false, new ScriptedRandom(0.0, RollOf(10 * n)));
        var failed = Refine.Attempt(Equipment(n - 1), Sword, Stone(n), useSafeStone: false, new ScriptedRandom(0.0, RollOf(10 * n - 1)));

        Assert.True(succeeded.Success);
        Assert.Equal(n, succeeded.Entry.RefineLevel);
        Assert.False(failed.Success);
        Assert.Equal(n - 1, failed.Entry.RefineLevel);
    }

    // 擲 100 成功；at [1, 2] 擲 2、df [3, 5] 擲 4，累加到原有的 at 1
    [Fact]
    public void Refine_SuccessAddsRolledBonus()
    {
        var equipment = Equipment(0) with { RefineBonus = StatBonus.Zero with { At = 1 } };
        var random = new ScriptedRandom(0.0, RollOf(100), RollOf(2, 1, 2), RollOf(4, 3, 5));

        var outcome = Refine.Attempt(equipment, Sword, Stone(1, df: new StatRange(3, 5)), useSafeStone: false, random);

        Assert.True(outcome.Success);
        Assert.Equal(StatBonus.Zero with { At = 3, Df = 4 }, outcome.Entry.RefineBonus);
        Assert.Equal(3, random.Consumed);
        Assert.Equal(StatBonus.Zero with { At = 1 }, equipment.RefineBonus);
    }

    // +4 失敗（擲 39 < 40）：裝備原封不動、只扣精鍊石，失敗不再擲能力
    [Fact]
    public void Refine_FailureKeepsEquipment()
    {
        var equipment = Equipment(3) with
        {
            RefineBonus = StatBonus.Zero with { At = 7 },
            Sockets = [new SocketedStone(1, "attack-stone", StatBonus.Zero with { At = 5 })],
            Holes = 2,
        };
        var random = new ScriptedRandom(0.0, RollOf(39));

        var outcome = Refine.Attempt(equipment, Sword, Stone(4), useSafeStone: false, random);

        Assert.False(outcome.Success);
        Assert.Same(equipment, outcome.Entry);
        Assert.True(outcome.ConsumedStone);
        Assert.False(outcome.ConsumedSafeStone);
        Assert.Equal(1, random.Consumed);
    }

    // 失敗時扣安全精鍊石、保留精鍊石；成功時照常扣精鍊石，安全精鍊石不動
    [Fact]
    public void Refine_SafeStoneKeepsRefineStone()
    {
        var failed = Refine.Attempt(Equipment(0), Sword, Stone(1), useSafeStone: true, new ScriptedRandom(0.0, RollOf(9)));
        var succeeded = Refine.Attempt(Equipment(0), Sword, Stone(1), useSafeStone: true, new ScriptedRandom(0.0, RollOf(10)));

        Assert.Equal((false, false, true), (failed.Success, failed.ConsumedStone, failed.ConsumedSafeStone));
        Assert.Equal(0, failed.Entry.RefineLevel);
        Assert.Equal((true, true, false), (succeeded.Success, succeeded.ConsumedStone, succeeded.ConsumedSafeStone));
    }

    // 目標等級 n = 目前 + 1 必須等於石頭等級；違規時 Attempt 不擲骰直接拒絕
    [Fact]
    public void Refine_RequiresNextLevelStone()
    {
        Assert.Equal(["wrongStoneLevel"], Refine.Check(Equipment(2), Sword, Stone(2)));
        Assert.Equal(["wrongStoneLevel"], Refine.Check(Equipment(2), Sword, Stone(4)));
        Assert.Empty(Refine.Check(Equipment(2), Sword, Stone(3)));
        var random = new ScriptedRandom(0.99);
        Assert.Throws<InvalidOperationException>(() => Refine.Attempt(Equipment(2), Sword, Stone(2), useSafeStone: false, random));
        Assert.Equal(0, random.Consumed);
    }

    [Fact]
    public void Refine_StopsAtMax()
    {
        var capped = Sword with { RefineMax = 4 };

        Assert.Equal(["atRefineMax"], Refine.Check(Equipment(4), capped, Stone(5)));
        Assert.Empty(Refine.Check(Equipment(3), capped, Stone(4)));
    }

    [Fact]
    public void Refine_RejectsNonEquipmentAndNonRefineStone()
    {
        var ore = TestItems.Item("ore", ItemCategory.Material);

        Assert.Equal(["notEquipment"], Refine.Check(Equipment(0), ore, Stone(1)));
        Assert.Equal(["notRefineStone"], Refine.Check(Equipment(0), Sword, ore));
    }
}
