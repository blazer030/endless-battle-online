using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// world.json 有兩種紀錄；寫回時只換自己的陣列，另一個陣列與 startingPlaceId、startingSubPlace 原樣保留
internal static class WorldFile
{
    public const string FileName = "world.json";

    public static IReadOnlyList<TEntry> ReadEntries<TEntry>(IReadOnlyDictionary<string, string> files, string arrayProperty)
    {
        var entries = ContentFileJson.ParseRoot(files[FileName], FileName)[arrayProperty] as System.Text.Json.Nodes.JsonArray
            ?? throw new GameContentException($"{FileName} 沒有 {arrayProperty} 陣列");
        return ContentFileJson.ReadEntries<TEntry>(entries, FileName);
    }

    public static IReadOnlyDictionary<string, string> WriteEntries<TEntry>(
        IReadOnlyDictionary<string, string> files, string arrayProperty, IEnumerable<TEntry> entries)
    {
        var root = ContentFileJson.ParseRoot(files[FileName], FileName);
        root[arrayProperty] = ContentFileJson.ToNode(entries);
        return new Dictionary<string, string> { [FileName] = ContentFileFormatter.Format(root) };
    }
}

public sealed class AreaCodec : IContentCodec<AreaRecord>
{
    public IReadOnlyList<string> FileNames { get; } = [WorldFile.FileName];

    public IReadOnlyList<AreaRecord> Read(IReadOnlyDictionary<string, string> files) =>
        WorldFile.ReadEntries<AreaEntry>(files, "areas")
            .Select(entry => new AreaRecord { Id = entry.Id, Name = entry.Name, PlaceIds = entry.PlaceIds })
            .ToList();

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<AreaRecord> records, IReadOnlyDictionary<string, string> files) =>
        WorldFile.WriteEntries(files, "areas", records.Select(record => new AreaEntry { Id = record.Id, Name = record.Name, PlaceIds = record.PlaceIds }));

    private sealed record AreaEntry
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<int> PlaceIds { get; init; }
    }
}

public sealed class PlaceCodec : IContentCodec<PlaceRecord>
{
    public IReadOnlyList<string> FileNames { get; } = [WorldFile.FileName];

    public IReadOnlyList<PlaceRecord> Read(IReadOnlyDictionary<string, string> files) =>
        WorldFile.ReadEntries<PlaceEntry>(files, "places")
            .Select(entry => new PlaceRecord
            {
                Id = entry.Id,
                Name = entry.Name,
                SubPlaceNames = entry.SubPlaces,
                RecommendedLevel = ContentFileJson.ToRange(entry.Recommended, $"地區 {entry.Id} 的推薦等級"),
                Restricted = entry.Restricted,
            })
            .ToList();

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<PlaceRecord> records, IReadOnlyDictionary<string, string> files) =>
        WorldFile.WriteEntries(files, "places", records.Select(record => new PlaceEntry
        {
            Id = record.Id,
            Name = record.Name,
            SubPlaces = record.SubPlaceNames,
            Recommended = ContentFileJson.FromRange(record.RecommendedLevel),
            Restricted = record.Restricted,
        }));

    private sealed record PlaceEntry
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<string> SubPlaces { get; init; }

        public required int[] Recommended { get; init; }

        public required bool Restricted { get; init; }
    }
}
