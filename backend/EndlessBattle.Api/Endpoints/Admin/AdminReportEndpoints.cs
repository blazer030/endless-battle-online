using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Balance;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

// SkillLevel 省略時為 Max
public sealed record ReportRequest(ReportSource Source, BalanceSkillLevel? SkillLevel);

public sealed record ReportRefusal(string Error);

public static class AdminReportEndpoints
{
    public static RouteGroupBuilder MapAdminReportEndpoints(this RouteGroupBuilder admin)
    {
        admin.MapPost("/reports", async Task<Results<Ok<BalanceReport>, NotFound, Conflict<ReportRefusal>>> (
                ReportRequest request, ContentReportService service, CancellationToken cancellationToken) =>
            await service.BuildAsync(request.Source, new BalanceReportOptions(request.SkillLevel ?? BalanceSkillLevel.Max), cancellationToken) switch
            {
                { Outcome: ReportOutcome.Built, Report: { } report } => TypedResults.Ok(report),
                { Outcome: ReportOutcome.InvalidDraft } result => TypedResults.Conflict(new ReportRefusal(result.Error ?? "")),
                _ => TypedResults.NotFound(),
            })
            .WithTags("Admin Reports");

        return admin;
    }
}
