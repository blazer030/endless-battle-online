using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Pets;

namespace EndlessBattle.Api.Endpoints;

public static class PetEndpoints
{
    public static IEndpointRouteBuilder MapPetEndpoints(this IEndpointRouteBuilder app)
    {
        var pets = app.MapGroup("/api/pets").WithTags("Pets").RequireAuthorization();

        pets.MapGet("/", async (ClaimsPrincipal user, PetService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.GetAsync(user.GetAccountId(), cancellationToken), content)));

        pets.MapPost("/carry", async (PetRequest request, ClaimsPrincipal user, PetService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.CarryAsync(user.GetAccountId(), PetIdOf(request), cancellationToken), content)));

        pets.MapPost("/store", async (PetRequest request, ClaimsPrincipal user, PetService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.StoreAsync(user.GetAccountId(), PetIdOf(request), cancellationToken), content)));

        pets.MapPost("/release", async (PetRequest request, ClaimsPrincipal user, PetService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(await service.ReleaseAsync(user.GetAccountId(), PetIdOf(request), cancellationToken), content)));

        pets.MapPost("/rename", async (PetRequest request, ClaimsPrincipal user, PetService service, IGameContent content, CancellationToken cancellationToken) =>
            TypedResults.Ok(ToResponse(
                await service.RenameAsync(user.GetAccountId(), PetIdOf(request), request.Name, cancellationToken), content)));

        pets.MapPost("/feed", async (
            PetRequest request, ClaimsPrincipal user, PetService service, CharacterService characters, IGameContent content, CancellationToken cancellationToken) =>
        {
            var foodId = request.FoodId ?? throw new InputValidationException("缺少食物 id");
            var overview = await service.FeedAsync(user.GetAccountId(), PetIdOf(request), foodId, cancellationToken);
            var profile = await characters.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new PetFeedResponse(
                ToResponse(overview, content),
                CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content)));
        });

        return app;
    }

    private static long PetIdOf(PetRequest request) => request.PetId ?? throw new InputValidationException("缺少寵物 id");

    // 內容檔移除的怪物以 id 代替物種名
    private static PetsResponse ToResponse(PetOverview overview, IGameContent content) => new(
        overview.Pets.Select(view => new PetResponse(
            view.Pet.Id,
            view.Pet.MonsterId,
            content.AllMonsters.FirstOrDefault(monster => monster.Id == view.Pet.MonsterId)?.Name ?? view.Pet.MonsterId,
            view.Pet.Name,
            view.Pet.Personality,
            view.Pet.Attack,
            view.Pet.Magic,
            view.Pet.Defense,
            view.Pet.Spirit,
            view.Pet.Satiety,
            view.Pet.Intimacy,
            view.Pet.Carried,
            view.Age,
            view.NextFeedAt)).ToList(),
        overview.MaxPets,
        overview.Foods.Select(food => new PetFoodResponse(food.Id, food.Name, food.Stat, food.Amount, food.Cost)).ToList());
}
