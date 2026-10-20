using Inkbound.Domain.Combat;

namespace Inkbound.Domain.Characters;

public static class PlayerStats
{
    // 計畫 3 加入境界後，屬性改由境界計算
    public static CombatStats Starting { get; } = new(
        MaxHealth: 120,
        Attack: 12,
        Defense: 4,
        CritChance: 0.10,
        CritMultiplier: 1.5,
        AttacksPerSecond: 1.0);
}
