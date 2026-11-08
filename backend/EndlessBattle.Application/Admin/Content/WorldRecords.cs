using System.Text.Json.Serialization;

namespace EndlessBattle.Application.Admin.Content;

public sealed record AreaRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<int> PlaceIds { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}

public sealed record PlaceRecord : IContentRecord
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<string> SubPlaceNames { get; init; }

    public required ContentRange RecommendedLevel { get; init; }

    public required bool Restricted { get; init; }

    [JsonIgnore]
    public string RecordId => Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
