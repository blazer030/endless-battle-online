using System.Globalization;
using System.Text;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Missions;

// 擲骰後的實際目標與報酬；一般任務就是定義本身
public sealed record MissionObjective(string MissionId, KillTarget? Kill, IReadOnlyList<ItemQuantity> Deliver, MissionReward Reward, long Exp);

public static class DailyMissions
{
    // Asia/Taipei，無夏令時間
    public static readonly TimeSpan ResetZoneOffset = TimeSpan.FromHours(8);
    public static readonly TimeSpan ResetTimeOfDay = TimeSpan.FromHours(9);

    private const uint FnvOffset = 0x811C9DC5;
    private const uint FnvPrime = 0x01000193;

    public static DateOnly PeriodOf(DateTimeOffset now) =>
        DateOnly.FromDateTime((now.ToOffset(ResetZoneOffset) - ResetTimeOfDay).Date);

    // 該日台北 09:00 的 UTC 時刻
    public static DateTimeOffset PeriodStart(DateOnly period) =>
        new DateTimeOffset(period.ToDateTime(TimeOnly.FromTimeSpan(ResetTimeOfDay)), ResetZoneOffset).ToUniversalTime();

    public static DateTimeOffset NextReset(DateTimeOffset now) => PeriodStart(PeriodOf(now).AddDays(1));

    public static bool IsExpired(MissionRecord record, DateTimeOffset now) =>
        record.DailyPeriod is { } period && period != PeriodOf(now);

    // FNV-1a 32 位元，對 UTF-8 "{characterId}|{missionId}|{yyyy-MM-dd}"
    public static int Seed(long characterId, string missionId, DateOnly period)
    {
        var text = string.Create(CultureInfo.InvariantCulture, $"{characterId}|{missionId}|{period:yyyy-MM-dd}");
        var hash = FnvOffset;
        foreach (var value in Encoding.UTF8.GetBytes(text))
        {
            hash = unchecked((hash ^ value) * FnvPrime);
        }

        return unchecked((int)hash);
    }

    // 一般任務忽略 period；每日任務先擲目標、再擲報酬
    public static MissionObjective Resolve(MissionDefinition mission, long characterId, DateOnly? period)
    {
        if (!mission.Daily)
        {
            return new MissionObjective(mission.Id, mission.Kill, mission.Deliver, mission.Rewards, mission.Exp);
        }

        if (period is not { } day)
        {
            throw new ArgumentException("每日任務需要週期", nameof(period));
        }

        var random = new SeededRandom(Seed(characterId, mission.Id, day));
        var kill = mission.DailyKills[random.NextInt(mission.DailyKills.Count)];
        var reward = mission.DailyRewards[random.NextInt(mission.DailyRewards.Count)];
        return new MissionObjective(mission.Id, kill, [], reward, mission.Exp);
    }
}
