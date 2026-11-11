using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Progression;

// WOG 的經驗、金錢、職業熟練度公式（spec §4，對應 wog_fight_m.php 的 get_exp／get_money）
public static class RewardFormulas
{
    private const double TimeoutLevelFactor = 0.6;
    private const double TimeoutDamageDivisor = 1.5;

    public static long Experience(
        MonsterDefinition m,
        // 擊殺時忽略；超限時傳 round(min(累計傷害, hpmax)/1.5)，愈高經驗愈多
        int monsterHealthPointsValue,
        int playerLevel,
        bool killed,
        IRandomSource random,
        double? levelOverride = null)
    {
        // 超過回合上限時呼叫端傳 m.Level × 0.6
        var monsterLevel = levelOverride ?? m.Level;
        var experienceBase = (m.Attack + m.MagicAttack) / 2.0;
        double e = PhpRandom.Between(random, experienceBase, experienceBase + monsterLevel);
        if (playerLevel <= 100)
        {
            e *= 1.3;
        }

        if (monsterLevel >= 450)
        {
            e *= 0.6;
        }

        // 擊殺時 WOG 傳入 m_hp = m_hpmax，比值為 1；未擊殺時比值乘兩次
        var healthPointsRatio = killed ? 1.0 : monsterHealthPointsValue / (double)m.HealthPoints;
        e = monsterLevel * e * (monsterLevel / playerLevel) * healthPointsRatio * 0.18;
        if (!killed)
        {
            e *= healthPointsRatio;
        }

        if (playerLevel <= 15)
        {
            e *= 2.5;
        }

        return (long)Math.Round(e * 2, MidpointRounding.AwayFromZero);
    }

    // 超限時 WOG 以怪物等級 × 0.6、round(min(累計傷害, hpmax) / 1.5) 計算部分經驗
    public static long TimeoutExperience(MonsterDefinition monster, int damageDealt, int playerLevel, IRandomSource random)
    {
        var damageValue = (int)Math.Round(Math.Min(damageDealt, monster.HealthPoints) / TimeoutDamageDivisor, MidpointRounding.AwayFromZero);
        return Experience(monster, damageValue, playerLevel, killed: false, random, levelOverride: monster.Level * TimeoutLevelFactor);
    }

    public static long Money(MonsterDefinition m, IRandomSource random)
    {
        double attackSum = m.Attack + m.MagicAttack;
        // At + Mat 為 0 時 WOG 的 hp/(at+mat) 除以零得 false，rand(0, false) = 0，金錢為 0
        if (attackSum == 0)
        {
            return 0;
        }

        double monsterLevel = m.Level;
        var levelRoll = PhpRandom.Between(random, monsterLevel / 2.5, monsterLevel);
        var attackRoll = PhpRandom.Between(random, attackSum / monsterLevel, m.HealthPoints / attackSum * 0.4);
        return (long)Math.Round(levelRoll * (attackRoll * 0.4), MidpointRounding.AwayFromZero);
    }

    // 每次擊殺的職業熟練度與技能點增量（WOG wog_fight_select.php 用 floor）
    public static long JobExperience(MonsterDefinition m) => m.JobExperience;
}
