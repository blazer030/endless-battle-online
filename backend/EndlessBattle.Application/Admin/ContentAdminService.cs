using System.Text.Json.Nodes;

namespace EndlessBattle.Application.Admin;

public enum ContentWriteOutcome
{
    Saved,
    NotFound,
    IdMismatch,
    MissingId,
    DuplicateId,
}

// Draft 只在 Saved 時有值：寫入後的草稿檢查結果
public sealed record ContentWriteResult(ContentWriteOutcome Outcome, DraftStatus? Draft = null);

public sealed class ContentAdminService(IContentDraftStore store)
{
    public Task<IReadOnlyList<JsonObject>> ListAsync(ContentKind kind, CancellationToken cancellationToken) =>
        store.ReadRecordsAsync(kind, cancellationToken);

    public async Task<JsonObject?> FindAsync(ContentKind kind, string id, CancellationToken cancellationToken) =>
        (await store.ReadRecordsAsync(kind, cancellationToken)).FirstOrDefault(record => IdOf(record) == id);

    public Task<DraftStatus> GetDraftAsync(CancellationToken cancellationToken) => store.CheckAsync(cancellationToken);

    // 只有通過檢查、且正式內容在建立草稿後沒被改過，才寫回正式內容
    public async Task<PublishResult> PublishAsync(CancellationToken cancellationToken)
    {
        var draft = await store.CheckAsync(cancellationToken);
        if (!draft.Exists)
        {
            return new(PublishOutcome.NoDraft);
        }

        if (!draft.Valid)
        {
            return new(PublishOutcome.InvalidDraft, Error: draft.Error);
        }

        var changed = await store.ChangedOfficialFilesAsync(cancellationToken);
        if (changed.Count > 0)
        {
            return new(PublishOutcome.OfficialChanged, ChangedFiles: changed);
        }

        await store.PublishAsync(cancellationToken);
        return new(PublishOutcome.Published);
    }

    public Task<bool> DiscardAsync(CancellationToken cancellationToken) => store.DiscardAsync(cancellationToken);

    // 以本體整筆取代；本體的 id 必須與路徑相同
    public async Task<ContentWriteResult> UpdateAsync(ContentKind kind, string id, JsonObject record, CancellationToken cancellationToken)
    {
        if (IdOf(record) != id)
        {
            return new(ContentWriteOutcome.IdMismatch);
        }

        var records = (await store.ReadRecordsAsync(kind, cancellationToken)).ToList();
        var index = records.FindIndex(existing => IdOf(existing) == id);
        if (index < 0)
        {
            return new(ContentWriteOutcome.NotFound);
        }

        records[index] = record;
        await store.WriteRecordsAsync(kind, records, cancellationToken);
        return new(ContentWriteOutcome.Saved, await store.CheckAsync(cancellationToken));
    }

    // 新紀錄加在陣列最後；id 不得與既有紀錄重複
    public async Task<ContentWriteResult> CreateAsync(ContentKind kind, JsonObject record, CancellationToken cancellationToken)
    {
        var id = IdOf(record);
        if (id is null)
        {
            return new(ContentWriteOutcome.MissingId);
        }

        var records = (await store.ReadRecordsAsync(kind, cancellationToken)).ToList();
        if (records.Any(existing => IdOf(existing) == id))
        {
            return new(ContentWriteOutcome.DuplicateId);
        }

        records.Add(record);
        await store.WriteRecordsAsync(kind, records, cancellationToken);
        return new(ContentWriteOutcome.Saved, await store.CheckAsync(cancellationToken));
    }

    // id 可能是字串（道具、怪物）或數字（地區）；一律以文字比對
    public static string? IdOf(JsonObject record) => record["id"] switch
    {
        JsonValue value when value.TryGetValue(out string? text) => text,
        JsonValue value => value.ToJsonString(),
        _ => null,
    };
}
