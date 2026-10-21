using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var characters = app.MapGroup("/api/characters").WithTags("Characters").RequireAuthorization();

        characters.MapPost("/", async (CreateCharacterRequest request, ClaimsPrincipal user, CharacterService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var character = await service.CreateAsync(user.GetAccountId(), ToCommand(request), cancellationToken);
            return TypedResults.Created("/api/characters/me", ToResponse(character, [], content));
        });

        characters.MapGet("/me", async (ClaimsPrincipal user, CharacterService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var profile = await service.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(ToResponse(profile.Character, profile.JobProficiency, content));
        });

        return app;
    }

    // 只檢查欄位是否齊全；數值範圍由 Domain 的 CharacterFactory 驗證
    private static CreateCharacterCommand ToCommand(CreateCharacterRequest request)
    {
        var allocation = request.Allocation;
        if (request.Name is null
            || request.Sex is not { } sex
            || request.Element is not { } element
            || request.Birth is not { } birth
            || request.JobId is null
            || allocation is not { Str: { } str, Smart: { } smart, Life: { } life, Agi: { } agi }
            || request.SpecialAttackName is null)
        {
            throw new InputValidationException("創角資料不完整");
        }

        return new CreateCharacterCommand(
            request.Name, sex, element, birth, request.JobId, new StatAllocation(str, smart, life, agi), request.SpecialAttackName);
    }

    public static CharacterResponse ToResponse(Character character, IReadOnlyList<JobProficiency> jobProficiency, IGameContent content)
    {
        var total = character.Base;
        var derived = DerivedStats.From(total);
        return new CharacterResponse(
            character.Name,
            character.Level,
            character.Exp,
            character.NextExp,
            character.Money,
            character.Hp,
            character.Sp,
            character.Sex,
            character.Element,
            character.Birth,
            character.JobId,
            content.GetJob(character.JobId).Name,
            character.SpecialAttackName,
            new StatsResponse(
                total.Str, total.Smart, total.Life, total.Vit, total.Agi, total.Au, total.Be,
                derived.At, derived.Mat, derived.Df, derived.Mdf, derived.HpMax, derived.SpMax),
            character.CurrentStageId,
            character.ActNum,
            StaminaRules.Max,
            jobProficiency.Select(row => new JobProficiencyResponse(row.JobId, row.Proficiency, row.SkillPoints)).ToList());
    }
}
