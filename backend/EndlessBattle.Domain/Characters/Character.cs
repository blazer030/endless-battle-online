namespace EndlessBattle.Domain.Characters;

// Version 用於樂觀鎖：更新時必須和資料庫中的版本一致
public sealed record Character(
    long Id,
    long AccountId,
    string Name,
    int Level,
    long Experience,
    long NextLevelExperience,
    long Money,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    BaseStats Base,
    int HealthPoints,
    int ManaPoints,
    int PlaceId,
    int? SubPlace,
    DateTimeOffset LastSettledAt,
    long Bank,
    int BagBonus,
    int DepotBonus,
    int Version)
{
    public AdventureSettings Settings { get; init; } = AdventureSettings.Default;
}
