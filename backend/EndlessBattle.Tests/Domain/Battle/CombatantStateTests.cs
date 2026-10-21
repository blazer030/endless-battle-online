using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Tests.Domain.Battle;

public class CombatantStateTests
{
    // 怪物每場都從滿 HP／SP 開始，沒有必殺技名
    [Fact]
    public void From_MonsterStartsAtFullHpAndSp()
    {
        var monster = new MonsterDefinition("frost-wolf", "寒狼", Level: 5, Hp: 95, Sp: 10, At: 20, Mat: 6, Df: 9, Mdf: 5, Agi: 13, Au: 7, Element: 2, JobExp: 18, Meet: 30);

        Assert.Equal(
            new CombatantState(Hp: 95, HpMax: 95, Sp: 10, SpMax: 10, At: 20, Mat: 6, Df: 9, Mdf: 5, Agi: 13, Au: 7, Element: 2, SpecialAttackName: null, Str: 20, Smart: 6),
            CombatantState.From(monster));
    }
}
