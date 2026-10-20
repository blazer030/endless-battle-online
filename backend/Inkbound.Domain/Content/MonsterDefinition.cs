using Inkbound.Domain.Combat;

namespace Inkbound.Domain.Content;

public sealed record MonsterDefinition(
    string Id,
    string Name,
    CombatStats Stats,
    long ExperienceReward,
    long SpiritStoneReward);
