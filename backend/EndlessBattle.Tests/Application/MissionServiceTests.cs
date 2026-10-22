using EndlessBattle.Application.Common;
using EndlessBattle.Application.Missions;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Missions;
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
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly FakeClock clock = new(Now);
    private readonly MissionService service;

    public MissionServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        missions = new InMemoryMissionRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        var unitOfWork = new InMemoryUnitOfWork(characters, inventory, missions);
        service = new MissionService(characters, missions, inventory, equipment, Content, clock, unitOfWork);
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
}
