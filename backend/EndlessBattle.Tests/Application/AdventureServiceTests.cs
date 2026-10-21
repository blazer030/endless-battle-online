using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Stamina;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// 擲骰一律 0.0：遇敵擲骰 0 → 只有 Meet 0 的測試雪精（Lv3、60 血、at 14、mat 8、df 6、agi 9、水、jobExp 12）。
// 預設角色 At 12、agi 12、地屬性：agi 較高 → 1 ≤ 70 先攻；必中；比值 12/9 → 暴擊機率 13.3（1 < 13.3）、倍率 1.6 → A = 19.2；
// 地剋水 ×1.2：(19.2·3.2 − 6)·1.2 = 66.528，rand(1,1) = 1 → round(67.528) = 68 ≥ 60，第 1 回合擊倒，雪精不出手。
// 每場 0.8 秒 + 場間 2 秒 = 2.8 秒。獎勵（玩家 Lv1）：經驗 base 11 → rand(11, 14) = 11；×1.3 = 14.3；3 × 14.3 × 3 × 0.18 = 23.166；
// ×2.5 = 57.915；×2 = 115.83 → 116。金錢 rand(1.2, 3) = 1 × rand(22/3, 60/22×0.4) 對調為 rand(1, 7) = 1 → round(0.4) = 0。熟練度 12
public class AdventureServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository repository;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly FakeClock clock = new(Start);
    private readonly AdventureService service;

    public AdventureServiceTests()
    {
        repository = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        service = new AdventureService(repository, proficiencies, Content, clock, new ScriptedRandomFactory());
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", "test-stage-1", Start);
        return repository.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    // 20 秒內在 0、2.8、…、19.6 開打 8 場，結算到 22.4 秒。100 + 8 × 116 = 1028 > 1000 → 第 8 場升 Lv2，剩 28
    [Fact]
    public async Task Advance_SettlesThroughLastStartedFight()
    {
        await AddCharacterAsync(character => character with { Exp = 100 });
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(8, result.Events.OfType<EncounterEvent>().Count());
        Assert.All(result.Events.OfType<EncounterEvent>(), encounter => Assert.Equal("test-sprite", encounter.MonsterId));
        Assert.Equal(new AdvanceSummary(Fights: 8, Wins: 8, Losses: 0, Timeouts: 0, Exp: 928, Money: 0, JobExp: 96, LevelsGained: 1), result.Summary);
        Assert.True(result.EventsCoverAll);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(Start.AddSeconds(22.4), stored!.LastSettledAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(28, stored.Exp);
        Assert.Equal(result.Character, stored);
        Assert.Equal([new JobProficiency("warrior", 96, 96)], result.JobProficiency);
    }

    // HP 0 → 先住宿 15 秒回滿 350／91；15 秒那場擊倒雪精：999 + 116 > 1000 → Lv2，成長取下限 (2,0,1,1,1,0,0)。
    // 行動力 50 − 1 = 49，回復時間從第一次結算的 Start 起算
    [Fact]
    public async Task Advance_PersistsHpAndLevel()
    {
        await AddCharacterAsync(character => character with { Hp = 0, Sp = 0, Exp = 999 });
        clock.Advance(TimeSpan.FromSeconds(16));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.IsType<RestEvent>(result.Events[0]);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(350, stored!.Hp);
        Assert.Equal(91, stored.Sp);
        Assert.Equal(2, stored.Level);
        Assert.Equal(115, stored.Exp);
        Assert.Equal(2184, stored.NextExp);
        Assert.Equal(new BaseStats(14, 12, 13, 11, 13, 10, 10), stored.Base);
        Assert.Equal(49, stored.ActNum);
        Assert.Equal(Start, stored.ActNumTime);
        Assert.Equal(1, stored.Version);
    }

    // 離線 30 小時：只模擬最後 24 小時，結果應與直接從 now − 24h 模擬到 now 相同
    [Fact]
    public async Task Advance_CapsAtTwentyFourHours()
    {
        var character = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromHours(30));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        var from = clock.UtcNow.AddHours(-24);
        var expected = AdventureSimulator.Run(
            character, Content.GetJob("warrior"), Content.GetStage("test-stage-1"), Content.GetMonster,
            new StaminaState(character.ActNum, character.ActNumTime), from, clock.UtcNow, new ScriptedRandom(0.0));
        Assert.Equal(expected.Summary, result.Summary);
        Assert.True(result.Summary.Fights > 1000);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(from.AddSeconds(expected.ConsumedSeconds), stored!.LastSettledAt);
        Assert.InRange(stored.LastSettledAt, clock.UtcNow, clock.UtcNow.AddSeconds(WogBattle.MaxRounds * BattleTiming.SecondsPerRound + BattleTiming.FightIntervalWhenDepletedSeconds));
        Assert.All(result.Events, battleEvent => Assert.True(battleEvent.AtSeconds >= AdventureSimulator.MaxSimulatedSeconds - AdventureSimulator.CatchUpWindowSeconds));
    }

    [Fact]
    public async Task Advance_WritesProficiencyAfterCharacterUpdate()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(5));

        await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(["update", "proficiency:warrior"], log);
    }

    // 版本衝突時角色沒有寫入，熟練度也不能寫
    [Fact]
    public async Task Advance_WritesNoProficiency_WhenCharacterUpdateConflicts()
    {
        await AddCharacterAsync();
        repository.FailNextUpdate = true;
        clock.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<ConflictException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.DoesNotContain(log, entry => entry.StartsWith("proficiency", StringComparison.Ordinal));
        Assert.Empty(await proficiencies.ListAsync(1, CancellationToken));
    }

    [Fact]
    public async Task Advance_ReturnsNothingAndWritesNothing_WhenClockIsBehind()
    {
        var character = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(-5));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Empty(result.Events);
        Assert.Equal(AdvanceSummary.Empty, result.Summary);
        Assert.Equal(character, result.Character);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Advance_Throws_WhenNoCharacter()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.AdvanceAsync(AccountId, CancellationToken));
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }

    // 記錄更新順序；FailNextUpdate 模擬別的請求先寫入造成的版本衝突
    private sealed class LoggingCharacterRepository(List<string> log) : ICharacterRepository
    {
        private readonly InMemoryCharacterRepository inner = new();

        public bool FailNextUpdate { get; set; }

        public Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken) =>
            inner.FindByAccountAsync(accountId, cancellationToken);

        public Task<Character> AddAsync(Character character, CancellationToken cancellationToken) =>
            inner.AddAsync(character, cancellationToken);

        public Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken)
        {
            if (FailNextUpdate)
            {
                FailNextUpdate = false;
                throw new ConflictException("版本不符");
            }

            log.Add("update");
            return inner.UpdateAsync(character, cancellationToken);
        }
    }
}
