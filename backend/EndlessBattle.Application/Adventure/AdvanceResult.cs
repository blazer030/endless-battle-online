using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Combat;

namespace EndlessBattle.Application.Adventure;

public sealed record AdvanceResult(
    IReadOnlyList<BattleEvent> Events,
    long ExperienceGained,
    long SpiritStonesGained,
    Character Character);
