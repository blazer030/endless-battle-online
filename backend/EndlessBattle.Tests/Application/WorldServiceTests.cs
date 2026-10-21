using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.World;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// TestContent：1 測試平原 1–10（四個子地區）、2 測試洞窟 6–15、20 測試神廟 30–40（無子地區）、17 測試禁地（禁區）
public class WorldServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly FakeClock clock = new(Start);
    private readonly WorldService service;

    public WorldServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        var adventure = new AdventureService(
            characters, proficiencies, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(),
            new InMemoryEquipmentRepository(new InMemoryInventoryRepository()), Content, clock, new ZeroRandomFactory());
        service = new WorldService(characters, Content, clock, adventure);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, Start);
        return characters.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    // 等級剛好等於上下限也算推薦
    [Theory]
    [InlineData(5, 1, Recommendation.Recommended)]
    [InlineData(5, 2, Recommendation.TooHard)]
    [InlineData(25, 1, Recommendation.TooEasy)]
    [InlineData(1, 1, Recommendation.Recommended)]
    [InlineData(10, 1, Recommendation.Recommended)]
    [InlineData(11, 1, Recommendation.TooEasy)]
    [InlineData(5, 17, Recommendation.None)]
    [InlineData(70, 17, Recommendation.None)]
    public async Task World_MarksRecommendation(int level, int placeId, Recommendation expected)
    {
        await AddCharacterAsync(character => character with { Level = level });

        var areas = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(expected, areas.SelectMany(area => area.Places).Single(place => place.Id == placeId).Recommendation);
    }

    [Fact]
    public async Task Move_UpdatesPlaceAndSubPlace()
    {
        await AddCharacterAsync();

        var profile = await service.MoveAsync(AccountId, 2, 3, CancellationToken);

        Assert.Equal((2, 3), (profile.Character.PlaceId, profile.Character.SubPlace));
        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((2, 3), (stored!.PlaceId, stored.SubPlace));
        Assert.Equal(1, stored.Version);
    }

    // 已過 20 秒：若先推進了，LastSettledAt 會往前並寫入；被擋下時什麼都不寫
    [Fact]
    public async Task Move_RejectsRestrictedPlace()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 17, 1, CancellationToken));

        Assert.Equal(["restricted"], exception.Reasons);
        Assert.Empty(log);
        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((1, 1, Start), (stored!.PlaceId, stored.SubPlace, stored.LastSettledAt));
    }

    [Fact]
    public async Task Move_RejectsSubPlace_ForPlaceWithoutSubPlaces()
    {
        await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 20, 1, CancellationToken));

        Assert.Equal(["subPlaceNotAllowed"], exception.Reasons);
        Assert.Empty(log);
        var profile = await service.MoveAsync(AccountId, 20, null, CancellationToken);
        Assert.Equal((20, (int?)null), (profile.Character.PlaceId, profile.Character.SubPlace));
    }

    [Fact]
    public async Task Move_RequiresSubPlace_WhenPlaceHasThem()
    {
        await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 2, null, CancellationToken));

        Assert.Equal(["subPlaceRequired"], exception.Reasons);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Move_RejectsSubPlaceOutOfRange(int subPlace)
    {
        await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 2, subPlace, CancellationToken));

        Assert.Equal(["subPlaceOutOfRange"], exception.Reasons);
        Assert.Empty(log);
    }

    // 同地區換子地區可以；地區與子地區都相同才擋
    [Fact]
    public async Task Move_RejectsSamePlace()
    {
        await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 1, 1, CancellationToken));

        Assert.Equal(["samePlace"], exception.Reasons);
        Assert.Empty(log);
        var profile = await service.MoveAsync(AccountId, 1, 2, CancellationToken);
        Assert.Equal((1, (int?)2), (profile.Character.PlaceId, profile.Character.SubPlace));
    }

    [Fact]
    public async Task Move_RejectsUnknownPlace()
    {
        await AddCharacterAsync();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 99, 1, CancellationToken));

        Assert.Equal(["unknownPlace"], exception.Reasons);
        Assert.Empty(log);
    }

    // 擲骰全 0：舊地區 1 只遇到測試雪精（每場 2.8 秒、經驗 116、熟練度 12，見 AdventureServiceTests）。
    // 20 秒打 8 場、結算到 22.4 秒：經驗 928、熟練度 96；若先搬到地區 2 才結算，會打測試蝙蝠（熟練度 14）
    [Fact]
    public async Task Move_SettlesOldPlaceFirst()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));

        var profile = await service.MoveAsync(AccountId, 2, 1, CancellationToken);

        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((2, (int?)1), (stored!.PlaceId, stored.SubPlace));
        Assert.Equal(Start.AddSeconds(22.4), stored.LastSettledAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(928, stored.Exp);
        Assert.Equal(2, stored.Version);
        Assert.Equal([new JobProficiency("warrior", 96, 96)], profile.JobProficiency);
        Assert.Equal(stored.Exp, profile.Character.Exp);
    }

    // 行動力 10、上次回復在 Start；400 秒後移動（剛結算過，推進不動）→ 回應 10 + 12 = 22（唯讀，存檔仍是 10）
    [Fact]
    public async Task Move_ReturnsRegeneratedActNum()
    {
        clock.Advance(TimeSpan.FromSeconds(400));
        await AddCharacterAsync(character => character with { ActNum = 10, ActNumTime = Start, LastSettledAt = clock.UtcNow });

        var profile = await service.MoveAsync(AccountId, 2, 1, CancellationToken);

        Assert.Equal(22, profile.Character.ActNum);
        Assert.Equal(10, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.ActNum);
    }

    // 推進撞上別的請求（輪詢）時直接丟 409，位置不變
    [Fact]
    public async Task Move_PropagatesConflict_WhenSettlingRaces()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.MoveAsync(AccountId, 2, 1, CancellationToken));

        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((1, (int?)1, 0), (stored!.PlaceId, stored.SubPlace, stored.Version));
    }

    private sealed class ZeroRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }
}
