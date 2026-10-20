namespace Inkbound.Domain.Combat;

public sealed record CombatStats(
    int MaxHealth,
    int Attack,
    int Defense,
    double CritChance,
    double CritMultiplier,
    double AttacksPerSecond);
