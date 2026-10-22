using EndlessBattle.Domain.Champion;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Champion;

public class ChampionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 4, 0, 0, TimeSpan.Zero);

    private static readonly ChampionSeat Seat = new("測試冠軍", Level: 5, Element: 0, SpecialAttackName: "測試劍", Hp: 120, HpMax: 120, Sp: 20, SpMax: 20,
        At: 20, Mat: 5, Df: 8, Mdf: 5, Agi: 10, Au: 6);

    [Fact]
    public void CanChallenge_ReasonsInOrder()
    {
        var broke = TestCharacters.Default(1, "挑戰者", 1, 1, Now) with { Money = 499, Hp = 0 };
        var ready = TestCharacters.Default(1, "挑戰者", 1, 1, Now) with { Money = 500 };

        Assert.Equal(["championLocked", "notEnoughMoney", "noHp"], ChampionRules.CanChallenge(broke, Seat with { LockedUntil = Now.AddSeconds(1) }, Now));
        Assert.Equal(["notEnoughMoney", "noHp"], ChampionRules.CanChallenge(broke, Seat with { LockedUntil = Now }, Now));
        Assert.Empty(ChampionRules.CanChallenge(ready, Seat, Now));
    }
}
