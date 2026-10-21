using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.World;

// 怪物基準數值（plan 6 Global Constraints）；不含角色倍率，給內容校驗與文件使用
public static class MonsterFormula
{
    public static MonsterDefinition Baseline(int level)
    {
        int Round(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        return new MonsterDefinition(
            $"baseline-{level}",
            "基準",
            level,
            Hp: Round(45 + 14m * level + 0.4m * level * level),
            Sp: Round(10 + 2m * level),
            At: Round(10 + 3.2m * level),
            Mat: Round(6 + 2.6m * level),
            Df: Round(2 + 1.0m * level),
            Mdf: Round(2 + 0.9m * level),
            Agi: Round(6 + 1.3m * level),
            Au: Round(4 + 1.1m * level),
            Element: 0,
            JobExp: Round(8 + 1.6m * level),
            Meet: 0);
    }
}
