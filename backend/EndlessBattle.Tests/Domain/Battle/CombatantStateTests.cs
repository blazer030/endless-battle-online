using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Tests.Domain.Battle;

public class CombatantStateTests
{
    // 怪物每場都從滿 HP／SP 開始
    [Fact]
    public void From_MonsterStartsAtFullHpAndSp()
    {
        var monster = new MonsterDefinition("frost-wolf", "寒狼", Level: 5, HealthPoints: 95, ManaPoints: 10, Attack: 20, MagicAttack: 6, Defense: 9, MagicDefense: 5, Agility: 13, Charisma: 7, Element: 2, JobExperience: 18, Meet: 30);

        Assert.Equal(
            new CombatantState(HealthPoints: 95, MaxHealthPoints: 95, ManaPoints: 10, MaxManaPoints: 10, Attack: 20, MagicAttack: 6, Defense: 9, MagicDefense: 5, Agility: 13, Charisma: 7, Element: 2, Strength: 0, Intelligence: 0, Level: 5),
            CombatantState.From(monster));
    }
}
