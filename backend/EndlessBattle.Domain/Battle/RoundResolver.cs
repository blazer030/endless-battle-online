using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public readonly record struct AttackResult(bool Hit, bool Critical, int Damage, double ElementMultiplier);

// 回合內的擲骰判定：攻擊方式、先攻、單次攻擊
public static class RoundResolver
{
    private const double BaseCriticalChance = 10;
    private const double BaseCriticalMultiplier = 1.2;
    private const double MaximumCriticalChance = 20;
    private const double MaximumCriticalMultiplier = 2;

    // WOG m_type：at ≥ mat 物理；否則 rand(1,10) ≤ 8 魔法、其餘物理
    public static AttackKind ChooseKind(int at, int mat, IRandomSource random)
    {
        if (at >= mat)
        {
            return AttackKind.Physical;
        }

        return PhpRandom.Between(random, 1, 10) <= 8 ? AttackKind.Magic : AttackKind.Physical;
    }

    public static bool PlayerStrikesFirst(int playerAgi, int monsterAgi, IRandomSource random) =>
        PhpRandom.Between(random, 1, 100) <= (playerAgi > monsterAgi ? 70 : 30);

    // rag、暴擊率加成、附加傷害來自技能（BattleModifiers），只有玩家出手時不為 0
    public static AttackResult Strike(
        CombatantState attacker,
        CombatantState defender,
        AttackKind kind,
        IRandomSource random,
        int rag = 0,
        double criticalChanceBonus = 0,
        int bonusDamage = 0)
    {
        var physical = kind == AttackKind.Physical;

        // 魔法必中
        if (physical && !PhysicalHits(attacker.Agi, defender.Agi, rag, random))
        {
            return new AttackResult(Hit: false, Critical: false, Damage: 0, ElementMultiplier: 1.0);
        }

        var (criticalChance, criticalMultiplier) = physical
            ? CriticalOdds(attacker.Agi, defender.Agi)
            : CriticalOdds(attacker.Au, defender.Au);

        // 技能的暴擊率加成加在 20% 上限之後，不再封頂
        var critical = PhpRandom.Between(random, 1, 100) < criticalChance + criticalChanceBonus;
        var elementMultiplier = ElementTable.Multiplier(attacker.Element, defender.Element);
        var (attack, defense) = physical ? (attacker.At, defender.Df) : (attacker.Mat, defender.Mdf);
        var damage = WogDamage.Fight(critical ? attack * criticalMultiplier : attack, defense, elementMultiplier, random, bonusDamage: bonusDamage);
        return new AttackResult(Hit: true, critical, damage, elementMultiplier);
    }

    // 物理比 agi、魔法比 au；攻方較高時依比值提高，怪物與玩家用同一套
    private static (double Chance, double Multiplier) CriticalOdds(int attackerStat, int defenderStat)
    {
        if (attackerStat <= defenderStat)
        {
            return (BaseCriticalChance, BaseCriticalMultiplier);
        }

        var ratio = (double)attackerStat / defenderStat;
        return (Math.Min(MaximumCriticalChance, ratio * BaseCriticalChance), Math.Min(ratio * BaseCriticalMultiplier, MaximumCriticalMultiplier));
    }

    // agi/(agi+敵agi)×100 + rag + 20 > rand(1,100)；rag 目前只有技能（戰神之怒）會改，裝備加成在計畫 7
    private static bool PhysicalHits(int attackerAgi, int defenderAgi, int rag, IRandomSource random) =>
        (double)attackerAgi / (attackerAgi + defenderAgi) * 100 + rag + 20 > PhpRandom.Between(random, 1, 100);
}
