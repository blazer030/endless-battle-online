namespace EndlessBattle.Application.Admin.Content;

public enum ContentWriteOutcome
{
    Saved,
    NotFound,
    DuplicateId,
    Rejected,
}

// Draft 只在 Saved 時有值：寫入後的草稿檢查結果；Error 指出被拒絕的欄位
public sealed record ContentWriteResult(ContentWriteOutcome Outcome, DraftStatus? Draft = null, ContentFieldError? Error = null);

// 內容紀錄的讀寫；有草稿時讀草稿，否則讀正式內容。寫入只寫草稿，沒有草稿時先建立
public interface IContentRecordStore<TRecord>
{
    Task<IReadOnlyList<TRecord>> ReadAsync(CancellationToken cancellationToken);

    // 一種內容可能對應多個檔案；要嘛全部寫入，要嘛全部不變
    Task WriteAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken);
}

// 寫入前的引用檢查：同一筆表單裡選的引用，沒有「之後才補上另一筆」的情況，所以不存在就直接拒絕；其他規則由載入程式回報
public interface IContentRecordCheck<in TRecord>
{
    Task<ContentFieldError?> FindErrorAsync(TRecord record, CancellationToken cancellationToken);
}

public sealed class ContentRecordService<TRecord>(
    IContentRecordStore<TRecord> store,
    IContentDraftStore drafts,
    IEnumerable<IContentRecordCheck<TRecord>> checks)
    where TRecord : class, IContentRecord
{
    public Task<IReadOnlyList<TRecord>> ListAsync(CancellationToken cancellationToken) => store.ReadAsync(cancellationToken);

    public async Task<TRecord?> FindAsync(string id, CancellationToken cancellationToken) =>
        (await store.ReadAsync(cancellationToken)).FirstOrDefault(record => record.RecordId == id);

    public async Task<ContentWriteResult> UpdateAsync(string id, TRecord record, CancellationToken cancellationToken)
    {
        if (record.RecordId != id)
        {
            return Rejected(new ContentFieldError("id", $"本體的 id {record.RecordId} 與路徑的 {id} 不同"));
        }

        var records = (await store.ReadAsync(cancellationToken)).ToList();
        var index = records.FindIndex(existing => existing.RecordId == id);
        if (index < 0)
        {
            return new(ContentWriteOutcome.NotFound);
        }

        if (await FindErrorAsync(record, cancellationToken) is { } error)
        {
            return Rejected(error);
        }

        records[index] = record;
        return await SaveAsync(records, cancellationToken);
    }

    // 新紀錄加在最後；id 不得空白或與既有紀錄重複
    public async Task<ContentWriteResult> CreateAsync(TRecord record, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.RecordId))
        {
            return Rejected(new ContentFieldError("id", "id 不可空白"));
        }

        var records = (await store.ReadAsync(cancellationToken)).ToList();
        if (records.Any(existing => existing.RecordId == record.RecordId))
        {
            return new(ContentWriteOutcome.DuplicateId, Error: new ContentFieldError("id", $"id {record.RecordId} 已存在"));
        }

        if (await FindErrorAsync(record, cancellationToken) is { } error)
        {
            return Rejected(error);
        }

        records.Add(record);
        return await SaveAsync(records, cancellationToken);
    }

    private static ContentWriteResult Rejected(ContentFieldError error) => new(ContentWriteOutcome.Rejected, Error: error);

    private async Task<ContentFieldError?> FindErrorAsync(TRecord record, CancellationToken cancellationToken)
    {
        foreach (var check in checks)
        {
            if (await check.FindErrorAsync(record, cancellationToken) is { } error)
            {
                return error;
            }
        }

        return null;
    }

    private async Task<ContentWriteResult> SaveAsync(IReadOnlyList<TRecord> records, CancellationToken cancellationToken)
    {
        await store.WriteAsync(records, cancellationToken);
        return new(ContentWriteOutcome.Saved, await drafts.CheckAsync(cancellationToken));
    }
}
