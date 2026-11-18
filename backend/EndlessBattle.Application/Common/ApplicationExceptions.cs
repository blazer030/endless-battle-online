using EndlessBattle.Domain;

namespace EndlessBattle.Application.Common;

// Api 層把這些例外對應成 HTTP 狀態碼：409、404、400、401，並把原因代碼放進 ProblemDetails 的 reasons
// 錯誤回應只帶原因代碼（文字由前端決定），所以每個例外都必須帶至少一個代碼；訊息只給伺服器紀錄與測試看
// innerException 是被轉換的原始錯誤（例如資料庫的唯一鍵或死鎖），留著給記錄與除錯
public sealed class ConflictException(string message, IReadOnlyList<string> reasons, Exception? innerException = null)
    : Exception(message, innerException)
{
    public IReadOnlyList<string> Reasons { get; } = ErrorCodeRequirement.Require(reasons);
}

public sealed class NotFoundException(string message, string reason) : Exception(message)
{
    public string Reason { get; } = ErrorCodeRequirement.Require([reason])[0];
}

public sealed class InputValidationException(string message, IReadOnlyList<string> reasons) : Exception(message)
{
    public IReadOnlyList<string> Reasons { get; } = ErrorCodeRequirement.Require(reasons);
}

public sealed class AuthenticationFailedException() : Exception("帳號或密碼錯誤");

// 代碼寫法的規則在 Domain 的 ErrorCodes.IsWellFormed
internal static class ErrorCodeRequirement
{
    public static IReadOnlyList<string> Require(IReadOnlyList<string> reasons) =>
        reasons.Count > 0 && reasons.All(reason => reason is not null && ErrorCodes.IsWellFormed(reason))
            ? reasons
            : throw new ArgumentException("錯誤必須帶至少一個全大寫底線的原因代碼", nameof(reasons));
}
