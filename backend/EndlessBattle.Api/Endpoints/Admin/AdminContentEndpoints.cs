using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Admin.Content;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

public static class AdminContentEndpoints
{
    public static RouteGroupBuilder MapAdminContentEndpoints(this RouteGroupBuilder admin)
    {
        var content = admin.MapGroup("/content").WithTags("Admin Content");

        MapRecords<MonsterRecord>(content, "monsters");
        MapRecords<ItemRecord>(content, "items");
        MapRecords<JobRecord>(content, "jobs");
        MapRecords<SkillRecord>(content, "skills");
        MapRecords<AreaRecord>(content, "areas");
        MapRecords<PlaceRecord>(content, "places");
        content.MapGet("/missions", async Task<Ok<IReadOnlyList<MissionSummaryRecord>>> (
                ContentRecordService<MissionSummaryRecord> service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.ListAsync(cancellationToken)));

        // 任務不開放編輯；沒有這條，只有 GET 的路徑會回 405 而不是 404
        content.MapPost("/missions", () => TypedResults.NotFound()).ExcludeFromDescription();

        return admin;
    }

    private static void MapRecords<TRecord>(RouteGroupBuilder content, string kind)
        where TRecord : class, IContentRecord
    {
        content.MapGet($"/{kind}", async Task<Ok<IReadOnlyList<TRecord>>> (ContentRecordService<TRecord> service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.ListAsync(cancellationToken)));

        content.MapGet($"/{kind}/{{id}}", async Task<Results<Ok<TRecord>, NotFound>> (
                string id, ContentRecordService<TRecord> service, CancellationToken cancellationToken) =>
            await service.FindAsync(id, cancellationToken) is { } record ? TypedResults.Ok(record) : TypedResults.NotFound());

        content.MapPut($"/{kind}/{{id}}", async Task<Results<Ok<DraftStatus>, BadRequest<ContentFieldError>, NotFound>> (
                string id, HttpRequest request, ContentRecordService<TRecord> service, CancellationToken cancellationToken) =>
            {
                var body = await ContentRecordBody.ReadAsync<TRecord>(request, cancellationToken);
                if (body.Error is not null)
                {
                    return TypedResults.BadRequest(body.Error);
                }

                var result = await service.UpdateAsync(id, body.Record!, cancellationToken);
                return result switch
                {
                    { Outcome: ContentWriteOutcome.Saved, Draft: { } draft } => TypedResults.Ok(draft),
                    { Outcome: ContentWriteOutcome.Rejected, Error: { } error } => TypedResults.BadRequest(error),
                    _ => TypedResults.NotFound(),
                };
            })
            .Accepts<TRecord>("application/json");

        content.MapPost($"/{kind}", async Task<Results<Created<DraftStatus>, BadRequest<ContentFieldError>, Conflict<ContentFieldError>>> (
                HttpRequest request, ContentRecordService<TRecord> service, CancellationToken cancellationToken) =>
            {
                var body = await ContentRecordBody.ReadAsync<TRecord>(request, cancellationToken);
                if (body.Error is not null)
                {
                    return TypedResults.BadRequest(body.Error);
                }

                var result = await service.CreateAsync(body.Record!, cancellationToken);
                return result switch
                {
                    { Outcome: ContentWriteOutcome.Saved, Draft: { } draft } => TypedResults.Created($"/admin/content/{kind}/{body.Record!.RecordId}", draft),
                    { Outcome: ContentWriteOutcome.DuplicateId, Error: { } error } => TypedResults.Conflict(error),
                    { Error: { } error } => TypedResults.BadRequest(error),
                    _ => throw new InvalidOperationException($"未預期的寫入結果：{result.Outcome}"),
                };
            })
            .Accepts<TRecord>("application/json");
    }
}
