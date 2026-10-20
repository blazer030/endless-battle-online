namespace Inkbound.Domain.Characters;

// Version 用於樂觀鎖：更新時必須和資料庫中的版本一致
public sealed record Character(
    long Id,
    long AccountId,
    string Name,
    long Experience,
    long SpiritStones,
    string CurrentStageId,
    DateTimeOffset LastSettledAt,
    int Version);
