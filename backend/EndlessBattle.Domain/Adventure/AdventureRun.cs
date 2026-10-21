using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Adventure;

// ConsumedSeconds 是實際用掉的時間；最後一場會打完，所以可能超過可用時間（最多一場戰鬥加休息）
public sealed record AdventureRun(
    IReadOnlyList<BattleEvent> Events,
    double ConsumedSeconds,
    long ExperienceGained,
    long SpiritStonesGained);
