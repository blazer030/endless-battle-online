using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Stamina;

public sealed record StaminaState(int ActNum, DateTimeOffset? LastRegenAt);

public static class StaminaRules
{
    public const int Max = 50;
    public const int RegenAmount = 6;
    public const int RegenSeconds = 180;

    // 照 WOG：滿 180 秒 +6、滿 360 秒再 +6（單次最多 +12），上限 50。
    // 只有回復發生時才推進 LastRegenAt（WOG 亦然），否則頻繁呼叫永遠累積不到 180 秒。
    public static StaminaState Regenerate(StaminaState s, DateTimeOffset now)
    {
        var last = s.LastRegenAt ?? now;
        var periods = Math.Max(0.0, (now - last).TotalSeconds) / RegenSeconds;
        if (periods < 1)
        {
            return s with { LastRegenAt = last };
        }

        var act = s.ActNum + RegenAmount;
        if (periods >= 2)
        {
            act += RegenAmount;
        }

        return new StaminaState(Math.Min(act, Max), now);
    }

    // 照 WOG 不設下限：行動力可降為負值，由呼叫端決定如何處理
    public static (StaminaState State, double FDown, double IntervalSeconds) Consume(StaminaState s)
    {
        var act = s.ActNum - 1;
        var next = s with { ActNum = act };
        return act switch
        {
            < 0 => (next, 0.5, BattleTiming.FightIntervalWhenDepletedSeconds),
            0 => (next, 1.0, BattleTiming.FightIntervalWhenDepletedSeconds),
            _ => (next, 1.0, BattleTiming.FightIntervalSeconds),
        };
    }
}
