namespace EndlessBattle.Application.Common;

// Api 層把這些例外對應成 HTTP 狀態碼：409、404、400、401
// innerException 是被轉換的原始錯誤（例如資料庫的唯一鍵或死鎖），留著給記錄與除錯
// Reasons 是給前端判斷用的原因代碼；Api 層非空時放進 ProblemDetails 的 reasons
public sealed class ConflictException(string message, Exception? innerException = null, IReadOnlyList<string>? reasons = null)
    : Exception(message, innerException)
{
    public IReadOnlyList<string> Reasons { get; } = reasons ?? [];
}

// Reason 是給前端判斷用的原因代碼；Api 層放進 ProblemDetails 的 reasons
public sealed class NotFoundException(string message, string? reason = null) : Exception(message)
{
    public string? Reason { get; } = reason;
}

// Reasons 是給前端判斷用的原因代碼；Api 層非空時放進 ProblemDetails 的 reasons
public sealed class InputValidationException(string message, IReadOnlyList<string>? reasons = null) : Exception(message)
{
    public IReadOnlyList<string> Reasons { get; } = reasons ?? [];
}

public sealed class AuthenticationFailedException() : Exception("帳號或密碼錯誤")
{
    public const string Reason = "AUTHENTICATION_FAILED";
}
