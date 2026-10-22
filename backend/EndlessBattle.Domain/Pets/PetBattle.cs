using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Pets;

// 個性加成已套用；Guard 是這一場的代擋量
public sealed record PetCombatant(string Name, double Attack, double Magic, int Guard, double Activation);

public readonly record struct PetStrikeResult(AttackKind Kind, bool Hit, bool Critical, int Damage);

public static class PetBattle
{
    public const double MaxActivation = 35;

    private const double PersonalityBonus = 1.2;
    private const double DesperateSpiritBonus = 1.5;
    private const double MaxBondScore = 255;
    private const double BondWeight = 10;
    private const double BaseCriticalChance = 10;
    private const double MaximumCriticalChance = 20;
    private const double BaseCriticalMultiplier = 1.2;
    private const double MaximumCriticalMultiplier = 2;
    private const int PetBonusDamage = 1;

    // player／monster 是含裝備的有效能力；怪物 au 為 0 時當 1
    public static PetCombatant ToCombatant(Pet pet, CombatantState player, CombatantState monster)
    {
        var attack = pet.Personality == PetPersonality.Aggressive ? pet.Attack * PersonalityBonus : pet.Attack;
        var magic = pet.Personality == PetPersonality.Cold ? pet.Magic * PersonalityBonus : pet.Magic;
        var guard = pet.Personality == PetPersonality.Ironwall ? DerivedStats.RoundLikePhp(pet.Defense * PersonalityBonus) : pet.Defense;
        var spirit = pet.Personality == PetPersonality.Desperate ? pet.Spirit * DesperateSpiritBonus : pet.Spirit;

        var monsterAu = monster.Au == 0 ? 1 : monster.Au;
        var activation = pet.Intimacy / MaxBondScore * BondWeight
            + spirit / MaxBondScore * BondWeight
            + DerivedStats.RoundLikePhp((double)player.Au / monsterAu);
        return new PetCombatant(pet.Name, attack, magic, guard, Math.Min(MaxActivation, activation));
    }

    // 代擋與出手共用：rand(1,100) < 出擊率
    public static bool Activates(PetCombatant pet, IRandomSource random) => PhpRandom.Between(random, 1, 100) < pet.Activation;

    // 出擊成功後的一擊：方式 rand(1,2) → 命中 rand(1,3) < 3 → 暴擊 → 傷害（PHP fight(..., 1, 1, 1)：無屬性、附加 1 點）。
    // player／monster 是本回合套用技能修正後的能力
    public static PetStrikeResult Strike(PetCombatant pet, CombatantState player, CombatantState monster, IRandomSource random)
    {
        var kind = PhpRandom.Between(random, 1, 2) == 1 ? AttackKind.Physical : AttackKind.Magic;
        if (PhpRandom.Between(random, 1, 3) == 3)
        {
            return new PetStrikeResult(kind, Hit: false, Critical: false, Damage: 0);
        }

        var ratio = (double)(player.Au + player.Agi) / Math.Max(1, monster.Au + monster.Agi);
        var critical = PhpRandom.Between(random, 1, 100) < Math.Min(MaximumCriticalChance, ratio * BaseCriticalChance);
        var multiplier = critical ? Math.Min(MaximumCriticalMultiplier, ratio * BaseCriticalMultiplier) : 1;
        var (attack, defense) = kind == AttackKind.Physical ? (pet.Attack, monster.Df) : (pet.Magic, monster.Mdf);
        var damage = WogDamage.Fight(attack * multiplier, defense, elementMultiplier: 1.0, random, bonusDamage: PetBonusDamage);
        return new PetStrikeResult(kind, Hit: true, critical, damage);
    }
}
