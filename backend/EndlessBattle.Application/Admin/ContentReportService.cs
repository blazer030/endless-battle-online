using EndlessBattle.Application.Balance;

namespace EndlessBattle.Application.Admin;

public enum ReportSource
{
    Official,
    Draft,
}

public enum ReportOutcome
{
    Built,
    NoDraft,
    InvalidDraft,
}

public sealed record ReportResult(ReportOutcome Outcome, BalanceReport? Report = null, string? Error = null);

// 對正式內容或草稿產生平衡報告；正式內容讀檔案（不是執行中的遊戲已載入的那份），發布後重啟前也能看到新內容
public sealed class ContentReportService(IContentDraftStore store, BalanceReportService reports)
{
    public async Task<ReportResult> BuildAsync(ReportSource source, BalanceReportOptions options, CancellationToken cancellationToken)
    {
        if (source == ReportSource.Official)
        {
            return new(ReportOutcome.Built, reports.Build(await store.LoadOfficialAsync(cancellationToken), options));
        }

        var draft = await store.CheckAsync(cancellationToken);
        if (!draft.Exists)
        {
            return new(ReportOutcome.NoDraft);
        }

        if (!draft.Valid)
        {
            return new(ReportOutcome.InvalidDraft, Error: draft.Error);
        }

        return new(ReportOutcome.Built, reports.Build(await store.LoadDraftAsync(cancellationToken), options));
    }
}
