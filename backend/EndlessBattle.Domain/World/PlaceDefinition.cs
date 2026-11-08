namespace EndlessBattle.Domain.World;

public sealed record PlaceDefinition(
    int Id,
    string Name,
    string AreaId,
    IReadOnlyList<SubPlaceDefinition> SubPlaces,
    LevelRange Recommended,
    bool Restricted)
{
    public bool HasSubPlaces => SubPlaces.Count > 0;
}
