using EndlessBattle.Api.Errors;
using EndlessBattle.Application.Common;
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

    [Fact]
    public async Task InputValidation_WritesReasonCodesAsExtension()
    {
        var problemDetails = new RecordingProblemDetailsService();

        await new ApplicationExceptionHandler(problemDetails).TryHandleAsync(
            new DefaultHttpContext(),
            new InputValidationException("無法學習技能", ["wrongJob", "notEnoughMoney"]),
            TestContext.Current.CancellationToken);

        var reasons = Assert.IsAssignableFrom<IEnumerable<string>>(problemDetails.Written?.ProblemDetails.Extensions["reasons"]);
        Assert.Equal(["wrongJob", "notEnoughMoney"], reasons);
    }

    [Fact]
    public async Task InputValidation_OmitsReasonsWhenEmpty()
    {
        var problemDetails = new RecordingProblemDetailsService();

        await new ApplicationExceptionHandler(problemDetails).TryHandleAsync(
            new DefaultHttpContext(), new InputValidationException("資料不完整"), TestContext.Current.CancellationToken);

        Assert.False(problemDetails.Written!.ProblemDetails.Extensions.ContainsKey("reasons"));
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
