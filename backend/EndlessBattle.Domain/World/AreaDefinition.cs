namespace EndlessBattle.Domain.World;

public sealed record AreaDefinition(string Id, string Name, IReadOnlyList<int> PlaceIds);
