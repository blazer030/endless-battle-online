using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var characters = app.MapGroup("/api/characters").WithTags("Characters").RequireAuthorization();

        characters.MapPost("/", async (CreateCharacterRequest request, ClaimsPrincipal user, CharacterService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var character = await service.CreateAsync(user.GetAccountId(), ToCommand(request), cancellationToken);
            return TypedResults.Created("/api/characters/me", ToResponse(character, [], EquipmentBonus.Zero, content));
        });

        characters.MapGet("/me", async (ClaimsPrincipal user, CharacterService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            var profile = await service.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content));
        });

        characters.MapGet("/me/buffs", async (ClaimsPrincipal user, CharacterService service, CancellationToken cancellationToken) =>
        {
            var buffs = await service.ListBuffsAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(buffs.Select(buff => new BuffResponse(buff.Kind, buff.Multiplier, buff.EndsAt)).ToList());
        });

        characters.MapPut("/me/settings", async (AdventureSettingsRequest request, ClaimsPrincipal user, CharacterService service, IGameContent content, CancellationToken cancellationToken) =>
        {
            if (request is not
                {
                    PotionHealthPointsPercent: { } potionHealthPoints, PotionManaPointsPercent: { } potionManaPoints, RestHealthPointsPercent: { } restHealthPoints, RestManaPointsPercent: { } restManaPoints,
                    EscapeHealthPointsPercent: { } escapeHealthPoints,
                })
            {
                throw new InputValidationException("設定資料不完整");
            }

            var settings = new AdventureSettings(request.Attack, potionHealthPoints, potionManaPoints, restHealthPoints, restManaPoints, escapeHealthPoints);
            var profile = await service.UpdateSettingsAsync(user.GetAccountId(), settings, cancellationToken);
            return TypedResults.Ok(ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content));
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
            || allocation is not { Strength: { } strength, Intelligence: { } intelligence, Life: { } life, Agility: { } agility })
        {
            throw new InputValidationException("創角資料不完整");
        }

        return new CreateCharacterCommand(
            request.Name, sex, element, birth, request.JobId, new StatAllocation(strength, intelligence, life, agility));
    }

    // Stats 是有效值（裸點 + 裝備），BaseStats 是裸點
    public static CharacterResponse ToResponse(
        Character character, IReadOnlyList<JobProficiency> jobProficiency, EquipmentBonus equipment, IGameContent content)
    {
        var bare = character.Base;
        var total = equipment.Effective(bare);
        var derived = DerivedStats.From(bare, equipment);
        var vitals = CombatBridge.ToCombatant(character, equipment);
        var place = content.GetPlace(character.PlaceId);
        return new CharacterResponse(
            character.Name,
            character.Level,
            character.Experience,
            character.NextLevelExperience,
            character.Money,
            character.Bank,
            vitals.HealthPoints,
            vitals.ManaPoints,
            character.Sex,
            character.Element,
            character.Birth,
            character.JobId,
            content.GetJob(character.JobId).Name,
            new StatsResponse(
                total.Strength, total.Intelligence, total.Life, total.Vitality, total.Agility, total.Charisma, total.Faith,
                derived.Attack, derived.MagicAttack, derived.Defense, derived.MagicDefense, derived.MaxHealthPoints, derived.MaxManaPoints),
            new BaseStatsResponse(bare.Strength, bare.Intelligence, bare.Life, bare.Vitality, bare.Agility, bare.Charisma, bare.Faith),
            place.Id,
            place.Name,
            character.SubPlace,
            place.SubPlaces.FirstOrDefault(subPlace => subPlace.Index == character.SubPlace)?.Name,
            content.GetArea(place.AreaId).Name,
            jobProficiency.Select(row => new JobProficiencyResponse(row.JobId, row.Proficiency, row.SkillPoints)).ToList(),
            new AdventureSettingsResponse(
                character.Settings.Attack,
                character.Settings.PotionHealthPointsPercent,
                character.Settings.PotionManaPointsPercent,
                character.Settings.RestHealthPointsPercent,
                character.Settings.RestManaPointsPercent,
                character.Settings.EscapeHealthPointsPercent));
    }
}
