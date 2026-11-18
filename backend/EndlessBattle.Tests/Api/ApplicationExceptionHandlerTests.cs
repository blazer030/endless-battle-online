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
        var reasons = Assert.IsAssignableFrom<IEnumerable<string>>(problemDetails.Written?.ProblemDetails.Extensions["reasons"]);
        Assert.Equal(["REQUEST_INVALID"], reasons);
    }

    [Fact]
    public async Task InputValidation_WritesReasonCodesAsExtension()
    {
        var problemDetails = new RecordingProblemDetailsService();

        await new ApplicationExceptionHandler(problemDetails).TryHandleAsync(
            new DefaultHttpContext(),
            new InputValidationException("無法學習技能", ["WRONG_JOB", "NOT_ENOUGH_MONEY"]),
            TestContext.Current.CancellationToken);

        var reasons = Assert.IsAssignableFrom<IEnumerable<string>>(problemDetails.Written?.ProblemDetails.Extensions["reasons"]);
        Assert.Equal(["WRONG_JOB", "NOT_ENOUGH_MONEY"], reasons);
    }

    [Fact]
    public async Task NotFound_WritesItsReason()
    {
        var problemDetails = new RecordingProblemDetailsService();

        await new ApplicationExceptionHandler(problemDetails).TryHandleAsync(
            new DefaultHttpContext(), new NotFoundException("尚未建立角色", "CHARACTER_NOT_FOUND"), TestContext.Current.CancellationToken);

        var reasons = Assert.IsAssignableFrom<IEnumerable<string>>(problemDetails.Written?.ProblemDetails.Extensions["reasons"]);
        Assert.Equal(["CHARACTER_NOT_FOUND"], reasons);
    }

    // spec game-api-contract：Error responses carry only reason codes——沒有 detail、沒有給玩家看的中文 title
    public static TheoryData<Exception, int> ApplicationErrors => new()
    {
        { new InputValidationException("無法學習技能", ["NOT_ENOUGH_MONEY"]), StatusCodes.Status400BadRequest },
        { new AuthenticationFailedException(), StatusCodes.Status401Unauthorized },
        { new NotFoundException("尚未建立角色", "CHARACTER_NOT_FOUND"), StatusCodes.Status404NotFound },
        { new ConflictException("版本不符", ["CONCURRENT_UPDATE"]), StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(ApplicationErrors))]
    public async Task ApplicationError_WritesOnlyStatusAndReasons(Exception exception, int status)
    {
        var problemDetails = new RecordingProblemDetailsService();

        await new ApplicationExceptionHandler(problemDetails).TryHandleAsync(new DefaultHttpContext(), exception, TestContext.Current.CancellationToken);

        var written = problemDetails.Written!.ProblemDetails;
        Assert.Equal(status, written.Status);
        Assert.Null(written.Detail);
        Assert.Null(written.Title);
        Assert.NotEmpty(Assert.IsAssignableFrom<IEnumerable<string>>(written.Extensions["reasons"]));
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
