using Inkbound.Application.Content;

namespace Inkbound.Api.Endpoints;

public static class ContentEndpoints
{
    // 公開端點：只給前端顯示名稱用，不回傳任何數值
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/content", (IGameContent content) =>
            TypedResults.Ok(new ContentResponse(
                content.AllMonsters.Select(monster => new NamedContent(monster.Id, monster.Name)).ToList(),
                content.AllStages.Select(stage => new NamedContent(stage.Id, stage.Name)).ToList())))
            .WithTags("Content");

        return app;
    }
}
