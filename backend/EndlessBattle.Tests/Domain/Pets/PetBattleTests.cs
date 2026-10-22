using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Pets;

public class PetBattleTests
{
    private static CombatantState Side(int au) =>
        new(Hp: 100, HpMax: 100, Sp: 0, SpMax: 0, At: 10, Mat: 10, Df: 10, Mdf: 10, Agi: 10, au, Element: 0, SpecialAttackName: null, Str: 0, Smart: 0);

    // 出擊率 = 親密度/255×10 + 出擊值'/255×10 + round(玩家 au / 怪 au)，上限 35。
    // 255／255 積極、30 對 10 → 10 + 10 + 3 = 23；危急的出擊值 ×1.5 → 15 + 10 + 3 = 28；300 對 10 → 50 封頂 35；
    // 怪 au 0 視為 1 → 0 + 0 + 3 = 3；25 對 10 = 2.5 依 PHP round 進位 → 3
    [Theory]
    [InlineData(255, 255, PetPersonality.Aggressive, 30, 10, 23)]
    [InlineData(255, 255, PetPersonality.Desperate, 30, 10, 28)]
    [InlineData(255, 255, PetPersonality.Aggressive, 300, 10, 35)]
    [InlineData(0, 0, PetPersonality.Aggressive, 3, 0, 3)]
    [InlineData(0, 0, PetPersonality.Aggressive, 25, 10, 3)]
    public void Activation_FollowsWogFormula(int intimacy, int spirit, PetPersonality personality, int playerAu, int monsterAu, double expected)
    {
        var pet = TestPets.Create(personality: personality, spirit: spirit, intimacy: intimacy);

        var combatant = PetBattle.ToCombatant(pet, Side(playerAu), Side(monsterAu));

        Assert.Equal(expected, combatant.Activation, precision: 9);
    }

    // Attack 10、Magic 20、Defense 13：積極 Attack ×1.2、冷酷 Magic ×1.2、鐵壁代擋量 round(13 × 1.2 = 15.6) = 16；危急三項都不變
    [Theory]
    [InlineData(PetPersonality.Aggressive, 12, 20, 13)]
    [InlineData(PetPersonality.Cold, 10, 24, 13)]
    [InlineData(PetPersonality.Ironwall, 10, 20, 16)]
    [InlineData(PetPersonality.Desperate, 10, 20, 13)]
    public void Personality_AppliesWogBonuses(PetPersonality personality, double attack, double magic, int guard)
    {
        var pet = TestPets.Create("小狼", personality: personality, attack: 10, magic: 20, defense: 13);

        var combatant = PetBattle.ToCombatant(pet, Side(10), Side(10));

        Assert.Equal("小狼", combatant.Name);
        Assert.Equal(attack, combatant.Attack, precision: 9);
        Assert.Equal(magic, combatant.Magic, precision: 9);
        Assert.Equal(guard, combatant.Guard);
    }
}
