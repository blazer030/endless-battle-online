using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.World;

public class MonsterFormulaTests
{
    // L=1：hp 45+14+0.4=59.4、at 13+3.2=16.2、mat 4+2=6、df 5+1=6、mdf 2.9、agi 7.3、au 5.1、jobExp 9.6
    // L=10：hp 45+140+40=225、at 45、mat 24、df 15、mdf 11、agi 19、au 15、jobExp 24
    [Theory]
    [InlineData(1, 59, 12, 16, 6, 6, 3, 7, 5, 10)]
    [InlineData(10, 225, 30, 45, 24, 15, 11, 19, 15, 24)]
    public void Formula_MatchesTable(
        int level, int hp, int sp, int at, int mat, int df, int mdf, int agi, int au, long jobExp)
    {
        var baseline = MonsterFormula.Baseline(level);

        Assert.Equal(
            (level, hp, sp, at, mat, df, mdf, agi, au, jobExp),
            (baseline.Level, baseline.HealthPoints, baseline.ManaPoints, baseline.Attack, baseline.MagicAttack, baseline.Defense, baseline.MagicDefense, baseline.Agility, baseline.Charisma, baseline.JobExperience));
        Assert.Equal($"baseline-{level}", baseline.Id);
        Assert.Equal((0, 0, 0), (baseline.Element, baseline.Meet, baseline.PlaceId));
        Assert.Empty(baseline.SubPlaces);
    }

    // 0.5 的進位方向：L=5 時 mdf = 2+4.5 = 6.5 → AwayFromZero 為 7（銀行家捨入會得 6）
    [Fact]
    public void Formula_RoundsHalfAwayFromZero() =>
        Assert.Equal(7, MonsterFormula.Baseline(5).MagicDefense);

    // 校準目標：Lv1 戰士（at 14、df 10、HP 400）3～6 下打倒同級普通怪，每下受傷約 3～6% HP
    [Fact]
    public void Formula_LevelOneWarriorTradesSeveralHits()
    {
        var monster = MonsterFormula.Baseline(1);
        const int warriorAt = 14;
        const int warriorDf = 10;
        const int warriorHpMax = 400;

        var dealt = WogDamage.Fight(warriorAt, monster.Defense, elementMultiplier: 1.0, new ScriptedRandom(0.0));
        var taken = WogDamage.Fight(monster.Attack, warriorDf, elementMultiplier: 1.0, new ScriptedRandom(0.0));

        Assert.InRange((monster.HealthPoints + dealt - 1) / dealt, 3, 6);
        Assert.InRange((double)taken / warriorHpMax, 0.03, 0.06);
    }
}
