using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Application.Pets;

public sealed record PetView(Pet Pet, int Age, DateTimeOffset NextFeedAt);

public sealed record PetOverview(IReadOnlyList<PetView> Pets, int MaxPets, IReadOnlyList<PetFood> Foods);

// 每個寫入型用例一筆交易：讀取與驗證 → 角色版本鎖（餵食時一併扣錢）→ 寫寵物；
// 不存在或別的角色的寵物一律 petUnknown
public sealed class PetService(
    ICharacterRepository characters,
    IPetRepository pets,
    IEquipmentRepository equipment,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
    IUnitOfWork unitOfWork)
{
    public async Task<PetOverview> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return await OverviewAsync(character.Id, cancellationToken);
    }

    // 同時只攜帶一隻：原本攜帶的放回獸欄
    public Task<PetOverview> CarryAsync(long accountId, long petId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var owned = await pets.ListAsync(character.Id, cancellationToken);
            FindPet(owned, petId);
            await characters.UpdateAsync(character, cancellationToken);
            foreach (var pet in PetRules.Carry(owned, petId).Except(owned))
            {
                await pets.UpdateAsync(character.Id, pet, cancellationToken);
            }

            return await OverviewAsync(character.Id, cancellationToken);
        }, cancellationToken);

    public Task<PetOverview> StoreAsync(long accountId, long petId, CancellationToken cancellationToken) =>
        WriteAsync(accountId, petId, pet => pet with { Carried = false }, cancellationToken);

    public Task<PetOverview> RenameAsync(long accountId, long petId, string? name, CancellationToken cancellationToken) =>
        WriteAsync(accountId, petId, pet =>
        {
            Check(PetRules.CanRename(name), "名字必須是 1～12 個字");
            return pet with { Name = name!.Trim() };
        }, cancellationToken);

    public Task<PetOverview> ReleaseAsync(long accountId, long petId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            FindPet(await pets.ListAsync(character.Id, cancellationToken), petId);
            await characters.UpdateAsync(character, cancellationToken);
            await pets.RemoveAsync(character.Id, petId, cancellationToken);
            return await OverviewAsync(character.Id, cancellationToken);
        }, cancellationToken);

    public Task<PetOverview> FeedAsync(long accountId, long petId, string foodId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var pet = FindPet(await pets.ListAsync(character.Id, cancellationToken), petId);
            var food = PetCare.Foods.FirstOrDefault(food => food.Id == foodId)
                ?? throw new InputValidationException("沒有這種食物", ["foodUnknown"]);
            var owner = DerivedStats.From(character.Base, await equipment.LoadBonusAsync(character.Id, content, cancellationToken));
            var now = clock.UtcNow;
            Check(PetCare.CanFeed(pet, food, owner, character.Money, now), "無法餵食");

            var fed = PetCare.Feed(pet, food, now, randomFactory.Create());
            await characters.UpdateAsync(character with { Money = character.Money - food.Cost }, cancellationToken);
            await pets.UpdateAsync(character.Id, fed, cancellationToken);
            return await OverviewAsync(character.Id, cancellationToken);
        }, cancellationToken);

    private Task<PetOverview> WriteAsync(long accountId, long petId, Func<Pet, Pet> change, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var changed = change(FindPet(await pets.ListAsync(character.Id, cancellationToken), petId));
            await characters.UpdateAsync(character, cancellationToken);
            await pets.UpdateAsync(character.Id, changed, cancellationToken);
            return await OverviewAsync(character.Id, cancellationToken);
        }, cancellationToken);

    private async Task<PetOverview> OverviewAsync(long characterId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var views = (await pets.ListAsync(characterId, cancellationToken))
            .Select(pet => new PetView(pet, PetCare.Age(pet, now), PetCare.NextFeedAt(pet)))
            .ToList();
        return new PetOverview(views, PetRules.MaxPets, PetCare.Foods);
    }

    private static Pet FindPet(IReadOnlyList<Pet> owned, long petId) =>
        owned.FirstOrDefault(pet => pet.Id == petId) ?? throw new InputValidationException("沒有這隻寵物", ["petUnknown"]);

    private static void Check(IReadOnlyList<string> reasons, string message)
    {
        if (reasons.Count > 0)
        {
            throw new InputValidationException(message, reasons);
        }
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
