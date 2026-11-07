using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin;
using EndlessBattle.Application.Admin.Content;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

public static class AdminContentEndpoints
{
    public static RouteGroupBuilder MapAdminContentEndpoints(this RouteGroupBuilder admin)
    {
        var content = admin.MapGroup("/content").WithTags("Admin Content");

        // 字面路徑優先於下面的 /{kind}；還沒遷移的種類繼續走舊路由
        MapRecords<MonsterRecord>(content, "monsters");

        content.MapGet("/{kind}", async Task<Results<Ok<IReadOnlyList<JsonObject>>, NotFound>> (
                string kind, ContentAdminService service, CancellationToken cancellationToken) =>
            ContentKind.Find(kind) is { } found
                ? TypedResults.Ok(await service.ListAsync(found, cancellationToken))
                : TypedResults.NotFound());

        content.MapGet("/{kind}/{id}", async Task<Results<Ok<JsonObject>, NotFound>> (
                string kind, string id, ContentAdminService service, CancellationToken cancellationToken) =>
            ContentKind.Find(kind) is { } found && await service.FindAsync(found, id, cancellationToken) is { } record
                ? TypedResults.Ok(record)
                : TypedResults.NotFound());

        content.MapPut("/{kind}/{id}", async Task<Results<Ok<DraftStatus>, NotFound, BadRequest>> (
                string kind, string id, JsonObject record, ContentAdminService service, CancellationToken cancellationToken) =>
            ContentKind.Find(kind) is not { } found
                ? TypedResults.NotFound()
                : await service.UpdateAsync(found, id, record, cancellationToken) switch
                {
                    { Outcome: ContentWriteOutcome.Saved, Draft: { } draft } => TypedResults.Ok(draft),
                    { Outcome: ContentWriteOutcome.IdMismatch } => TypedResults.BadRequest(),
                    _ => TypedResults.NotFound(),
                });

        content.MapPost("/{kind}", async Task<Results<Created<DraftStatus>, NotFound, BadRequest, Conflict>> (
                string kind, JsonObject record, ContentAdminService service, CancellationToken cancellationToken) =>
            ContentKind.Find(kind) is not { } found
                ? TypedResults.NotFound()
                : await service.CreateAsync(found, record, cancellationToken) switch
                {
                    { Outcome: ContentWriteOutcome.Saved, Draft: { } draft } =>
                        TypedResults.Created($"/admin/content/{found.Name}/{ContentAdminService.IdOf(record)}", draft),
                    { Outcome: ContentWriteOutcome.DuplicateId } => TypedResults.Conflict(),
                    _ => TypedResults.BadRequest(),
                });

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
