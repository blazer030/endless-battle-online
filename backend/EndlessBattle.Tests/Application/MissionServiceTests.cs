using EndlessBattle.Application.Common;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：test-hunt、test-sprite-bounty、test-delivery、test-chief-hunt（Lv1 一般任務）、
// test-skill-reward（Lv5、戰士、前置 test-hunt）、test-daily（每日）。現在是 2026-10-22T02:00Z ＝ 週期 2026-10-22
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
    private readonly FakeClock clock = new(Now);
    private readonly MissionService service;

    public MissionServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        missions = new InMemoryMissionRepository(log);
        inventory = new InMemoryInventoryRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        skillBook = new InMemorySkillBookRepository(log);
        var unitOfWork = new InMemoryUnitOfWork(characters, inventory, missions);
        service = new MissionService(
            characters, missions, inventory, equipment, skillBook, Content, clock, new FixedSeedRandomFactory(1), unitOfWork);
    }

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

    private static MissionRecord Active(string id, int progress = 0) => new(id, MissionState.Active, progress, null, Now.AddHours(-1), null);

    private Task<IReadOnlyList<MissionRecord>> RecordsAsync(Character character) => missions.ListAsync(character.Id, CancellationToken);

    private async Task<long> VersionAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version;

    private async Task<Character> CurrentAsync() => (await characters.FindByAccountAsync(AccountId, CancellationToken))!;

    private Task<long> GiveAsync(Character character, string itemId, int quantity) =>
        inventory.AddAsync(character.Id, TestItems.Entry(0, itemId, quantity), CancellationToken);

    private Task<IReadOnlyList<InventoryEntry>> BagAsync(Character character) => inventory.ListAsync(character.Id, CancellationToken);

    // 材料分類 15 格：14 格 stone-remover ×99 ＋ 1 格 test-ore
    private async Task FillMaterialsAsync(Character character, int ore)
    {
        for (var slot = 0; slot < 14; slot++)
        {
            await GiveAsync(character, "stone-remover", 99);
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
        Assert.Equal(new MissionRecord("test-hunt", MissionState.Active, 0, null, Now, null), record);
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
        await SeedAsync(character, new MissionRecord("test-daily", MissionState.Completed, 2, yesterday, Now.AddDays(-1), Now.AddDays(-1)));

        var before = await service.GetBoardAsync(AccountId, CancellationToken);
        Assert.Empty(log);
        Assert.Contains(before.Available, entry => entry.Definition.Id == "test-daily");

        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        Assert.Equal(["update", "mission:remove:test-daily", "mission:add:test-daily"], log);
        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Active, 0, (DateOnly?)Today), (record.State, record.Progress, record.DailyPeriod));
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

        Assert.Equal(0, Assert.Single(await RecordsAsync(character)).Progress);
    }

    [Fact]
    public async Task Abandon_RejectsCompleted()
    {
        var character = await AddCharacterAsync();
        await SeedAsync(character, new MissionRecord("test-hunt", MissionState.Completed, 3, null, Now.AddHours(-2), Now.AddHours(-1)));

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
        Assert.Equal((character.Money + 50, character.Exp + 190), (after.Money, after.Exp));
        Assert.Equal(("謝謝你。", 190L, 50L, 0), (completion.CompletionMessage, completion.Exp, completion.Money, completion.LevelsGained));
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
        var required = Assert.Single(accepted.Active).Objective.Kill!.Count;
        await missions.UpdateProgressAsync(character.Id, "test-daily", required, CancellationToken);

        await service.CompleteAsync(AccountId, "test-daily", CancellationToken);
        var sameDay = await Assert.ThrowsAsync<InputValidationException>(() => service.AcceptAsync(AccountId, "test-daily", CancellationToken));

        Assert.Equal(["alreadyCompleted"], sameDay.Reasons);

        clock.Advance(TimeSpan.FromHours(23));
        await service.AcceptAsync(AccountId, "test-daily", CancellationToken);

        var record = Assert.Single(await RecordsAsync(character));
        Assert.Equal((MissionState.Active, 0, (DateOnly?)new DateOnly(2026, 10, 23)), (record.State, record.Progress, record.DailyPeriod));
    }

    // 角色（版本鎖）最先寫，之後才刪過期每日紀錄、交付、放獎勵、完成任務
    [Fact]
    public async Task Complete_WritesCharacterVersionFirst()
    {
        var character = await AddCharacterAsync();
        var ore = await GiveAsync(character, "test-ore", 3);
        var yesterday = Today.AddDays(-1);
        await SeedAsync(character,
            new MissionRecord("test-daily", MissionState.Completed, 2, yesterday, Now.AddDays(-1), Now.AddDays(-1)),
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
            TestCharacters.Default(AccountId, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with { Level = 4, Exp = 900 }, CancellationToken);
        await SeedAsync(character, Active("test-hunt", progress: 3));

        var completion = await service.CompleteAsync(AccountId, "test-hunt", CancellationToken);

        Assert.Equal(1, completion.LevelsGained);
        Assert.Equal(5, (await CurrentAsync()).Level);
        Assert.Contains(completion.Board.Available, entry => entry.Definition.Id == "test-skill-reward");
    }
}
