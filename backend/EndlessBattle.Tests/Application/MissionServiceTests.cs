using EndlessBattle.Application.Common;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-hunt、test-sprite-bounty、test-delivery、test-chief-hunt（Lv1 一般任務）、
// test-skill-reward（Lv5、戰士、前置 test-hunt）、test-daily（每日）、test-daily-pet（每日交寵物：test-sprite 或 test-wolf）、
// test-pet-reward（擊敗 test-sprite ×1、寵物 test-wolf 倍率 2）、test-stone-reward（擊敗 test-sprite ×1、獎勵 test-stone ×1）。現在是 2026-10-22T02:00Z ＝ 週期 2026-10-22
public class MissionServiceTests
{
    private const long AccountId = 7;
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 22);

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryMissionRepository missions;
    private readonly InMemoryInventoryRepository inventory;
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemorySkillBookRepository skillBook;
    private readonly InMemoryPetRepository pets;
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly FakeClock clock = new(Now);
    private readonly MissionService service;

    public MissionServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        missions = new InMemoryMissionRepository(log);
        inventory = new InMemoryInventoryRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        skillBook = new InMemorySkillBookRepository(log);
        pets = new InMemoryPetRepository(log);
        unitOfWork = new InMemoryUnitOfWork(characters, inventory, missions, pets);
        service = Service(Content);
    }

    private MissionService Service(JsonGameContent content) => new(
        characters, missions, inventory, equipment, skillBook, pets, content, clock, new FixedSeedRandomFactory(1), unitOfWork);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> AddCharacterAsync() =>
        await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch), CancellationToken);

    // 預置紀錄後清空 log，只留下用例本身的寫入
    private async Task SeedAsync(Character character, params MissionRecord[] records)
    {
        foreach (var record in records)
        {
            await missions.AddAsync(character.Id, record, CancellationToken);
        }

        log.Clear();
    }

    private static MissionRecord Active(string id, int progress = 0) => new(id, MissionState.Active, [progress], null, Now.AddHours(-1), null);

    private Task<IReadOnlyList<MissionRecord>> RecordsAsync(Character character) => missions.ListAsync(character.Id, CancellationToken);

    private async Task<long> VersionAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version;

    private async Task<Character> CurrentAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    private Task<long> GiveAsync(Character character, string itemId, int quantity) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity), CancellationToken);

    private Task<IReadOnlyList<InventoryEntry>> BagAsync(Character character) => inventory.ListAsync(character.Id, CancellationToken);

    // 材料分類 15 格：14 格 item-1378 ×99 ＋ 1 格 test-ore
    private async Task FillMaterialsAsync(Character character, int ore)
    {
        for (var slot = 0; slot < 14; slot++)
        {
            await GiveAsync(character, "item-1378", 99);
        }

        await GiveAsync(character, "test-ore", ore);
    }

    [Fact]
    public async Task Accept_WritesCharacterVersionBeforeMission()
    {
        var character = await AddCharacterAsync();

        var board = await service.AcceptAsync(AccountId, "test-hunt", CancellationToken);

        Assert.Equal(["update", "mission:add:test-hunt"], log);
        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal(("test-hunt", MissionState.Active, 0, (DateOnly?)null, Now, (DateTimeOffset?)null), (record.MissionId, record.State, record.ProgressOf(0), record.DailyPeriod, record.AcceptedAt, record.CompletedAt));
        Assert.Equal("test-hunt", Assert.Single(board.Active).Definition.Id);
        Assert.DoesNotContain(board.Available, entry => entry.Definition.Id == "test-hunt");
    }

    [Fact]
    public async Task Accept_RejectsWithReasons()
    {
        var character = await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(
            () => service.AcceptAsync(AccountId, "test-skill-reward", CancellationToken));

        Assert.Equal(["levelTooLow", "prerequisiteMissing"], exception.Reasons);
        Assert.Empty(await RecordsAsync(character));
        Assert.Equal(0, await VersionAsync());
        Assert.Empty(log);
    }

    [Fact]
    public async Task Accept_DailyStoresTodaysPeriod()
    {
        var character = await AddCharacterAsync();

        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal(Today, record.DailyPeriod);
    }

    // 看板只讀，不刪過期紀錄；接取時才順手刪掉，再新增本週期的紀錄
    [Fact]
    public async Task Accept_ReplacesExpiredDailyRecord()
    {
        var character = await AddCharacterAsync();
        var yesterday = Today.AddDays(-1);
        await SeedAsync(character, new MissionRecord("test-daily", MissionState.Completed, [2], yesterday, Now.AddDays(-1), Now.AddDays(-1)));

        var before = await service.GetBoardAsync(AccountId, CancellationToken);
        Assert.Empty(log);
        Assert.Contains(before.Available, entry => entry.Definition.Id == "test-daily");

        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        Assert.Equal(["update", "mission:remove:test-daily", "mission:add:test-daily"], log);
        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Active, 0, (DateOnly?)Today), (record.State, record.ProgressOf(0), record.DailyPeriod));
    }

    // 定義從一般改成每日後，留下的無週期紀錄看板看不到；接取時比照過期紀錄先刪掉，不會撞唯一鍵而 409
    [Fact]
    public async Task Accept_ReplacesKindChangedRecord()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, new MissionRecord("test-daily", MissionState.Active, [1], DailyPeriod: null, Now.AddDays(-3), null));

        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        Assert.Equal(["update", "mission:remove:test-daily", "mission:add:test-daily"], log);
        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Active, 0, (DateOnly?)Today), (record.State, record.ProgressOf(0), record.DailyPeriod));
    }

    [Fact]
    public async Task Abandon_RemovesActiveAndAllowsReaccept()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, Active("test-hunt", progress: 2));

        var abandoned = await service.AbandonAsync(AccountId, "test-hunt", CancellationToken);

        Assert.Equal(["update", "mission:remove:test-hunt"], log);
        Assert.Empty(abandoned.Active);
        Assert.Contains(abandoned.Available, entry => entry.Definition.Id == "test-hunt");

        await service.AcceptAsync(AccountId, "test-hunt", CancellationToken);

        Assert.Equal(0, Assert.Single(await RecordsAsync(character)).ProgressOf(0));
    }

    [Fact]
    public async Task Abandon_RejectsCompleted()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, new MissionRecord("test-hunt", MissionState.Completed, [3], null, Now.AddHours(-2), Now.AddHours(-1)));

        var exception = await Assert.ThrowsAsync<InputValidationException>(
            () => service.AbandonAsync(AccountId, "test-hunt", CancellationToken));

        Assert.Equal(["missionNotActive"], exception.Reasons);
        Assert.Single(await RecordsAsync(character));
        Assert.Empty(log);
    }

    [Fact]
    public async Task Board_IgnoresMissionsMissingFromContent()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, Active("test-hunt"), Active("test-sprite-bounty"), Active("removed-mission"));

        var board = await service.GetBoardAsync(AccountId, CancellationToken);
        Assert.Equal(["test-hunt", "test-sprite-bounty"], board.Active.Select(entry => entry.Definition.Id));

        var accepted = await service.AcceptAsync(AccountId, "test-delivery", CancellationToken);

        Assert.Equal(3, accepted.Active.Count);
        Assert.Equal(4, (await RecordsAsync(character)).Count);
    }

    [Fact]
    public async Task Board_OwnedCountsBagOnly()
    {
        var character = await AddCharacterAsync();
        var equipped = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-ore", 2), CancellationToken);
        await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-ore", 3), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, equipped, null, CancellationToken);

        var board = await service.GetBoardAsync(AccountId, CancellationToken);

        Assert.Equal(1, board.Owned["test-sword"]);
        Assert.Equal(5, board.Owned["test-ore"]);
    }

    [Fact]
    public async Task Accept_ConflictsWhenCharacterVersionIsStale()
    {
        var character = await AddCharacterAsync();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.AcceptAsync(AccountId, "test-hunt", CancellationToken));

        Assert.Empty(await RecordsAsync(character));
        Assert.Empty(log);
    }

    [Fact]
    public async Task Complete_KillMissionGrantsRewardsOnce()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, Active("test-hunt", progress: 3));

        var completion = await service.CompleteAsync(AccountId, "test-hunt", CancellationToken);

        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Completed, (DateTimeOffset?)Now), (record.State, record.CompletedAt));
        var after = await CurrentAsync();
        Assert.Equal((character.Money + 50, character.Experience + 190), (after.Money, after.Experience));
        Assert.Equal(("謝謝你。", 190L, 50L, 0), (completion.CompletionMessage, completion.Experience, completion.Money, completion.LevelsGained));
        Assert.Empty(completion.Items);
        Assert.Null(completion.Skill);
        Assert.False(completion.SkillGranted);
        Assert.Equal("test-hunt", Assert.Single(completion.Board.Completed).Definition.Id);

        var again = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-hunt", CancellationToken));

        Assert.Equal(["missionNotActive"], again.Reasons);
        Assert.Equal(after.Money, (await CurrentAsync()).Money);
    }

    [Fact]
    public async Task Complete_RejectsIncompleteKills()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, Active("test-hunt", progress: 2));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-hunt", CancellationToken));

        Assert.Equal(["objectiveIncomplete"], exception.Reasons);
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Equal(0, await VersionAsync());
        Assert.Empty(log);
    }

    [Fact]
    public async Task Complete_RejectsMissingItems()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-ore", 2);
        await SeedAsync(character, Active("test-delivery"));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-delivery", CancellationToken));

        Assert.Equal(["itemsMissing"], exception.Reasons);
        Assert.Equal(2, Assert.Single(await BagAsync(character)).Quantity);
        Assert.Empty(log);
    }

    // 交付清掉 test-ore 那一格，獎勵 safe-refine-stone 才放得進去
    [Fact]
    public async Task Complete_DeliveryFreesSlotForReward()
    {
        var character = await AddCharacterAsync();
        await FillMaterialsAsync(character, ore: 3);
        await SeedAsync(character, Active("test-delivery"));

        var completion = await service.CompleteAsync(AccountId, "test-delivery", CancellationToken);

        var bag = await BagAsync(character);
        Assert.DoesNotContain(bag, entry => entry.ItemId == "test-ore");
        Assert.Equal(1, Assert.Single(bag, entry => entry.ItemId == "safe-refine-stone").Quantity);
        Assert.Equal(15, bag.Count);
        Assert.Equal([new ItemQuantity("safe-refine-stone", 1)], completion.Items);
        Assert.Equal(MissionState.Completed, Assert.Single(await RecordsAsync(character)).State);
    }

    // 交付後 test-ore 還剩 2、格子沒空出來 → 整筆不寫
    [Fact]
    public async Task Complete_BagFullRollsBackEverything()
    {
        var character = await AddCharacterAsync();
        await FillMaterialsAsync(character, ore: 5);
        await SeedAsync(character, Active("test-delivery"));
        var before = await BagAsync(character);

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-delivery", CancellationToken));

        Assert.Equal(["bagFull"], exception.Reasons);
        Assert.Equal(before, await BagAsync(character));
        Assert.Equal(5, Assert.Single(await BagAsync(character), entry => entry.ItemId == "test-ore").Quantity);
        Assert.Equal(character, await CurrentAsync());
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Empty(log);
    }

    // 回報的錯誤順序：同時缺寵物、技能、背包空間時先報寵物，補上一項才輪到下一項
    [Fact]
    public async Task Complete_ReportsUnmetConditionsInOrder()
    {
        var character = await AddCharacterAsync();
        for (var slot = 0; slot < InventoryRules.SlotsPerCategory; slot++)
        {
            await GiveAsync(character, "test-stone", InventoryRules.StackLimit);
        }

        await SeedAsync(character, Active("test-order-gate"));
        Task<IReadOnlyList<string>> ReasonsAsync() => CompleteReasonsAsync("test-order-gate");

        Assert.Equal(["petMissing"], await ReasonsAsync());
        await GivePetAsync(character, "test-wolf", carried: true);
        Assert.Equal(["skillTooLow"], await ReasonsAsync());
        await skillBook.UpsertAsync(character.Id, new LearnedSkill("burst", 2), CancellationToken);
        Assert.Equal(["bagFull"], await ReasonsAsync());
    }

    private async Task<IReadOnlyList<string>> CompleteReasonsAsync(string missionId) =>
        (await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, missionId, CancellationToken))).Reasons;

    // 獎勵是魔石：魔石分類 15 格都滿疊、道具分類還空著 → 400 bagFull，什麼都不寫
    [Fact]
    public async Task Complete_RejectsWhenRewardCategoryFull()
    {
        var character = await AddCharacterAsync();
        for (var slot = 0; slot < InventoryRules.SlotsPerCategory; slot++)
        {
            await GiveAsync(character, "test-stone", InventoryRules.StackLimit);
        }

        await SeedAsync(character, Active("test-stone-reward", progress: 1));
        var before = await BagAsync(character);

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-stone-reward", CancellationToken));

        Assert.Equal(["bagFull"], exception.Reasons);
        Assert.Equal(before, await BagAsync(character));
        Assert.Equal(character, await CurrentAsync());
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Complete_SkillRewardLearnsLevelOne()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, Active("test-skill-reward", progress: 2));

        var completion = await service.CompleteAsync(AccountId, "test-skill-reward", CancellationToken);

        Assert.Equal([new LearnedSkill("iron-skin", 1)], await skillBook.ListAsync(character.Id, CancellationToken));
        Assert.True(completion.SkillGranted);
        Assert.Equal(new SkillReward("iron-skin", 1), completion.Skill);
        Assert.Equal(["update", "book:iron-skin:1", "mission:complete:test-skill-reward"], log);
    }

    [Fact]
    public async Task Complete_KnownSkillStillCompletes()
    {
        var character = await AddCharacterAsync();
        await skillBook.UpsertAsync(character.Id, new LearnedSkill("iron-skin", 2), CancellationToken);
        await SeedAsync(character, Active("test-skill-reward", progress: 2));

        var completion = await service.CompleteAsync(AccountId, "test-skill-reward", CancellationToken);

        Assert.False(completion.SkillGranted);
        Assert.Equal([new LearnedSkill("iron-skin", 2)], await skillBook.ListAsync(character.Id, CancellationToken));
        Assert.Equal(character.Money + 250, (await CurrentAsync()).Money);
        Assert.Equal(["update", "mission:complete:test-skill-reward"], log);
    }

    [Fact]
    public async Task Complete_DailyReacceptOnlyAfterReset()
    {
        var character = await AddCharacterAsync();
        var accepted = await service.AcceptAsync(AccountId, "test-daily", CancellationToken);
        var required = Assert.Single(accepted.Active).Objective.Kills[0].Count;
        await missions.UpdateProgressAsync(character.Id, "test-daily", [required], CancellationToken);

        await service.CompleteAsync(AccountId, "test-daily", CancellationToken);
        var sameDay = await Assert.ThrowsAsync<InputValidationException>(() => service.AcceptAsync(AccountId, "test-daily", CancellationToken));

        Assert.Equal(["alreadyCompleted"], sameDay.Reasons);

        clock.Advance(TimeSpan.FromHours(23));
        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Active, 0, (DateOnly?)new DateOnly(2026, 10, 23)), (record.State, record.ProgressOf(0), record.DailyPeriod));
    }

    // 角色（版本鎖）最先寫，之後才刪過期每日紀錄、交付、放獎勵、完成任務
    [Fact]
    public async Task Complete_WritesCharacterVersionFirst()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 3);
        var yesterday = Today.AddDays(-1);
        await SeedAsync(character,
            new MissionRecord("test-daily", MissionState.Completed, [2], yesterday, Now.AddDays(-1), Now.AddDays(-1)),
            Active("test-delivery"));

        await service.CompleteAsync(AccountId, "test-delivery", CancellationToken);

        Assert.Equal(
            ["update", "mission:remove:test-daily", $"remove:{ore}", "add:safe-refine-stone", "mission:complete:test-delivery"],
            log);
        Assert.Equal(1, await VersionAsync());
    }

    [Fact]
    public async Task Complete_ConflictsWhenCharacterVersionIsStale()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-ore", 3);
        await SeedAsync(character, Active("test-delivery"));
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.CompleteAsync(AccountId, "test-delivery", CancellationToken));

        Assert.Equal(3, Assert.Single(await BagAsync(character)).Quantity);
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Empty(log);
    }

    // Lv4 完成 test-hunt：900 + 190 > 1000 升到 Lv5，回傳的看板已能接 test-skill-reward（Lv5、前置 test-hunt）
    [Fact]
    public async Task Complete_BoardReflectsLevelUp()
    {
        var character = await characters.AddAsync(
            TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with { Level = 4, Experience = 900 }, CancellationToken);
        await SeedAsync(character, Active("test-hunt", progress: 3));

        var completion = await service.CompleteAsync(AccountId, "test-hunt", CancellationToken);

        Assert.Equal(1, completion.LevelsGained);
        Assert.Equal(5, (await CurrentAsync()).Level);
        Assert.Contains(completion.Board.Available, entry => entry.Definition.Id == "test-skill-reward");
    }

    // 第一個讓 test-daily-pet 擲到指定物種的週期（從今天起找），時鐘移到該週期內
    private DateOnly MoveToPetPeriod(Character character, string species, JsonGameContent? content = null)
    {
        var mission = (content ?? Content).GetMission("test-daily-pet");
        var period = Enumerable.Range(0, 60)
            .Select(offset => Today.AddDays(offset))
            .First(day => DailyMissions.Resolve(mission, character.Id, day).DeliverPet == species);
        clock.Advance(DailyMissions.PeriodStart(period).AddHours(1) - clock.UtcNow);
        return period;
    }

    private async Task<long> GivePetAsync(Character character, string monsterId, bool carried) =>
        await pets.AddAsync(character.Id, TestPets.Create(monsterId: monsterId, carried: carried), CancellationToken);

    private Task<IReadOnlyList<Pet>> PetsAsync(Character character) => pets.ListAsync(character.Id, CancellationToken);

    [Fact]
    public async Task Complete_DeliversCarriedPet()
    {
        var character = await AddCharacterAsync();
        var period = MoveToPetPeriod(character, "test-wolf");
        var wolf = await GivePetAsync(character, "test-wolf", carried: true);
        var stored = await GivePetAsync(character, "test-wolf", carried: false);
        var accepted = await service.AcceptAsync(AccountId, "test-daily-pet", CancellationToken);
        Assert.True(Assert.Single(accepted.Active).Ready);
        var medal = Assert.Single(DailyMissions.Resolve(Content.GetMission("test-daily-pet"), character.Id, period).Reward.Items);
        log.Clear();

        var completion = await service.CompleteAsync(AccountId, "test-daily-pet", CancellationToken);

        Assert.Equal([stored], (await PetsAsync(character)).Select(pet => pet.Id));
        Assert.Equal(medal.Quantity, Assert.Single(await BagAsync(character), entry => entry.ItemId == medal.ItemId).Quantity);
        Assert.Equal(["update", $"add:{medal.ItemId}", $"pet:remove:{wolf}", "mission:complete:test-daily-pet"], log);
        Assert.Null(completion.Pet);
        Assert.Equal(MissionState.Completed, Assert.Single(await RecordsAsync(character)).State);
    }

    // 獸欄裡有對的物種也不行，要攜帶中的
    [Fact]
    public async Task Complete_RejectsWrongCarriedPet()
    {
        var character = await AddCharacterAsync();
        MoveToPetPeriod(character, "test-wolf");
        await GivePetAsync(character, "test-sprite", carried: true);
        await GivePetAsync(character, "test-wolf", carried: false);
        var accepted = await service.AcceptAsync(AccountId, "test-daily-pet", CancellationToken);
        Assert.False(Assert.Single(accepted.Active).Ready);
        var before = await PetsAsync(character);
        var version = await VersionAsync();
        log.Clear();

        var error = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-daily-pet", CancellationToken));

        Assert.Equal(["petMissing"], error.Reasons);
        Assert.Equal(before, await PetsAsync(character));
        Assert.Empty(await BagAsync(character));
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Equal(version, await VersionAsync());
        Assert.Empty(log);
    }

    [Fact]
    public async Task Complete_PetRewardAddsStoredPet()
    {
        var character = await AddCharacterAsync();
        await GivePetAsync(character, "test-sprite", carried: true);
        await SeedAsync(character, Active("test-pet-reward", progress: 1));

        var completion = await service.CompleteAsync(AccountId, "test-pet-reward", CancellationToken);

        var reward = Assert.Single(await PetsAsync(character), pet => pet.MonsterId == "test-wolf");
        Assert.False(reward.Carried);
        Assert.Equal("測試寒狼", reward.Name);
        Assert.Equal((1, 80, 0), (reward.Spirit, reward.Satiety, reward.Intimacy));
        Assert.Equal(clock.UtcNow, reward.BornAt);
        Assert.All(new[] { reward.Attack, reward.Magic, reward.Defense }, stat => Assert.True(stat is >= 2 and <= 20 && stat % 2 == 0));
        Assert.Equal(new PetReward("test-wolf", 2), completion.Pet);
        Assert.Equal(["update", $"pet:add:{reward.Id}", "mission:complete:test-pet-reward"], log);
    }

    [Fact]
    public async Task Complete_PetRewardRejectedWhenPetsFull()
    {
        var character = await AddCharacterAsync();
        await GiveAsync(character, "test-ore", 2);
        for (var count = 0; count < PetRules.MaxPets; count++)
        {
            await GivePetAsync(character, "test-sprite", carried: count == 0);
        }

        await SeedAsync(character, Active("test-pet-reward", progress: 1));
        var before = await CurrentAsync();
        var pets = await PetsAsync(character);
        var bag = await BagAsync(character);

        var error = await Assert.ThrowsAsync<InputValidationException>(() => service.CompleteAsync(AccountId, "test-pet-reward", CancellationToken));

        Assert.Equal(["petsFull"], error.Reasons);
        Assert.Equal(before, await CurrentAsync());
        Assert.Equal(pets, await PetsAsync(character));
        Assert.Equal(bag, await BagAsync(character));
        Assert.Equal(MissionState.Active, Assert.Single(await RecordsAsync(character)).State);
        Assert.Empty(log);
    }

    // 交寵物的每日任務若也送寵物，以交出後的寵物數判斷：3 隻交出 1 隻，還放得下獎勵
    [Fact]
    public async Task Complete_PetDeliveryFreesRoomForPetReward()
    {
        var directory = Directory.CreateTempSubdirectory("ebo-content-");
        try
        {
            foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "TestContent")))
            {
                File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
            }

            var missionsPath = Path.Combine(directory.FullName, "missions.json");
            var json = File.ReadAllText(missionsPath);
            var medalB = "{ \"money\": 0, \"items\": [{ \"itemId\": \"test-medal-b\", \"quantity\": 1 }] }";
            Assert.Contains(medalB, json);
            File.WriteAllText(missionsPath, json.Replace(medalB, "{ \"money\": 0, \"pet\": { \"monsterId\": \"test-sprite\", \"level\": 1 } }"));
            var content = JsonGameContent.Load(directory.FullName);
            var withPetReward = Service(content);

            var character = await AddCharacterAsync();
            var period = Enumerable.Range(0, 120)
                .Select(offset => Today.AddDays(offset))
                .First(day => DailyMissions.Resolve(content.GetMission("test-daily-pet"), character.Id, day) is
                    { DeliverPet: "test-wolf", Reward.Pet: not null });
            clock.Advance(DailyMissions.PeriodStart(period).AddHours(1) - clock.UtcNow);
            var wolf = await GivePetAsync(character, "test-wolf", carried: true);
            await GivePetAsync(character, "test-sprite", carried: false);
            await GivePetAsync(character, "test-sprite", carried: false);
            await withPetReward.AcceptAsync(AccountId, "test-daily-pet", CancellationToken);

            var completion = await withPetReward.CompleteAsync(AccountId, "test-daily-pet", CancellationToken);

            var after = await PetsAsync(character);
            Assert.Equal(PetRules.MaxPets, after.Count);
            Assert.DoesNotContain(after, pet => pet.Id == wolf);
            Assert.Equal(new PetReward("test-sprite", 1), completion.Pet);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
