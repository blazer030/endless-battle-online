using EndlessBattle.Application.Common;
using EndlessBattle.Application.Pets;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// 預設角色戰士、2000 錢，有效能力 At 12、Mat 12、Df 10、Mdf 10；寵物預設 Attack／Magic／Defense 10。
// 擲骰一律 0.5：飽腹 rand(10,20) → 15、親密度 rand(2,6) → 4
public class PetServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryPetRepository pets;
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly PetService service;

    public PetServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        pets = new InMemoryPetRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        unitOfWork = new InMemoryUnitOfWork(characters, pets, inventory);
        service = new PetService(characters, pets, equipment, Content, new FakeClock(Now), new HalfRandomFactory(), unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> AddCharacterAsync(long accountId = AccountId) =>
        await characters.AddAsync(TestCharacters.Default(accountId, $"測試者{accountId}", 1, 1, Now), CancellationToken);

    // 準備寵物並清掉準備過程的紀錄；HungerSince 1 分鐘前，可以餵
    private async Task<long> AddPetAsync(Character character, Pet pet)
    {
        var id = await pets.AddAsync(character.Id, pet with { HungerSince = Now.AddMinutes(-1) }, CancellationToken);
        log.Clear();
        return id;
    }

    private async Task<Pet> FindPetAsync(Character character, long petId) =>
        (await pets.ListAsync(character.Id, CancellationToken)).Single(pet => pet.Id == petId);

    private Task<Character?> StoredAsync() => characters.FindByAccountAsync(AccountId, CancellationToken);

    [Fact]
    public async Task Get_ListsAgeNextFeedAtAndMenu()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create() with { BornAt = Now.AddDays(-15) });

        var overview = await service.GetAsync(AccountId, CancellationToken);

        var view = Assert.Single(overview.Pets);
        Assert.Equal(id, view.Pet.Id);
        Assert.Equal(2, view.Age);
        Assert.Equal(Now.AddSeconds(-50), view.NextFeedAt);
        Assert.Equal(PetRules.MaxPets, overview.MaxPets);
        Assert.Equal(PetCare.Foods, overview.Foods);
    }

    [Fact]
    public async Task Feed_ChargesMoneyAndWritesCharacterFirst()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create());

        var overview = await service.FeedAsync(AccountId, id, "fruit-vigor-small", CancellationToken);

        Assert.Equal(["update", $"pet:update:{id}"], log);
        Assert.Equal(["begin", "commit"], unitOfWork.Log);
        Assert.Equal(1980, (await StoredAsync())!.Money);
        var fed = await FindPetAsync(character, id);
        Assert.Equal((12, 95, 4, Now), (fed.Attack, fed.Satiety, fed.Intimacy, fed.HungerSince));
        Assert.Equal(fed, Assert.Single(overview.Pets).Pet);
    }

    // 規格 pets：Feeding raises the pet——100 錢吃 20 錢的果實剩 80
    [Fact]
    public async Task Feed_SpecExampleCharges20Of100()
    {
        var character = await characters.AddAsync(
            TestCharacters.Default(AccountId, $"測試者{AccountId}", 1, 1, Now) with { Money = 100 }, CancellationToken);
        var id = await AddPetAsync(character, TestPets.Create());

        await service.FeedAsync(AccountId, id, "fruit-vigor-small", CancellationToken);

        Assert.Equal(80, Assert.IsType<Character>(await StoredAsync()).Money);
    }

    [Fact]
    public async Task Feed_OverOwnerChangesNothing()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create(attack: 11));
        var before = await FindPetAsync(character, id);

        var error = await Assert.ThrowsAsync<InputValidationException>(
            () => service.FeedAsync(AccountId, id, "fruit-vigor-small", CancellationToken));

        Assert.Equal(["overOwner"], error.Reasons);
        Assert.Empty(log);
        Assert.Equal(character, await StoredAsync());
        Assert.Equal(before, await FindPetAsync(character, id));
    }

    // 主人能力含裝備：裝上 test-sword（at +8）後 At 20，Attack 11 + 2 = 13 可以
    [Fact]
    public async Task Feed_OwnerCapIncludesEquipment()
    {
        var character = await AddCharacterAsync();
        var sword = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword", 1), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);
        var id = await AddPetAsync(character, TestPets.Create(attack: 11));

        await service.FeedAsync(AccountId, id, "fruit-vigor-small", CancellationToken);

        Assert.Equal(13, (await FindPetAsync(character, id)).Attack);
    }

    [Fact]
    public async Task Feed_ConflictWritesNoPet()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create());
        var before = await FindPetAsync(character, id);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.FeedAsync(AccountId, id, "fruit-vigor-small", CancellationToken));

        Assert.Empty(log);
        Assert.Equal(before, await FindPetAsync(character, id));
    }

    [Fact]
    public async Task Feed_UnknownFoodIsRejected()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create());

        var error = await Assert.ThrowsAsync<InputValidationException>(() => service.FeedAsync(AccountId, id, "vip-fruit", CancellationToken));

        Assert.Equal(["foodUnknown"], error.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Carry_SwitchesTheCarriedPet()
    {
        var character = await AddCharacterAsync();
        var first = await AddPetAsync(character, TestPets.Create(name: "甲", carried: true));
        var second = await AddPetAsync(character, TestPets.Create(name: "乙", carried: false));

        var overview = await service.CarryAsync(AccountId, second, CancellationToken);

        Assert.Equal(["update", $"pet:update:{first}", $"pet:update:{second}"], log);
        Assert.Equal([(first, false), (second, true)], overview.Pets.Select(view => (view.Pet.Id, view.Pet.Carried)));
        Assert.Equal([false, true], (await pets.ListAsync(character.Id, CancellationToken)).Select(pet => pet.Carried));
    }

    [Fact]
    public async Task Store_PutsThePetInTheStable()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create(carried: true));

        await service.StoreAsync(AccountId, id, CancellationToken);

        Assert.Equal(["update", $"pet:update:{id}"], log);
        Assert.False((await FindPetAsync(character, id)).Carried);
    }

    [Fact]
    public async Task Rename_TrimsTheName()
    {
        var character = await AddCharacterAsync();
        var id = await AddPetAsync(character, TestPets.Create());

        await service.RenameAsync(AccountId, id, "  小風  ", CancellationToken);

        Assert.Equal(["update", $"pet:update:{id}"], log);
        Assert.Equal("小風", (await FindPetAsync(character, id)).Name);

        var error = await Assert.ThrowsAsync<InputValidationException>(() => service.RenameAsync(AccountId, id, " ", CancellationToken));
        Assert.Equal(["nameInvalid"], error.Reasons);
    }

    [Fact]
    public async Task Release_RemovesThePet()
    {
        var character = await AddCharacterAsync();
        var kept = await AddPetAsync(character, TestPets.Create(name: "甲"));
        var released = await AddPetAsync(character, TestPets.Create(name: "乙", carried: false));

        var overview = await service.ReleaseAsync(AccountId, released, CancellationToken);

        Assert.Equal(["update", $"pet:remove:{released}"], log);
        Assert.Equal([kept], overview.Pets.Select(view => view.Pet.Id));
        Assert.Equal([kept], (await pets.ListAsync(character.Id, CancellationToken)).Select(pet => pet.Id));
    }

    // 不存在的 id 與別的角色的寵物都一樣
    [Fact]
    public async Task Pets_UnknownPetIsRejected()
    {
        await AddCharacterAsync();
        var other = await AddCharacterAsync(accountId: 8);
        var othersPet = await AddPetAsync(other, TestPets.Create());

        foreach (var petId in new[] { othersPet, 999 })
        {
            Assert.Equal(["petUnknown"], (await Assert.ThrowsAsync<InputValidationException>(
                () => service.CarryAsync(AccountId, petId, CancellationToken))).Reasons);
            Assert.Equal(["petUnknown"], (await Assert.ThrowsAsync<InputValidationException>(
                () => service.StoreAsync(AccountId, petId, CancellationToken))).Reasons);
            Assert.Equal(["petUnknown"], (await Assert.ThrowsAsync<InputValidationException>(
                () => service.RenameAsync(AccountId, petId, "阿雪", CancellationToken))).Reasons);
            Assert.Equal(["petUnknown"], (await Assert.ThrowsAsync<InputValidationException>(
                () => service.ReleaseAsync(AccountId, petId, CancellationToken))).Reasons);
            Assert.Equal(["petUnknown"], (await Assert.ThrowsAsync<InputValidationException>(
                () => service.FeedAsync(AccountId, petId, "fruit-vigor-small", CancellationToken))).Reasons);
        }

        Assert.Empty(log);
        Assert.Single(await pets.ListAsync(other.Id, CancellationToken));
    }

    private sealed class HalfRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.5);
    }
}
