using Inkbound.Application.Adventure;
using Inkbound.Application.Common;
using Inkbound.Domain.Adventure;
using Inkbound.Domain.Randomness;
using Inkbound.Infrastructure.Content;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Application;

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
    // 預設角色 At 12（HpMax 350、Df 10，撐得住）對防禦 3：每下 144/15 = 9.6 → 10；寒狼 60 血，第 6 秒倒下；休息到第 7 秒。
    // 第 7 秒開打的第二場跨過第 10 秒，照樣打完：第 13 秒倒下，休息到第 14 秒。
    [Fact]
    public async Task Advance_SettlesThroughLastStartedFight()
    {
        clock.Advance(TimeSpan.FromSeconds(10));

        var result = await service.AdvanceAsync(AccountId, TestContext.Current.CancellationToken);

        Assert.Equal(16, result.ExperienceGained);
        Assert.Equal(6, result.SpiritStonesGained);
        var stored = await repository.FindByAccountAsync(AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(Start.AddSeconds(14), stored!.LastSettledAt);
        Assert.Equal(StartingExp + 16, stored.Exp);
        Assert.Equal(2006, stored.Money);
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
