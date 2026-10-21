using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Progression;

// WOG 的經驗、金錢、職業熟練度公式（spec §4，對應 wog_fight_m.php 的 get_exp／get_money）
public static class RewardFormulas
{
    public static long Exp(
        MonsterDefinition m,
        // 擊殺時忽略；超限時傳 round(min(累計傷害, hpmax)/1.5)，愈高經驗愈多
        int monsterHpValue,
        int playerLevel,
        bool killed,
        double fDown,
        IRandomSource random,
        double? levelOverride = null)
    {
        // 超過回合上限時呼叫端傳 m.Level × 0.6
        var monsterLevel = levelOverride ?? m.Level;
        var expBase = (m.At + m.Mat) / 2.0;
        double e = PhpRandom.Between(random, expBase, expBase + monsterLevel);
        if (playerLevel <= 100)
        {
            e *= 1.3;
        }

        if (monsterLevel >= 450)
        {
            e *= 0.6;
        }

        // 擊殺時 WOG 傳入 m_hp = m_hpmax，比值為 1；未擊殺時比值乘兩次
        var hpRatio = killed ? 1.0 : monsterHpValue / (double)m.Hp;
        e = monsterLevel * e * (monsterLevel / playerLevel) * hpRatio * 0.18;
        if (!killed)
        {
            e *= hpRatio;
        }

        if (playerLevel <= 15)
        {
            e *= 2.5;
        }

        return (long)Math.Round(e * 2 * fDown, MidpointRounding.AwayFromZero);
    }

    public static long Money(MonsterDefinition m, double fDown, IRandomSource random)
    {
        double monsterLevel = m.Level;
        double attackSum = m.At + m.Mat;
        var levelRoll = PhpRandom.Between(random, monsterLevel / 2.5, monsterLevel);
        // At + Mat 為 0 時 WOG 會除以零，這裡把除數當 1
        var attackDivisor = attackSum == 0 ? 1.0 : attackSum;
        var attackRoll = PhpRandom.Between(random, attackSum / monsterLevel, m.Hp / attackDivisor * 0.4);
        return (long)Math.Round(levelRoll * (attackRoll * 0.4) * fDown, MidpointRounding.AwayFromZero);
    }

    // 每次擊殺的職業熟練度與技能點增量（WOG wog_fight_select.php 用 floor）
    public static long JobExp(MonsterDefinition m, double fDown) => (long)Math.Floor(m.JobExp * fDown);
}
