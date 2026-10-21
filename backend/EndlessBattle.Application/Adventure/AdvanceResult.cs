using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Application.Adventure;

public sealed record AdvanceResult(
    IReadOnlyList<BattleEvent> Events,
    long ExperienceGained,
    long SpiritStonesGained,
    Character Character);
