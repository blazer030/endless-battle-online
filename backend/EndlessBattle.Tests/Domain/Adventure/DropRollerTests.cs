using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Adventure;

// 每項擲 rand(1, ⌊topr × 0.9 × (行動力不足 ? 1.5 : 1)⌋)，結果 ≤ 1 才掉落；ScriptedRandom 的值 v 對 Between(1, n) 取 ⌊v × n⌋ + 1
public class DropRollerTests
{
    // 兩項各 topr 10 → 分母 9：第一項 0.0 → 1 命中；第二項 0.5 → ⌊4.5⌋ + 1 = 5 落空
    [Fact]
    public void Roll_EachEntryIndependent()
    {
        DropEntry[] drops = [new("supply", 10), new("ore", 10)];

        var dropped = DropRoller.Roll(drops, fDown: 1, new ScriptedRandom(0.0, 0.0, 0.5));

        Assert.Equal(["supply"], dropped);
    }

    // topr 10：正常分母 9、行動力不足 ⌊13.5⌋ = 13。擲 0.1：正常 ⌊0.9⌋ + 1 = 1 命中；不足 ⌊1.3⌋ + 1 = 2 落空
    [Fact]
    public void Roll_DepletedStaminaHalvesChance()
    {
        DropEntry[] drops = [new("supply", 10)];

        Assert.Equal(["supply"], DropRoller.Roll(drops, fDown: 1, new ScriptedRandom(0.1)));
        Assert.Empty(DropRoller.Roll(drops, fDown: 0.5, new ScriptedRandom(0.1)));
    }

    // topr 1 → 0.9 → 分母 0 < 1：一定掉落且不消耗亂數
    [Fact]
    public void Roll_ToprBelowOneAlwaysDrops()
    {
        var random = new ScriptedRandom(0.99);

        var dropped = DropRoller.Roll([new DropEntry("supply", 1)], fDown: 1, random);

        Assert.Equal(["supply"], dropped);
        Assert.Equal(0, random.Consumed);
    }

    // topr 10 → 分母 9 擲一次；topr 2 → ⌊1.8⌋ = 1，分母 1 仍要擲（一定命中）；topr 1 → 分母 0，不擲。共 2 次
    [Fact]
    public void Roll_ConsumesOneRollPerEntry()
    {
        var random = new ScriptedRandom(0.0);

        DropRoller.Roll([new DropEntry("a", 10), new DropEntry("b", 2), new DropEntry("c", 1)], fDown: 1, random);

        Assert.Equal(2, random.Consumed);
    }

    // 傭兵：topr 10 × 0.9 × 1.7 → ⌊15.3⌋ = 15。0.066 → ⌊0.99⌋ + 1 = 1 命中；0.067 → ⌊1.005⌋ + 1 = 2 落空
    // （分母 14 時 0.067 也命中、分母 16 時 0.066 也落空）
    [Fact]
    public void DropRoller_AppliesDenominatorFactor()
    {
        DropEntry[] drops = [new("supply", 10)];

        Assert.Equal(["supply"], DropRoller.Roll(drops, fDown: 1, new ScriptedRandom(0.066), denominatorFactor: 1.7));
        Assert.Empty(DropRoller.Roll(drops, fDown: 1, new ScriptedRandom(0.067), denominatorFactor: 1.7));
    }
}
