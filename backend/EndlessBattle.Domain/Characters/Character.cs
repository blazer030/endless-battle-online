namespace EndlessBattle.Domain.Characters;

// Version 用於樂觀鎖：更新時必須和資料庫中的版本一致
public sealed record Character(
    long Id,
    long AccountId,
    string Name,
    int Level,
    long Exp,
    long NextExp,
    long Money,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    string SpecialAttackName,
    BaseStats Base,
    int Hp,
    int Sp,
    string CurrentStageId,
    DateTimeOffset LastSettledAt,
    int Version);
