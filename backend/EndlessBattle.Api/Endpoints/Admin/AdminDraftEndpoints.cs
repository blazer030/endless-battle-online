using EndlessBattle.Application.Admin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

// 發布成功後要重啟 API，遊戲才會載入新內容
public sealed record PublishResponse(bool RestartRequired);

// Reason：invalidDraft（草稿檢查不通過）或 officialChanged（建立草稿後正式內容被改過）
public sealed record PublishRefusal(string Reason, string? Error, IReadOnlyList<string> ChangedFiles);

public static class AdminDraftEndpoints
{
    public static RouteGroupBuilder MapAdminDraftEndpoints(this RouteGroupBuilder admin)
    {
        var draft = admin.MapGroup("/draft").WithTags("Admin Draft");

        draft.MapGet("", async Task<Ok<DraftStatus>> (ContentAdminService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.GetDraftAsync(cancellationToken)));

        draft.MapDelete("", async Task<Results<NoContent, NotFound>> (ContentAdminService service, CancellationToken cancellationToken) =>
            await service.DiscardAsync(cancellationToken) ? TypedResults.NoContent() : TypedResults.NotFound());

        draft.MapPost("/publish", async Task<Results<Ok<PublishResponse>, NotFound, Conflict<PublishRefusal>>> (
                ContentAdminService service, CancellationToken cancellationToken) =>
            await service.PublishAsync(cancellationToken) switch
            {
                { Outcome: PublishOutcome.Published } => TypedResults.Ok(new PublishResponse(RestartRequired: true)),
                { Outcome: PublishOutcome.InvalidDraft } result =>
                    TypedResults.Conflict(new PublishRefusal("invalidDraft", result.Error, [])),
                { Outcome: PublishOutcome.OfficialChanged } result =>
                    TypedResults.Conflict(new PublishRefusal("officialChanged", null, result.ChangedFiles ?? [])),
                _ => TypedResults.NotFound(),
            });

        return admin;
    }
}
