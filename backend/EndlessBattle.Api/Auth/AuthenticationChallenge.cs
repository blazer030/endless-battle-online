using EndlessBattle.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace EndlessBattle.Api.Auth;

// 沒帶權杖、權杖過期或不合法：401 也照錯誤格式只帶原因代碼（AUTHENTICATION_REQUIRED），
// 跟登入時帳號密碼不符的 AUTHENTICATION_FAILED 分開
public static class AuthenticationChallenge
{
    public static async Task WriteAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var response = context.Response;
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
        var problemDetails = new ProblemDetails { Status = StatusCodes.Status401Unauthorized };
        problemDetails.Extensions["reasons"] = new[] { ReasonCodes.AuthenticationRequired };
        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = problemDetails,
        });
    }
}
