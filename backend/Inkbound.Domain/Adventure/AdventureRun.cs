using Inkbound.Domain.Combat;

namespace Inkbound.Domain.Adventure;

// ConsumedSeconds 是實際用掉的時間；打不完而捨棄的那場不算，留給下一次推進
public sealed record AdventureRun(
    IReadOnlyList<BattleEvent> Events,
    double ConsumedSeconds,
    long ExperienceGained,
    long SpiritStonesGained);
