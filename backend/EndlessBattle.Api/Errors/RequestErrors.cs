using EndlessBattle.Application.Common;

namespace EndlessBattle.Api.Errors;

// 請求不完整或讀不懂（缺欄位、不是正確的 JSON）一律 REQUEST_INVALID；缺哪個欄位只留在例外訊息
public static class RequestErrors
{
    public const string RequestInvalid = "REQUEST_INVALID";

    public static InputValidationException Invalid(string message) => new(message, [RequestInvalid]);
}
