using EndlessBattle.Api.Errors;
using Microsoft.AspNetCore.Http;

namespace EndlessBattle.Tests.Api;

public class ApplicationExceptionHandlerTests
{
    // 框架的 BadHttpRequestException 自帶狀態碼（例如 413 本文過大），回應要沿用它而不是一律 400。
    // 測試主機觸發不了 400 以外的狀態（415 由框架直接回應、不丟例外），所以直接呼叫 handler
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status413PayloadTooLarge)]
    public async Task BadHttpRequest_KeepsTheExceptionStatusCode(int statusCode)
    {
        var problemDetails = new RecordingProblemDetailsService();
        var httpContext = new DefaultHttpContext();

        var handled = await new ApplicationExceptionHandler(problemDetails)
            .TryHandleAsync(httpContext, new BadHttpRequestException("bad", statusCode), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(statusCode, httpContext.Response.StatusCode);
        Assert.Equal(statusCode, problemDetails.Written?.ProblemDetails.Status);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }
}
