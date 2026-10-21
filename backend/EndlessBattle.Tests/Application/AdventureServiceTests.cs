using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class AdventureServiceTests
{
    private const long AccountId = 7;
    private const long StartingExp = 100;
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCharacterRepository repository = new();
    private readonly FakeClock clock = new(Start);
    private readonly AdventureService service;

    public AdventureServiceTests()
    {
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        service = new AdventureService(repository, content, clock, new ScriptedRandomFactory());
        repository.AddAsync(TestCharacters.Default(AccountId, "測試者", "test-stage-1", Start) with { Exp = StartingExp }, CancellationToken.None).Wait();
    }

    // 手算：0.99 讓妖怪一律抽到 index 1（test-wolf）且不會心。
    // 預設角色 At 12（HpMax 350、Df 10，撐得住）對防禦 9：每下 144/21 = 6.86 → 7；寒狼 95 血，第 14 秒倒下；休息到第 15 秒。
    // 第 15 秒開打的第二場跨過第 20 秒，照樣打完：第 29 秒倒下，休息到第 30 秒。
    // 計畫 4 Task 5 移除：橋接期間獎勵為 經驗 Lv5×2 = 10、金錢 5
    [Fact]
    public async Task Advance_SettlesThroughLastStartedFight()
    {
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, TestContext.Current.CancellationToken);

        Assert.Equal(20, result.ExperienceGained);
        Assert.Equal(10, result.SpiritStonesGained);
        var stored = await repository.FindByAccountAsync(AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(Start.AddSeconds(30), stored!.LastSettledAt);
        Assert.Equal(StartingExp + 20, stored.Exp);
        Assert.Equal(2010, stored.Money);
    }

    [Fact]
    public async Task Advance_SettlesToNow_WhenCapped()
    {
        clock.Advance(TimeSpan.FromHours(1));

        var result = await service.AdvanceAsync(AccountId, TestContext.Current.CancellationToken);

        var stored = await repository.FindByAccountAsync(AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(clock.UtcNow, stored!.LastSettledAt);
        Assert.All(result.Events, battleEvent => Assert.True(
            battleEvent.AtSeconds <= AdventureService.MaxSimulatedSeconds + AdventureSimulator.BattleTimeLimitSeconds));
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.99);
    }
}
