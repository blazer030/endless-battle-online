using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Tests.Domain.Missions;

public class DailyMissionsTests
{
    private static readonly DateOnly October22 = new(2026, 10, 22);

    private static readonly MissionDefinition Daily = new(
        "test-daily", "測試公會", "測試每日", "每日討伐。", "明天再來。", 1, null, true, null, [], new MissionRequirements(),
        new MissionReward(0, []),
        [new KillTarget("test-sprite", 2), new KillTarget("test-wolf", 2)],
        [new MissionReward(100, []), new MissionReward(0, [new ItemQuantity("test-supply", 2)])]);

    private static readonly MissionDefinition Delivery = new(
        "test-delivery", "測試鐵匠", "測試交付", "帶礦石來。", "收下了。", 4, 1, false, new KillTarget("test-sprite", 3),
        [new ItemQuantity("test-ore", 3)], new MissionRequirements(),
        new MissionReward(50, [new ItemQuantity("safe-refine-stone", 1)]), [], []);

    private static DateTimeOffset Utc(int day, int hour, int minute = 0, int second = 0) =>
        new(2026, 10, day, hour, minute, second, TimeSpan.Zero);

    private static MissionRecord Record(DateOnly? period) =>
        new("test-daily", MissionState.Active, 0, period, Utc(21, 2), null);

    [Fact]
    public void Period_SwitchesAtNineTaipei()
    {
        Assert.Equal(new DateOnly(2026, 10, 21), DailyMissions.PeriodOf(Utc(22, 0, 59, 59)));
        Assert.Equal(October22, DailyMissions.PeriodOf(Utc(22, 1)));
        Assert.Equal(Utc(22, 1), DailyMissions.NextReset(Utc(22, 0, 59, 59)));
        Assert.Equal(Utc(22, 1), DailyMissions.PeriodStart(October22));
    }

    [Fact]
    public void IsExpired_OnlyForPastPeriods()
    {
        var yesterday = Record(new DateOnly(2026, 10, 21));

        Assert.False(DailyMissions.IsExpired(yesterday, Utc(22, 0, 30)));
        Assert.True(DailyMissions.IsExpired(yesterday, Utc(22, 1)));
        Assert.False(DailyMissions.IsExpired(Record(null), Utc(30, 1)));
    }

    [Fact]
    public void Seed_IsFnv1a()
    {
        Assert.Equal(-1404635945, DailyMissions.Seed(1, "test-daily", October22));
        Assert.Equal(1996040880, DailyMissions.Seed(42, "daily-hunt-novice", October22));
    }

    [Fact]
    public void Resolve_IsDeterministic()
    {
        Assert.Equivalent(DailyMissions.Resolve(Daily, 7, October22), DailyMissions.Resolve(Daily, 7, October22));

        var kills = Enumerable.Range(0, 30)
            .Select(offset => DailyMissions.Resolve(Daily, 7, new DateOnly(2026, 10, 1).AddDays(offset)).Kill)
            .Distinct()
            .ToList();
        Assert.True(kills.Count >= 2);

        var objective = DailyMissions.Resolve(Delivery, 7, October22);
        Assert.Equal("test-delivery", objective.MissionId);
        Assert.Equal(Delivery.Kill, objective.Kill);
        Assert.Equal(Delivery.Deliver, objective.Deliver);
        Assert.Equal(Delivery.Rewards, objective.Reward);
        Assert.Equal(4 * 190, objective.Exp);
    }

    [Fact]
    public void Period_CrossesMonthAndYear()
    {
        Assert.Equal(new DateOnly(2026, 12, 31), DailyMissions.PeriodOf(new DateTimeOffset(2026, 12, 31, 16, 30, 0, TimeSpan.Zero)));
        Assert.Equal(new DateOnly(2026, 12, 31), DailyMissions.PeriodOf(new DateTimeOffset(2027, 1, 1, 0, 59, 0, TimeSpan.Zero)));
        Assert.Equal(
            new DateTimeOffset(2027, 1, 1, 1, 0, 0, TimeSpan.Zero),
            DailyMissions.NextReset(new DateTimeOffset(2026, 12, 31, 16, 30, 0, TimeSpan.Zero)));
    }

    // 時鐘倒退等情況下，未來週期的紀錄也不屬於現在週期
    [Fact]
    public void IsExpired_TreatsAnyOtherPeriodAsExpired()
    {
        Assert.True(DailyMissions.IsExpired(Record(new DateOnly(2026, 10, 23)), Utc(22, 3)));
        Assert.False(DailyMissions.IsExpired(Record(October22), Utc(22, 3)));
    }

    [Fact]
    public void Resolve_RollsTargetThenReward()
    {
        KillTarget[] kills = [new("test-sprite", 2), new("test-wolf", 2), new("test-sprite-chief", 1)];
        MissionReward[] rewards = [new(100, []), new(0, [new ItemQuantity("test-supply", 2)])];
        var daily = Daily with { DailyKills = kills, DailyRewards = rewards };
        const long characterId = 3;

        var random = new SeededRandom(DailyMissions.Seed(characterId, "test-daily", October22));
        var killIndex = random.NextInt(kills.Length);
        var rewardIndex = random.NextInt(rewards.Length);
        var swapped = new SeededRandom(DailyMissions.Seed(characterId, "test-daily", October22));
        var swappedRewardIndex = swapped.NextInt(rewards.Length);
        var swappedKillIndex = swapped.NextInt(kills.Length);

        // 前提：這個種子能分辨「對調擲骰順序」與「報酬固定取第一項」
        Assert.NotEqual(0, rewardIndex);
        Assert.NotEqual((killIndex, rewardIndex), (swappedKillIndex, swappedRewardIndex));

        var objective = DailyMissions.Resolve(daily, characterId, October22);

        Assert.Equal(kills[killIndex], objective.Kill);
        Assert.Equal(rewards[rewardIndex], objective.Reward);
    }
}
