using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Champion;

namespace EndlessBattle.Tests.Domain.Champion;

public class ChampionSeatTests
{
    [Fact]
    public void ToCombatant_CarriesStatsAndSpecialAttack()
    {
        var seat = new ChampionSeat("格雷德", Level: 30, Element: 1, SpecialAttackName: "王者之劍", Hp: 900, HpMax: 1400, Sp: 100, SpMax: 175,
            At: 180, Mat: 25, Df: 150, Mdf: 40, Agi: 30, Au: 25);

        Assert.Equal(
            new CombatantState(Hp: 900, HpMax: 1400, Sp: 100, SpMax: 175, At: 180, Mat: 25, Df: 150, Mdf: 40, Agi: 30, Au: 25,
                Element: 1, SpecialAttackName: "王者之劍", Str: 0, Smart: 0),
            seat.ToCombatant());
    }
}
