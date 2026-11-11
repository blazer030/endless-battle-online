using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Domain;

public class DerivedStatsTests
{
    [Fact]
    public void Derived_MatchesWogFormulas()
    {
        var derived = DerivedStats.From(new BaseStats(Strength: 17, Intelligence: 12, Life: 15, Vitality: 10, Agility: 14, Charisma: 10, Faith: 10));

        Assert.Equal(17, derived.Attack);
        Assert.Equal(12, derived.MagicAttack);
        Assert.Equal(10, derived.Defense);
        Assert.Equal(10, derived.MagicDefense);
        Assert.Equal(425, derived.MaxHealthPoints);
        Assert.Equal(91, derived.MaxManaPoints);
    }

    // 七項各不相同，換錯欄位就會被抓到
    [Fact]
    public void Derived_UsesEachStatDistinctly()
    {
        var derived = DerivedStats.From(new BaseStats(Strength: 1, Intelligence: 2, Life: 3, Vitality: 4, Agility: 5, Charisma: 6, Faith: 7));

        Assert.Equal(new DerivedStats(Attack: 1, MagicAttack: 2, Defense: 4, MagicDefense: 7, MaxHealthPoints: 125, MaxManaPoints: 52), derived);
    }

    // life×25 與 (smart+be)×3 在整數輸入下不會產生 .5，所以直接驗證四捨五入輔助函式與 PHP round 一致
    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -3)]
    [InlineData(2.4, 2)]
    public void Derived_RoundsHalfAwayFromZero(double value, int expected)
    {
        Assert.Equal(expected, DerivedStats.RoundLikePhp(value));
    }
    // 裸點 (12,12,12,10,12,10,10) + 加成 str 2、smart 1、life 4、vit 3、agi 5、au 6、be 2 → 有效 (14,13,16,13,17,16,12)；
    // at 28+14、mat 7+13、df 9+13、mdf 11+12、hpMax 16×25+50+50、spMax (13+12)×3+25
    private static readonly EquipmentBonus SampleBonus = new(new StatBonus(
        Strength: 2, Intelligence: 1, Life: 4, Vitality: 3, Agility: 5, Charisma: 6, Faith: 2, Attack: 28, MagicAttack: 7, Defense: 9, MagicDefense: 11, MaxHealthPoints: 50));

    private static readonly BaseStats SampleBase = new(Strength: 12, Intelligence: 12, Life: 12, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10);

    [Fact]
    public void Derived_AddsEquipmentBonus()
    {
        var derived = DerivedStats.From(SampleBase, SampleBonus);

        Assert.Equal(new DerivedStats(Attack: 42, MagicAttack: 20, Defense: 22, MagicDefense: 23, MaxHealthPoints: 500, MaxManaPoints: 100), derived);
    }

    [Fact]
    public void Effective_AddsSevenStatsOfTheBonus()
    {
        Assert.Equal(new BaseStats(Strength: 14, Intelligence: 13, Life: 16, Vitality: 13, Agility: 17, Charisma: 16, Faith: 12), SampleBonus.Effective(SampleBase));
    }

    [Fact]
    public void Derived_ZeroBonusMatchesBareFormula()
    {
        Assert.Equal(DerivedStats.From(SampleBase), DerivedStats.From(SampleBase, EquipmentBonus.Zero));
    }
}
