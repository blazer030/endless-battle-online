namespace EndlessBattle.Application.Admin;

public sealed class ContentAdminService(IContentDraftStore store)
{
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
}
