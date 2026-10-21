using EndlessBattle.Application.Characters;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class CharacterServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // 10 分鐘 = 3.33 個 180 秒週期 → 單次最多 +12：20 → 32；回復只反映在回傳值，不寫回
    [Fact]
    public async Task GetAsync_RegeneratesActNumWithoutWriting()
    {
        var repository = new InMemoryCharacterRepository();
        var service = new CharacterService(repository, new InMemoryJobProficiencyRepository(), Content, new FakeClock(Now));
        await repository.AddAsync(
            TestCharacters.Default(AccountId, "測試者", 1, 1, Now) with { ActNum = 20, ActNumTime = Now.AddMinutes(-10) },
            CancellationToken);

        var profile = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal(32, profile.Character.ActNum);
        Assert.Equal(Now, profile.Character.ActNumTime);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(20, stored!.ActNum);
        Assert.Equal(Now.AddMinutes(-10), stored.ActNumTime);
        Assert.Equal(0, stored.Version);
    }

    // 起始地取自內容檔（TestContent：地區 1、子地區 1）
    [Fact]
    public async Task Create_StartsAtContentStartingPlace()
    {
        var service = new CharacterService(
            new InMemoryCharacterRepository(), new InMemoryJobProficiencyRepository(), Content, new FakeClock(Now));

        var character = await service.CreateAsync(
            AccountId, new CreateCharacterCommand("墨客", 1, 1, 1, "warrior", new StatAllocation(4, 2, 3, 1), "烈焰斬"), CancellationToken);

        Assert.Equal((Content.StartingPlaceId, Content.StartingSubPlace), (character.PlaceId, character.SubPlace));
    }
}
