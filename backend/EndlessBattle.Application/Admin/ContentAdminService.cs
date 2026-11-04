using System.Text.Json.Nodes;

namespace EndlessBattle.Application.Admin;

public sealed class ContentAdminService(IContentDraftStore store)
{
    public Task<IReadOnlyList<JsonObject>> ListAsync(ContentKind kind, CancellationToken cancellationToken) =>
        store.ReadRecordsAsync(kind, cancellationToken);

    public async Task<JsonObject?> FindAsync(ContentKind kind, string id, CancellationToken cancellationToken) =>
        (await store.ReadRecordsAsync(kind, cancellationToken)).FirstOrDefault(record => IdOf(record) == id);

    // id 可能是字串（道具、怪物）或數字（地區）；一律以文字比對
    public static string? IdOf(JsonObject record) => record["id"] switch
    {
        JsonValue value when value.TryGetValue(out string? text) => text,
        JsonValue value => value.ToJsonString(),
        _ => null,
    };
}
