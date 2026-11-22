using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Api.Errors;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Api.Endpoints;

public static class SkillEndpoints
{
    public static IEndpointRouteBuilder MapSkillEndpoints(this IEndpointRouteBuilder app)
    {
        var skills = app.MapGroup("/api/skills").WithTags("Skills").RequireAuthorization();

        skills.MapGet("/", async (ClaimsPrincipal user, SkillService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken))));

        skills.MapPost("/learn", async (LearnSkillRequest request, ClaimsPrincipal user, SkillService service, CancellationToken cancellationToken) =>
        {
            var skillId = request.SkillId ?? throw RequestErrors.Invalid("缺少技能 id");
            return TypedResults.Ok(ToResponse(await service.LearnAsync(user.GetAccountId(), skillId, request.JobId, cancellationToken)));
        });

        skills.MapPut("/slots", async (SetSkillSlotsRequest request, ClaimsPrincipal user, SkillService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.SetSlotsAsync(user.GetAccountId(), ToSlots(request), cancellationToken))));

        return app;
    }

    // 只檢查欄位是否齊全；欄位規則由 Domain 的 SkillLoadout 驗證
    private static IReadOnlyList<SkillSlot> ToSlots(SetSkillSlotsRequest request) =>
        (request.Slots ?? throw RequestErrors.Invalid("缺少技能欄位"))
            .Select(slot => slot is { Slot: { } number, SkillId: { } skillId }
                ? new SkillSlot(number, skillId)
                : throw RequestErrors.Invalid("技能欄位資料不完整"))
            .ToList();

    private static SkillOverviewResponse ToResponse(SkillOverview overview) => new(
        overview.JobId,
        overview.SkillPoints,
        overview.Skills.Select(skill => new SkillResponse(
            skill.Id,
            skill.Name,
            skill.Type,
            skill.Timing,
            skill.Description,
            skill.Icon,
            skill.Level,
            skill.MaxLevel,
            skill.Summary,
            skill.TaughtByCurrentJob,
            skill.Prerequisite is { } prerequisite
                ? new SkillPrerequisiteCheckResponse(prerequisite.SkillId, prerequisite.Name, prerequisite.Level, prerequisite.Met)
                : null,
            skill.Next is { } next
                ? new SkillNextLevelResponse(next.Level, next.Money, next.Proficiency, next.Summary, next.CanLearn, next.EnoughMoney, next.EnoughProficiency)
                : null)).ToList(),
        overview.Slots.Select(slot => new SkillSlotResponse(slot.Slot, slot.SkillId)).ToList(),
        overview.Jobs.Select(job => new SkillJobResponse(
            job.JobId, job.JobName, job.Tier, job.Current, job.SkillPoints, job.Skills.Select(ToResponse).ToList())).ToList());

    private static LearnableSkillResponse ToResponse(JobSkillView skill) => new(
        skill.Id,
        skill.Name,
        skill.Type,
        skill.Timing,
        skill.Description,
        skill.Icon,
        skill.Level,
        skill.MaxLevel,
        skill.Rows is { } rows ? ToResponse(rows) : null,
        skill.Prerequisite is { } prerequisite
            ? new SkillPrerequisiteCheckResponse(prerequisite.SkillId, prerequisite.Name, prerequisite.Level, prerequisite.Met)
            : null,
        skill.Next is { } next
            ? new LearnableSkillNextLevelResponse(
                next.Level, next.Money, next.SkillPoints, ToResponse(next.Rows), next.CanLearn, next.EnoughMoney, next.EnoughSkillPoints)
            : null);

    private static List<SkillDetailRowResponse> ToResponse(IReadOnlyList<SkillDetailRow> rows) =>
        rows.Select(row => new SkillDetailRowResponse(row.Kind, row.Text)).ToList();
}
