using Inkbound.Domain.Characters;
using Inkbound.Domain.Combat;

namespace Inkbound.Application.Adventure;

public sealed record AdvanceResult(
    IReadOnlyList<BattleEvent> Events,
    long ExperienceGained,
    long SpiritStonesGained,
    Character Character);
