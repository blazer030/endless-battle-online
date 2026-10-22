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

// TestContent：1 測試平原 1–10（四個子地區）、2 測試洞窟 6–15、20 測試神廟 30–40（無子地區）、17 測試禁地（禁區，test-key 3600 秒）
public class WorldServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly InMemoryInventoryRepository inventory;
    private readonly InMemoryAreaPassRepository passes;
    private readonly FakeClock clock = new(Start);
    private readonly WorldService service;

    public WorldServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        inventory = new InMemoryInventoryRepository(log);
        passes = new InMemoryAreaPassRepository(log);
        var equipment = new InMemoryEquipmentRepository(inventory);
        var unitOfWork = new InMemoryUnitOfWork(characters, proficiencies, inventory, passes);
        var adventure = new AdventureService(
            characters, proficiencies, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(),
            inventory, equipment, new InMemoryMissionRepository(), new InMemoryPetRepository(), new InMemoryMercenaryRepository(), passes,
            Content, clock, new ZeroRandomFactory(), unitOfWork);
        service = new WorldService(characters, inventory, equipment, passes, Content, clock, unitOfWork, adventure);
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

    private async Task GiveKeysAsync(long characterId, int quantity)
    {
        await inventory.AddAsync(characterId, TestItems.Entry(0, "test-key", quantity), CancellationToken);
        log.Clear();
    }

    private async Task<int> KeysAsync(long characterId) =>
        (await inventory.ListAsync(characterId, CancellationToken)).Where(entry => entry.ItemId == "test-key").Sum(entry => entry.Quantity);

    // 已過 20 秒：若先推進了，LastSettledAt 會往前並寫入；被擋下時什麼都不寫（通行證過期也不算）
    [Fact]
    public async Task Move_RejectsRestrictedWithoutKeyAndWritesNothing()
    {
        var character = await AddCharacterAsync();
        await passes.SaveAsync(character.Id, new AreaPass(17, Start), CancellationToken);
        log.Clear();
        clock.Advance(TimeSpan.FromSeconds(20));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.MoveAsync(AccountId, 17, 1, CancellationToken));

        Assert.Equal(["restricted"], exception.Reasons);
        Assert.Empty(log);
        var stored = await characters.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal((1, 1, Start), (stored!.PlaceId, stored.SubPlace, stored.LastSettledAt));
        Assert.Equal(new AreaPass(17, Start), await passes.FindAsync(character.Id, 17, CancellationToken));
    }

    [Fact]
    public async Task Move_ConsumesOneKeyAndOpensPass()
    {
        var character = await AddCharacterAsync();
        await GiveKeysAsync(character.Id, 2);

        var profile = await service.MoveAsync(AccountId, 17, 1, CancellationToken);

        Assert.Equal((17, (int?)1), (profile.Character.PlaceId, profile.Character.SubPlace));
        Assert.Equal(1, await KeysAsync(character.Id));
        var places = (await service.GetAsync(AccountId, CancellationToken)).SelectMany(area => area.Places).ToList();
        var restricted = places.Single(place => place.Id == 17);
        Assert.Equal((1, (DateTimeOffset?)Start.AddSeconds(3600)), (restricted.Keys, restricted.PassExpiresAt));
        var plain = places.Single(place => place.Id == 2);
        Assert.Equal((0, (DateTimeOffset?)null), (plain.Keys, plain.PassExpiresAt));
    }

    // 通行證未到期：免費進入，鑰匙與到期時刻都不變
    [Fact]
    public async Task Move_WithValidPassKeepsKey()
    {
        var character = await AddCharacterAsync();
        await GiveKeysAsync(character.Id, 2);
        await passes.SaveAsync(character.Id, new AreaPass(17, Start.AddSeconds(60)), CancellationToken);
        log.Clear();

        await service.MoveAsync(AccountId, 17, 2, CancellationToken);

        Assert.Equal(2, await KeysAsync(character.Id));
        Assert.Equal(new AreaPass(17, Start.AddSeconds(60)), await passes.FindAsync(character.Id, 17, CancellationToken));
        Assert.Equal(["update"], log);
    }

    // 人在禁地・禁一、通行證在 Start + 60 到期、離線到 Start + 600：到期後的時間不結算，同一子地區可以用最後一把鑰匙再次進入
    [Fact]
    public async Task Move_ReentersSameRestrictedPlaceWithKeyAfterExpiry()
    {
        var character = await AddCharacterAsync(character => character with { PlaceId = 17, SubPlace = 1 });
        await GiveKeysAsync(character.Id, 1);
        await passes.SaveAsync(character.Id, new AreaPass(17, Start.AddSeconds(60)), CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(600));

        var profile = await service.MoveAsync(AccountId, 17, 1, CancellationToken);

        Assert.Equal((17, (int?)1), (profile.Character.PlaceId, profile.Character.SubPlace));
        Assert.Equal(0, await KeysAsync(character.Id));
        Assert.Equal(new AreaPass(17, clock.UtcNow.AddSeconds(3600)), await passes.FindAsync(character.Id, 17, CancellationToken));
        Assert.Equal(clock.UtcNow, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.LastSettledAt);
    }

    // 角色版本更新是交易裡的第一個寫入，之後才扣鑰匙、寫通行證
    [Fact]
    public async Task Move_WritesCharacterVersionFirst()
    {
        var character = await AddCharacterAsync();
        await GiveKeysAsync(character.Id, 2);
        var keyId = (await inventory.ListAsync(character.Id, CancellationToken)).Single().Id;

        await service.MoveAsync(AccountId, 17, 1, CancellationToken);

        Assert.Equal(["update", $"quantity:{keyId}", "pass:17"], log);
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
