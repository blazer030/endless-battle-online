using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Mercenaries;

namespace EndlessBattle.Api.Endpoints;

public static class AdventureEndpoints
{
    private const int RecentMercenaryFights = 5;

    public static IEndpointRouteBuilder MapAdventureEndpoints(this IEndpointRouteBuilder app)
    {
        var adventure = app.MapGroup("/api/adventure").WithTags("Adventure").RequireAuthorization();

        adventure.MapPost("/advance", async (ClaimsPrincipal user, AdventureService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var result = await service.AdvanceAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new AdvanceResponse(
                result.Events.Select(BattleEventResponse.From).ToList(),
                AdvanceSummaryResponse.From(result.Summary),
                result.EventsCoverAll,
                CharacterEndpoints.ToResponse(result.Character, result.JobProficiency, result.Equipment, content),
                result.Mercenary is null ? null : ToResponse(result.Mercenary, content),
                result.AreaPassExpiresAt));
        });

        return app;
    }

    // 內容檔移除的怪物以 id 代替名字
    private static MercenaryReportResponse ToResponse(MercenaryReport report, IGameContent content) => new(
        report.Name,
        report.Fights.Count,
        report.Fights.Count(fight => fight.Result == MercenaryFightResult.Won),
        report.Exp,
        report.Money,
        report.LevelsGained,
        report.Remaining,
        report.Dismissed,
        report.Fights.TakeLast(RecentMercenaryFights).Select(fight => new MercenaryFightResponse(
            fight.At,
            fight.MonsterId,
            content.AllMonsters.FirstOrDefault(monster => monster.Id == fight.MonsterId)?.Name ?? fight.MonsterId,
            fight.Result == MercenaryFightResult.Won,
            fight.Exp,
            fight.Money)).ToList());
}
