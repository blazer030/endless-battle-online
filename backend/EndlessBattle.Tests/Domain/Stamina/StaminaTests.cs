using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Tests.Domain.Stamina;

public class StaminaTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    // 190 秒 / 180 = 1.05 ≥ 1 → 40 + 6 = 46，時間推進到 now
    // 400 秒 / 180 = 2.22 ≥ 2 → 40 + 12 = 52 → 上限 50
    // 100 秒 / 180 = 0.56 < 1 → 不變，LastRegenAt 保持原值（讓後續呼叫能累積到 180 秒）
    // act 30、5000 秒 / 180 = 27.8 → 單次最多 +12（照 WOG）→ 42
    // 時間倒退 → 視為 0，不變
    // LastRegenAt 為 null → 視為 now，不回復，之後以 now 起算
    [Fact]
    public void Stamina_RegeneratesSixPerThreeMinutes_CappedAtFifty()
    {
        var s = new StaminaState(40, T0);

        Assert.Equal(new StaminaState(46, T0.AddSeconds(190)), StaminaRules.Regenerate(s, T0.AddSeconds(190)));
        Assert.Equal(new StaminaState(50, T0.AddSeconds(400)), StaminaRules.Regenerate(s, T0.AddSeconds(400)));
        Assert.Equal(new StaminaState(40, T0), StaminaRules.Regenerate(s, T0.AddSeconds(100)));
        Assert.Equal(new StaminaState(42, T0.AddSeconds(5000)), StaminaRules.Regenerate(new StaminaState(30, T0), T0.AddSeconds(5000)));
        Assert.Equal(new StaminaState(40, T0), StaminaRules.Regenerate(s, T0.AddSeconds(-60)));
        Assert.Equal(new StaminaState(40, T0.AddSeconds(500)), StaminaRules.Regenerate(new StaminaState(40, null), T0.AddSeconds(500)));
    }

    // act 1 → 0：fDown 1、12 秒；act 0 → −1：fDown 0.5、12 秒（存回 0）；act 5 → 4：fDown 1、2 秒
    [Fact]
    public void Stamina_DepletedHalvesRewardsAndSlowsFights()
    {
        var last = T0.AddSeconds(7);

        Assert.Equal((new StaminaState(0, last), 1.0, 12.0), StaminaRules.Consume(new StaminaState(1, last)));
        Assert.Equal((new StaminaState(0, last), 0.5, 12.0), StaminaRules.Consume(new StaminaState(0, last)));
        Assert.Equal((new StaminaState(4, last), 1.0, 2.0), StaminaRules.Consume(new StaminaState(5, last)));
    }

    // 存回的行動力不低於 0：連續耗盡時一直停在 0，回復的 +6 才有效；就算傳入負值也一樣
    [Fact]
    public void Stamina_StoredValueNeverDropsBelowZero()
    {
        var last = T0.AddSeconds(7);

        var (afterDepleted, _, _) = StaminaRules.Consume(StaminaRules.Consume(new StaminaState(0, last)).State);
        Assert.Equal(new StaminaState(0, last), afterDepleted);
        Assert.Equal((new StaminaState(0, last), 0.5, 12.0), StaminaRules.Consume(new StaminaState(-5, last)));
    }

    // 恰好 180 秒 → periods = 1 → +6；恰好 360 秒 → periods = 2 → +12
    [Fact]
    public void Stamina_RegeneratesAtExactBoundaries()
    {
        var s = new StaminaState(20, T0);

        Assert.Equal(new StaminaState(26, T0.AddSeconds(180)), StaminaRules.Regenerate(s, T0.AddSeconds(180)));
        Assert.Equal(new StaminaState(32, T0.AddSeconds(360)), StaminaRules.Regenerate(s, T0.AddSeconds(360)));
        Assert.Equal(new StaminaState(20, T0), StaminaRules.Regenerate(s, T0.AddSeconds(179)));
    }
}
