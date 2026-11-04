using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

public static class AdminContentEndpoints
{
    public static RouteGroupBuilder MapAdminContentEndpoints(this RouteGroupBuilder admin)
    {
        var content = admin.MapGroup("/content").WithTags("Admin Content");

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

        content.MapPut("/{kind}/{id}", async Task<Results<NoContent, NotFound, BadRequest>> (
                string kind, string id, JsonObject record, ContentAdminService service, CancellationToken cancellationToken) =>
            ContentKind.Find(kind) is not { } found
                ? TypedResults.NotFound()
                : await service.UpdateAsync(found, id, record, cancellationToken) switch
                {
                    ContentWriteOutcome.Saved => TypedResults.NoContent(),
                    ContentWriteOutcome.IdMismatch => TypedResults.BadRequest(),
                    _ => TypedResults.NotFound(),
                });

        return admin;
    }
}
