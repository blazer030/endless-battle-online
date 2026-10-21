using Inkbound.Domain.Combat;

namespace Inkbound.Domain.Characters;

// 計畫 4 換成 WOG 戰鬥前，暫時把 WOG 能力值接到現有的即時戰鬥模型
public static class CombatBridge
{
    public static CombatStats ToCombatStats(DerivedStats derived) => new(
        MaxHealth: derived.HpMax,
        Attack: derived.At,
        Defense: derived.Df,
        CritChance: 0.10,
        CritMultiplier: 1.2,
        AttacksPerSecond: 1.0);
}
