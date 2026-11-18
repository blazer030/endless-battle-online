using System.Text.RegularExpressions;

namespace EndlessBattle.Application.Common;

// Api 層把這些例外對應成 HTTP 狀態碼：409、404、400、401，並把原因代碼放進 ProblemDetails 的 reasons
// 錯誤回應只帶原因代碼（文字由前端決定），所以每個例外都必須帶至少一個代碼；訊息只給伺服器紀錄與測試看
// innerException 是被轉換的原始錯誤（例如資料庫的唯一鍵或死鎖），留著給記錄與除錯
public sealed class ConflictException(string message, IReadOnlyList<string> reasons, Exception? innerException = null)
    : Exception(message, innerException)
{
    public IReadOnlyList<string> Reasons { get; } = ReasonCodes.Require(reasons);
}

public sealed class NotFoundException(string message, string reason) : Exception(message)
{
    public string Reason { get; } = ReasonCodes.Require([reason])[0];
}

public sealed class InputValidationException(string message, IReadOnlyList<string> reasons) : Exception(message)
{
    public IReadOnlyList<string> Reasons { get; } = ReasonCodes.Require(reasons);
}

public sealed class AuthenticationFailedException() : Exception("帳號或密碼錯誤")
{
    public const string Reason = "AUTHENTICATION_FAILED";
}

// 原因代碼：全大寫、字之間用底線；帶參數的代碼在冒號後接參數（參數照原本的寫法，例如 STAT_TOO_LOW:strength）
internal static partial class ReasonCodes
{
    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(:.+)?$")]
    private static partial Regex Spelling();

    public static IReadOnlyList<string> Require(IReadOnlyList<string> reasons) =>
        reasons.Count > 0 && reasons.All(reason => reason is not null && Spelling().IsMatch(reason))
            ? reasons
            : throw new ArgumentException("錯誤必須帶至少一個全大寫底線的原因代碼", nameof(reasons));
}
