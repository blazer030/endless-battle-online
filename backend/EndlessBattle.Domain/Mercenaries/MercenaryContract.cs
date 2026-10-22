namespace EndlessBattle.Domain.Mercenaries;

// PlaceId 與 NextFightAt 同時有值＝出擊中
public sealed record MercenaryContract(
    string MercenaryId,
    string Name,
    int Remaining,
    int? PlaceId,
    DateTimeOffset? NextFightAt,
    DateTimeOffset HiredAt)
{
    public bool Fighting => PlaceId is not null && NextFightAt is not null;
}
