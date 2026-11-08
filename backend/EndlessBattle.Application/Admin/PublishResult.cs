namespace EndlessBattle.Application.Admin;

public enum PublishOutcome
{
    Published,
    NoDraft,
    InvalidDraft,
    OfficialChanged,
}

// Error：草稿檢查不通過時的錯誤；ChangedFiles：建立草稿後被改過的正式內容檔
public sealed record PublishResult(PublishOutcome Outcome, string? Error = null, IReadOnlyList<string>? ChangedFiles = null);
