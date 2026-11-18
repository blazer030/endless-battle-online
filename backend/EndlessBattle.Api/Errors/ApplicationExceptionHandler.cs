using EndlessBattle.Application.Common;
using EndlessBattle.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EndlessBattle.Api.Errors;

// 把 Application 層的例外轉成 ProblemDetails；其他例外交給預設處理（500）
// 回應只帶狀態碼與原因代碼（reasons），文字由前端依代碼決定；例外訊息只留在伺服器紀錄
public sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, IReadOnlyList<string> Reasons)? error = exception switch
        {
            InputValidationException validation => (StatusCodes.Status400BadRequest, validation.Reasons),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, [ReasonCodes.AuthenticationFailed]),
            NotFoundException notFound => (StatusCodes.Status404NotFound, [notFound.Reason]),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Reasons),
            BadHttpRequestException badRequest => (badRequest.StatusCode, [ReasonCodes.RequestInvalid]),
            _ => null,
        };
        if (error is null)
        {
            return false;
        }

        var (status, reasons) = error.Value;

        var problemDetails = new ProblemDetails { Status = status };
        problemDetails.Extensions["reasons"] = reasons;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
