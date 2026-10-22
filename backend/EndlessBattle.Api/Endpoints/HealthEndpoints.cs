using EndlessBattle.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints;

public static class HealthEndpoints
{
    // 公開端點：給 docker compose 與反向代理確認服務與資料庫都可用
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async Task<Results<Ok<HealthResponse>, JsonHttpResult<HealthResponse>>> (IDatabaseStatus database, CancellationToken cancellationToken) =>
                await database.CanConnectAsync(cancellationToken)
                    ? TypedResults.Ok(new HealthResponse("ok"))
                    : TypedResults.Json(new HealthResponse("unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable))
            .WithTags("Health");

        return app;
    }
}
