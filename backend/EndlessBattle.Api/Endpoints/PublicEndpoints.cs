using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Api.Endpoints;

// 不用登入就能讀的遊戲資料；帶過期或無效的 token 也照樣回應
public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public").AllowAnonymous().WithTags("Public");

        group.MapGet("/settings", () => TypedResults.Ok(new PublicSettingsResponse(
            CharacterOptions.Sexes.Select(ToResponse).ToList(),
            CharacterOptions.Elements.Select(element => new ElementOptionResponse(
                element.Id,
                element.Name,
                ElementTable.StrongAgainst(element.Id),
                ElementTable.WeakAgainst(element.Id))).ToList(),
            CharacterOptions.Births.Select(ToResponse).ToList())));

        return app;
    }

    private static OptionResponse ToResponse(CharacterOption option) => new(option.Id, option.Name);
}
