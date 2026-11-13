using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Adventure;

// 上限固定 HpMax 100、SpMax 100，門檻直接是百分比
public class RestRulesTests
{
    private static readonly DerivedStats Derived = new(Attack: 10, MagicAttack: 10, Defense: 10, MagicDefense: 10, MaxHealthPoints: 100, MaxManaPoints: 100);

    private static Character Hero(int hp, int sp, AdventureSettings? settings = null) =>
        TestCharacters.Default(accountId: 1, "勇者", 1, 1, DateTimeOffset.UnixEpoch) with
        {
            HealthPoints = hp,
            ManaPoints = sp,
            Settings = settings ?? AdventureSettings.Default,
        };

    // 計畫 9 的固定門檻：HP < 15%；有技能時 SP < 15%；沒有技能時 SP 不看
    [Theory]
    [InlineData(14, 100, false, true)]
    [InlineData(15, 100, false, false)]
    [InlineData(100, 14, true, true)]
    [InlineData(100, 15, true, false)]
    [InlineData(100, 0, false, false)]
    public void ShouldRest_DefaultMatchesPlanNine(int hp, int sp, bool hasActiveSkills, bool expected)
    {
        Assert.Equal(expected, RestRules.ShouldRest(Hero(hp, sp), Derived, hasActiveSkills));
    }

    [Fact]
    public void ShouldRest_DeadAlwaysRests()
    {
        Assert.True(RestRules.ShouldRest(Hero(0, 100, AdventureSettings.Default with { RestHealthPointsPercent = -1 }), Derived, hasActiveSkills: false));
    }

    [Fact]
    public void ShouldRest_SpSkippedWhenMinusOne()
    {
        Assert.False(RestRules.ShouldRest(Hero(100, 0, AdventureSettings.Default with { RestManaPointsPercent = -1 }), Derived, hasActiveSkills: true));
    }

    [Fact]
    public void ShouldRest_HpSkippedWhenMinusOne()
    {
        Assert.False(RestRules.ShouldRest(Hero(1, 100, AdventureSettings.Default with { RestHealthPointsPercent = -1 }), Derived, hasActiveSkills: false));
    }

    // 規格 inn：Rest triggers 表（HpMax、SpMax 100）
    [Theory]
    [InlineData(0, 100, -1, -1, false, true)]
    [InlineData(14, 100, 15, 15, false, true)]
    [InlineData(15, 100, 15, 15, false, false)]
    [InlineData(100, 10, 15, 15, false, false)]
    [InlineData(100, 10, 15, 15, true, true)]
    [InlineData(10, 100, -1, 15, true, false)]
    public void ShouldRest_TriggerTable(int hp, int sp, int hpThreshold, int spThreshold, bool hasActiveSkills, bool expected)
    {
        var settings = AdventureSettings.Default with { RestHealthPointsPercent = hpThreshold, RestManaPointsPercent = spThreshold };

        Assert.Equal(expected, RestRules.ShouldRest(Hero(hp, sp, settings), Derived, hasActiveSkills));
    }
}
