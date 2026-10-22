using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Api.Endpoints;

public static class MissionEndpoints
{
    // 全域怪物（placeId 0）沒有對應的地區
    private const string AnyPlaceName = "各地";

    public static IEndpointRouteBuilder MapMissionEndpoints(this IEndpointRouteBuilder app)
    {
        var missions = app.MapGroup("/api/missions").WithTags("Missions").RequireAuthorization();

        missions.MapGet("/", async (ClaimsPrincipal user, MissionService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetBoardAsync(user.GetAccountId(), cancellationToken), content)));

        missions.MapPost("/accept", async (
            MissionRequest request, ClaimsPrincipal user, MissionService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.AcceptAsync(user.GetAccountId(), RequireMissionId(request), cancellationToken), content)));

        missions.MapPost("/abandon", async (
            MissionRequest request, ClaimsPrincipal user, MissionService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.AbandonAsync(user.GetAccountId(), RequireMissionId(request), cancellationToken), content)));

        return app;
    }

    private static string RequireMissionId(MissionRequest request) =>
        string.IsNullOrWhiteSpace(request.MissionId) ? throw new InputValidationException("缺少任務 id") : request.MissionId;

    public static MissionBoardResponse ToResponse(MissionBoardView board, IGameContent content)
    {
        List<MissionResponse> Map(IReadOnlyList<MissionEntry> entries) => entries.Select(entry => ToResponse(entry, board.Owned, content)).ToList();
        return new MissionBoardResponse(Map(board.Available), Map(board.Active), Map(board.Completed), MissionRules.MaxActive, board.NextDailyReset);
    }

    private static MissionResponse ToResponse(MissionEntry entry, IReadOnlyDictionary<string, int> owned, IGameContent content)
    {
        var mission = entry.Definition;
        var objective = entry.Objective;
        var requirements = mission.Requirements;
        var reward = objective.Reward;
        return new MissionResponse(
            mission.Id,
            mission.Giver,
            mission.Title,
            mission.Description,
            mission.Level,
            mission.PlaceId,
            mission.PlaceId is { } placeId ? content.GetPlace(placeId).Name : null,
            mission.Daily,
            objective.Kill is { } kill ? ToResponse(kill, entry.Record?.Progress ?? 0, content) : null,
            objective.Deliver
                .Select(item => new MissionDeliverResponse(item.ItemId, content.GetItem(item.ItemId).Name, item.Quantity, owned.GetValueOrDefault(item.ItemId)))
                .ToList(),
            new MissionRequirementsResponse(
                requirements.JobId,
                requirements.Sex,
                requirements.Birth,
                requirements.PrerequisiteId,
                requirements.PrerequisiteId is { } prerequisiteId ? content.GetMission(prerequisiteId).Title : null),
            new MissionRewardResponse(
                objective.Exp,
                reward.Money,
                reward.Items.Select(item => new MissionRewardItemResponse(item.ItemId, content.GetItem(item.ItemId).Name, item.Quantity)).ToList(),
                reward.Skill is { } skill ? new MissionRewardSkillResponse(skill.SkillId, content.GetSkill(skill.SkillId).Name, skill.Level) : null),
            entry.Ready,
            entry.ExpiresAt);
    }

    private static MissionKillResponse ToResponse(KillTarget kill, int progress, IGameContent content)
    {
        var monster = content.GetMonster(kill.MonsterId);
        var placeName = monster.PlaceId == MonsterPool.GlobalPlaceId ? AnyPlaceName : content.GetPlace(monster.PlaceId).Name;
        return new MissionKillResponse(monster.Id, monster.Name, monster.PlaceId, placeName, kill.Count, progress);
    }
}
