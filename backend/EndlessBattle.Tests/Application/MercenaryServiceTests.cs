using EndlessBattle.Application.Common;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// 預設角色 2000 錢。TestContent：test-mercenary 100 錢、3 次；test-honour-mercenary 只能用勳章兌換；地區 1 平原、17 禁地
public class MercenaryServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryMercenaryRepository mercenaries;
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly MercenaryService service;

    public MercenaryServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        mercenaries = new InMemoryMercenaryRepository(log);
        unitOfWork = new InMemoryUnitOfWork(characters, mercenaries);
        service = new MercenaryService(characters, mercenaries, Content, new FakeClock(Now), unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, Now);
        return await characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    // 準備合約並清掉準備過程的紀錄
    private async Task<Character> HireAsync()
    {
        var character = await AddCharacterAsync();
        await service.HireAsync(AccountId, "test-mercenary", CancellationToken);
        log.Clear();
        return character;
    }

    private static Task<InputValidationException> RejectedAsync(Func<Task> action) => Assert.ThrowsAsync<InputValidationException>(action);

    [Fact]
    public async Task Hire_ChargesPriceAndStartsResting()
    {
        var character = await AddCharacterAsync();

        var overview = await service.HireAsync(AccountId, "test-mercenary", CancellationToken);

        var expected = new MercenaryContract("test-mercenary", "測試傭兵", Remaining: 3, PlaceId: null, NextFightAt: null, HiredAt: Now);
        Assert.Equal(expected, overview.Contract);
        Assert.False(overview.Contract!.Fighting);
        Assert.Equal(Content.AllMercenaries, overview.Catalogue);
        Assert.Equal(expected, await mercenaries.FindAsync(character.Id, CancellationToken));
        Assert.Equal(1900, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Equal(["update", "mercenary:add"], log);
    }

    [Fact]
    public async Task Hire_RejectsSecondMercenary()
    {
        await HireAsync();

        var error = await RejectedAsync(() => service.HireAsync(AccountId, "test-mercenary", CancellationToken));

        Assert.Equal(["mercenaryHired"], error.Reasons);
        Assert.Equal(1900, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Hire_RejectsHonourOnly()
    {
        var character = await AddCharacterAsync();

        var error = await RejectedAsync(() => service.HireAsync(AccountId, "test-honour-mercenary", CancellationToken));
        var unknown = await RejectedAsync(() => service.HireAsync(AccountId, "nobody", CancellationToken));

        Assert.Equal(["honourOnly", "notEnoughMoney"], error.Reasons);
        Assert.Equal(["mercenaryUnknown"], unknown.Reasons);
        Assert.Null(await mercenaries.FindAsync(character.Id, CancellationToken));
        Assert.Empty(log);
    }

    [Fact]
    public async Task Deploy_SchedulesFirstFightAfterInterval()
    {
        var character = await HireAsync();

        var overview = await service.DeployAsync(AccountId, placeId: 2, CancellationToken);

        Assert.Equal((2, Now.AddSeconds(100), true), (overview.Contract!.PlaceId, overview.Contract.NextFightAt, overview.Contract.Fighting));
        Assert.Equal(overview.Contract, await mercenaries.FindAsync(character.Id, CancellationToken));
        Assert.Equal(["update", "mercenary:update"], log);
    }

    [Fact]
    public async Task Deploy_RejectsRestrictedPlace()
    {
        await HireAsync();

        var restricted = await RejectedAsync(() => service.DeployAsync(AccountId, placeId: 17, CancellationToken));
        var unknown = await RejectedAsync(() => service.DeployAsync(AccountId, placeId: 999, CancellationToken));

        Assert.Equal(["placeRestricted"], restricted.Reasons);
        Assert.Equal(["placeUnknown"], unknown.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Rest_StopsFighting()
    {
        var character = await HireAsync();
        await service.DeployAsync(AccountId, placeId: 1, CancellationToken);

        var overview = await service.RestAsync(AccountId, CancellationToken);

        Assert.Equal((null, null, false), (overview.Contract!.PlaceId, overview.Contract.NextFightAt, overview.Contract.Fighting));
        Assert.Equal(overview.Contract, await mercenaries.FindAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Dismiss_RemovesContract()
    {
        var character = await HireAsync();

        var overview = await service.DismissAsync(AccountId, CancellationToken);

        Assert.Null(overview.Contract);
        Assert.Null(await mercenaries.FindAsync(character.Id, CancellationToken));
        Assert.Equal(["update", "mercenary:remove"], log);
    }

    [Fact]
    public async Task Rename_LimitsToEightCharacters()
    {
        await HireAsync();

        var renamed = await service.RenameAsync(AccountId, "  一二三四五六七八  ", CancellationToken);
        var error = await RejectedAsync(() => service.RenameAsync(AccountId, "一二三四五六七八九", CancellationToken));

        Assert.Equal("一二三四五六七八", renamed.Contract!.Name);
        Assert.Equal(["nameInvalid"], error.Reasons);
    }

    [Fact]
    public async Task Commands_RequireAContract()
    {
        await AddCharacterAsync();

        Assert.Equal(["noMercenary"], (await RejectedAsync(() => service.DeployAsync(AccountId, 1, CancellationToken))).Reasons);
        Assert.Equal(["noMercenary"], (await RejectedAsync(() => service.RestAsync(AccountId, CancellationToken))).Reasons);
        Assert.Equal(["noMercenary"], (await RejectedAsync(() => service.RenameAsync(AccountId, "阿傭", CancellationToken))).Reasons);
        Assert.Equal(["noMercenary"], (await RejectedAsync(() => service.DismissAsync(AccountId, CancellationToken))).Reasons);
        Assert.Null((await service.GetAsync(AccountId, CancellationToken)).Contract);
    }
}
