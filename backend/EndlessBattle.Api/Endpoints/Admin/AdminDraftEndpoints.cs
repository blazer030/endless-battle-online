using EndlessBattle.Application.Admin;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EndlessBattle.Api.Endpoints.Admin;

public static class AdminDraftEndpoints
{
    public static RouteGroupBuilder MapAdminDraftEndpoints(this RouteGroupBuilder admin)
    {
        var draft = admin.MapGroup("/draft").WithTags("Admin Draft");

        draft.MapGet("", async Task<Ok<DraftStatus>> (ContentAdminService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.GetDraftAsync(cancellationToken)));

        return admin;
    }
}
