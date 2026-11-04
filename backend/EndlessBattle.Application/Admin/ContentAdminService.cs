using System.Text.Json.Nodes;

namespace EndlessBattle.Application.Admin;

public enum ContentWriteOutcome
{
    Saved,
    NotFound,
    IdMismatch,
}

public sealed class ContentAdminService(IContentDraftStore store)
{
    public Task<IReadOnlyList<JsonObject>> ListAsync(ContentKind kind, CancellationToken cancellationToken) =>
        store.ReadRecordsAsync(kind, cancellationToken);

    public async Task<JsonObject?> FindAsync(ContentKind kind, string id, CancellationToken cancellationToken) =>
        (await store.ReadRecordsAsync(kind, cancellationToken)).FirstOrDefault(record => IdOf(record) == id);

    // 以本體整筆取代；本體的 id 必須與路徑相同
    public async Task<ContentWriteOutcome> UpdateAsync(ContentKind kind, string id, JsonObject record, CancellationToken cancellationToken)
    {
        if (IdOf(record) != id)
        {
            return ContentWriteOutcome.IdMismatch;
        }

        var records = (await store.ReadRecordsAsync(kind, cancellationToken)).ToList();
        var index = records.FindIndex(existing => IdOf(existing) == id);
        if (index < 0)
        {
            return ContentWriteOutcome.NotFound;
        }

        records[index] = record;
        await store.WriteRecordsAsync(kind, records, cancellationToken);
        return ContentWriteOutcome.Saved;
    }

    // id 可能是字串（道具、怪物）或數字（地區）；一律以文字比對
    public static string? IdOf(JsonObject record) => record["id"] switch
    {
        JsonValue value when value.TryGetValue(out string? text) => text,
        JsonValue value => value.ToJsonString(),
        _ => null,
    };
}
