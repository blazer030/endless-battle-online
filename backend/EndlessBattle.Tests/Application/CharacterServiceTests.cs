using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class CharacterServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static InMemoryEquipmentRepository NoEquipment() => new(new InMemoryInventoryRepository());

    // 冒險推進與角色服務共用同一組 repository
    private static (CharacterService Characters, AdventureService Adventure) Services(
        InMemoryCharacterRepository repository,
        InMemoryJobProficiencyRepository proficiencies,
        InMemoryEquipmentRepository equipment,
        FakeClock clock,
        InMemoryInventoryRepository? inventory = null)
    {
        var adventure = new AdventureService(
            repository, proficiencies, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(),
            inventory ?? new InMemoryInventoryRepository(), equipment, new InMemoryMissionRepository(), new InMemoryPetRepository(),
            new InMemoryMercenaryRepository(), new InMemoryAreaPassRepository(), new InMemoryBuffRepository(), new InMemoryLodgingRepository(), Content, clock, new FixedSeedRandomFactory(20261022), new InMemoryUnitOfWork());
        return (new CharacterService(repository, proficiencies, equipment, new InMemoryBuffRepository(), Content, clock, adventure), adventure);
    }

    private static CharacterService Service(InMemoryCharacterRepository repository, InMemoryEquipmentRepository equipment, FakeClock clock) =>
        Services(repository, new InMemoryJobProficiencyRepository(), equipment, clock).Characters;

    // 起始地取自內容檔（TestContent：地區 1、子地區 1）
    [Fact]
    public async Task Create_StartsAtContentStartingPlace()
    {
        var service = Service(new InMemoryCharacterRepository(), NoEquipment(), new FakeClock(Now));

        var character = await service.CreateAsync(
            AccountId, new CreateCharacterCommand("墨客", 1, 1, 1, "warrior", new StatAllocation(4, 2, 3, 1)), CancellationToken);

        Assert.Equal((Content.StartingPlaceId, Content.StartingSubPlace), (character.PlaceId, character.SubPlace));
    }

    // 裝上測試鐵劍 at 8：個人資料帶出裝備加成，供有效能力值顯示
    [Fact]
    public async Task GetAsync_IncludesEquipmentBonus()
    {
        var repository = new InMemoryCharacterRepository();
        var inventory = new InMemoryInventoryRepository();
        var equipment = new InMemoryEquipmentRepository(inventory);
        var service = Service(repository, equipment, new FakeClock(Now));
        var character = await repository.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, Now), CancellationToken);
        var sword = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);

        var profile = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(StatBonus.Zero with { Attack = 8 }, profile.Equipment.Total);
    }

    private static readonly AdventureSettings Thirsty = new(AttackKind.Magic, PotionHealthPointsPercent: 100, PotionManaPointsPercent: -1, RestHealthPointsPercent: 20, RestManaPointsPercent: -1, EscapeHealthPointsPercent: 5);

    // SP 全滿（91／91）、沒有技能：舊門檻（SP < 100%）永遠不喝；新門檻（HP < 100%、略過 SP）幾乎每回合都會喝。
    // 存設定前先以舊門檻結算經過的 60 秒 → 補給一瓶都沒少；存完之後的推進才用新門檻
    [Fact]
    public async Task UpdateSettings_SettlesElapsedTimeWithOldSettings()
    {
        var repository = new InMemoryCharacterRepository();
        var inventory = new InMemoryInventoryRepository();
        var equipment = new InMemoryEquipmentRepository(inventory);
        var clock = new FakeClock(Now);
        var (service, adventure) = Services(repository, new InMemoryJobProficiencyRepository(), equipment, clock, inventory);
        var character = await repository.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, Now) with { ManaPoints = 91 }, CancellationToken);
        var supply = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-supply", 20), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Supply, supply, 20, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(60));

        var profile = await service.UpdateSettingsAsync(AccountId, Thirsty, CancellationToken);

        Assert.Equal(Thirsty, profile.Character.Settings);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(Thirsty, stored!.Settings);
        Assert.True(stored.LastSettledAt >= Now.AddSeconds(60));
        Assert.Equal(2, stored.Version);
        Assert.Equal(20, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);

        clock.Advance(TimeSpan.FromSeconds(60));
        var after = await adventure.AdvanceAsync(AccountId, CancellationToken);
        Assert.True(after.Summary.PotionsUsed > 0);
    }

    // 已過 20 秒：先推進的話 LastSettledAt 與版本都會變；驗證失敗時什麼都不寫
    [Theory]
    [InlineData(101)]
    [InlineData(-2)]
    public async Task UpdateSettings_RejectsOutOfRangeAndWritesNothing(int potionHpPercent)
    {
        var repository = new InMemoryCharacterRepository();
        var clock = new FakeClock(Now);
        var service = Service(repository, NoEquipment(), clock);
        await repository.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, Now), CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(20));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() =>
            service.UpdateSettingsAsync(AccountId, AdventureSettings.Default with { PotionHealthPointsPercent = potionHpPercent }, CancellationToken));

        Assert.Equal(["invalidPercent"], exception.Reasons);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((0, Now, AdventureSettings.Default), (stored!.Version, stored.LastSettledAt, stored.Settings));
    }
}
