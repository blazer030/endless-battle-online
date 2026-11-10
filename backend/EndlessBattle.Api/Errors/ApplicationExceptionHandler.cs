using EndlessBattle.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EndlessBattle.Api.Errors;

// 把 Application 層的例外轉成 ProblemDetails；其他例外交給預設處理（500）
public sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            InputValidationException => (StatusCodes.Status400BadRequest, "輸入不正確"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "驗證失敗"),
            NotFoundException => (StatusCodes.Status404NotFound, "找不到資料"),
            ConflictException => (StatusCodes.Status409Conflict, "資料衝突"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "請求格式錯誤"),
            _ => (0, ""),
        };
        if (status == 0)
        {
            return false;
        }

        var problemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message };
        var reasons = exception switch
        {
            InputValidationException validation => validation.Reasons,
            ConflictException conflict => conflict.Reasons,
            _ => [],
        };
        if (reasons.Count > 0)
        {
            problemDetails.Extensions["reasons"] = reasons;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
