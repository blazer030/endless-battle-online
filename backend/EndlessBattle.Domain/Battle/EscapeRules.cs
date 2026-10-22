using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

// wog_fight_m.php 的逃跑：PHP 把整數當陣列取 agi，所有加成都失效，實際恆為 10%
public static class EscapeRules
{
    public const int Chance = 10;

    // 門檻 0 不逃
    public static bool ShouldTry(int hp, int hpMax, int escapeHpPercent) =>
        escapeHpPercent > 0 && (long)hp * 100 < (long)escapeHpPercent * hpMax;

    public static bool Succeeds(IRandomSource random) => PhpRandom.Between(random, 1, 100) <= Chance;
}
