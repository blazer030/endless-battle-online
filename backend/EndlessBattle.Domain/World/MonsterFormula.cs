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
            HealthPoints: Round(45 + 14m * level + 0.4m * level * level),
            ManaPoints: Round(10 + 2m * level),
            Attack: Round(13 + 3.2m * level),
            MagicAttack: Round(4 + 2.0m * level),
            Defense: Round(5 + 1.0m * level),
            MagicDefense: Round(2 + 0.9m * level),
            Agility: Round(6 + 1.3m * level),
            Charisma: Round(4 + 1.1m * level),
            Element: 0,
            JobExperience: Round(8 + 1.6m * level),
            Meet: 0);
    }
}
