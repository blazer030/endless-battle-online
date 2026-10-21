using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Battle;
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

    // 手算（全部擲骰 0.0 → rand 皆為 1、NextInt 皆為 0）：妖怪一律抽到 index 0 的測試雪精（60 血、df 6、agi 9、水）。
    // 預設角色 At 12、agi 12、地屬性：agi 較高 1 ≤ 70 先攻；必中；比值 12/9 → 暴擊機率 13.3（1 < 13.3）、倍率 1.6 → A = 19.2；
    // 地剋水 ×1.2：(19.2·3.2 − 6)·1.2 = 66.528，rand(1,1) = 1 → round(67.528) = 68 ≥ 60，第 1 回合擊倒。
    // 每場 0.8 秒 + 場間 2 秒 = 2.8 秒；20 秒內在 0、2.8、…、19.6 開打 8 場，結算到 22.4 秒。
    // 計畫 4 Task 5 加入獎勵前，經驗與金錢不變
    [Fact]
    public async Task Advance_SettlesThroughLastStartedFight()
    {
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, TestContext.Current.CancellationToken);

        Assert.Equal(8, result.Events.OfType<EncounterEvent>().Count());
        Assert.All(result.Events.OfType<EncounterEvent>(), encounter => Assert.Equal("test-sprite", encounter.MonsterId));
        Assert.Equal(0, result.ExperienceGained);
        Assert.Equal(0, result.SpiritStonesGained);
        var stored = await repository.FindByAccountAsync(AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(Start.AddSeconds(22.4), stored!.LastSettledAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(StartingExp, stored.Exp);
        Assert.Equal(2000, stored.Money);
    }

    [Fact]
    public async Task Advance_SettlesToNow_WhenCapped()
    {
        clock.Advance(TimeSpan.FromHours(1));

        var result = await service.AdvanceAsync(AccountId, TestContext.Current.CancellationToken);

        var stored = await repository.FindByAccountAsync(AccountId, TestContext.Current.CancellationToken);
        Assert.Equal(clock.UtcNow, stored!.LastSettledAt);
        Assert.All(result.Events, battleEvent => Assert.True(
            battleEvent.AtSeconds <= AdventureService.MaxSimulatedSeconds + WogBattle.MaxRounds * BattleTiming.SecondsPerRound));
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }
}
