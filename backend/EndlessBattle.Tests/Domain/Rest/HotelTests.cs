using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Rest;

public class HotelTests
{
    // Life 38 → HpMax = round(38×25) + 50 = 1000；Smart 12、Be 10 → SpMax = round(22×3) + 25 = 91
    private static Character Hero(int level, int hp, long money = 100_000, long exp = 500) =>
        TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with
        {
            Level = level,
            HealthPoints = hp,
            ManaPoints = 3,
            Money = money,
            Experience = exp,
            Base = new BaseStats(Strength: 12, Intelligence: 12, Life: 38, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10),
        };

    // Lv 100 以下免費：不扣錢、無懲罰、回滿 HP 1000／SP 91、等待 5 秒
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Hotel_IsFreeAtOrBelowLevel100(int level)
    {
        var hero = Hero(level, hp: 30);

        var result = Hotel.Rest(hero);

        Assert.Equal(hero with { HealthPoints = 1000, ManaPoints = 91 }, result.Character);
        Assert.Equal(0, result.Cost);
        Assert.False(result.ExperiencePenalty);
        Assert.Equal(5.0, result.WaitSeconds);
    }

    // Lv 120、HP 30／1000：1000/10 = 100 > 30 → 係數 1.8；round(10×120×1.8) = 2160 ≥ 下限 1200
    [Fact]
    public void Hotel_ChargesByMissingHp()
    {
        var result = Hotel.Rest(Hero(120, hp: 30));

        Assert.Equal(2160, result.Cost);
        Assert.Equal(100_000 - 2160, result.Character.Money);
        Assert.Equal(1000, result.Character.HealthPoints);
        Assert.Equal(91, result.Character.ManaPoints);
        Assert.Equal(500, result.Character.Experience);
        Assert.False(result.ExperiencePenalty);
        Assert.Equal(5.0, result.WaitSeconds);
    }

    // HP 990／1000：係數 1.4×10/1000 = 0.014；round(1200×0.014 = 16.8) = 17 < 下限 1200 → 1200
    [Fact]
    public void Hotel_UsesMinimumCost()
    {
        var result = Hotel.Rest(Hero(120, hp: 990));

        Assert.Equal(1200, result.Cost);
        Assert.Equal(100_000 - 1200, result.Character.Money);
    }

    // HP 500／1000：係數 1.4×500/1000 = 0.7；round(1200×0.7 = 840) = 840 < 1200 → 1200（缺血比例低於約 0.86 都落在下限）
    // HP 0：死亡係數 2.1；round(1200×2.1) = 2520
    [Fact]
    public void Hotel_ChargesDeathRate()
    {
        var result = Hotel.Rest(Hero(120, hp: 0));

        Assert.Equal(2520, result.Cost);
        Assert.Equal(100_000 - 2520, result.Character.Money);
    }

    // 錢 2159 < 2160：免費，Exp = (long)(500×0.8) = 400，HP／SP 仍回滿；奇數 Exp 999×0.8 = 799.2 → 799（截斷）
    [Fact]
    public void Hotel_TakesExpPenalty_WhenBroke()
    {
        var result = Hotel.Rest(Hero(120, hp: 30, money: 2159));

        Assert.Equal(0, result.Cost);
        Assert.True(result.ExperiencePenalty);
        Assert.Equal(2159, result.Character.Money);
        Assert.Equal(400, result.Character.Experience);
        Assert.Equal(1000, result.Character.HealthPoints);
        Assert.Equal(799, Hotel.Rest(Hero(120, hp: 30, money: 0, exp: 999)).Character.Experience);
    }

    // 死亡等待 15 秒；存活 5 秒（Lv ≤ 100 與付費路徑皆然）
    [Fact]
    public void Hotel_WaitsFifteenSecondsWhenDead()
    {
        Assert.Equal(15.0, Hotel.Rest(Hero(50, hp: 0)).WaitSeconds);
        Assert.Equal(15.0, Hotel.Rest(Hero(120, hp: 0)).WaitSeconds);
        Assert.Equal(5.0, Hotel.Rest(Hero(120, hp: 1)).WaitSeconds);
    }

    // 經驗懲罰四捨五入（WOG 寫回 INT 欄位）：Exp 3 × 0.8 = 2.4 → 2；Exp 2 × 0.8 = 1.6 → 2（截斷會得 1）
    [Fact]
    public void Hotel_RoundsExpPenalty_LikeTheOriginal()
    {
        var result = Hotel.Rest(Hero(120, hp: 30, money: 0, exp: 2));

        Assert.True(result.ExperiencePenalty);
        Assert.Equal(2, result.Character.Experience);
        Assert.Equal(1000, result.Character.HealthPoints);
        Assert.Equal(0, result.Character.Money);
    }

    // 最小重現：Exp 7 × 0.8 = 5.6 → 6（截斷會得 5）；Exp 3 → 2
    [Theory]
    [InlineData(7, 6)]
    [InlineData(3, 2)]
    public void Hotel_ExpPenalty_RoundsToNearest(long exp, long expected)
    {
        Assert.Equal(expected, Hotel.Rest(Hero(120, hp: 30, money: 0, exp: exp)).Character.Experience);
    }

    // HP 100 = HpMax/10：100 > 100 不成立 → 1.4×(1000−100)/1000 = 1.26；round(1200×1.26 = 1512) = 1512
    [Fact]
    public void Hotel_AtTenPercentHp_UsesProportionalRate()
    {
        Assert.Equal(1512, Hotel.Rest(Hero(120, hp: 100)).Cost);
    }

    // HP 120：1.4×880/1000 = 1.232；round(1200×1.232 = 1478.4) = 1478
    [Fact]
    public void Hotel_MidRangeHp_UsesProportionalRate()
    {
        Assert.Equal(1478, Hotel.Rest(Hero(120, hp: 120)).Cost);
    }

    // 錢剛好等於費用 2160：付得起，扣到 0，不罰經驗
    [Fact]
    public void Hotel_PaysWhenMoneyEqualsCost()
    {
        var result = Hotel.Rest(Hero(120, hp: 30, money: 2160));

        Assert.Equal(2160, result.Cost);
        Assert.Equal(0, result.Character.Money);
        Assert.False(result.ExperiencePenalty);
        Assert.Equal(500, result.Character.Experience);
    }

    // Lv 101 開始收費：HP 30 → 1.8；round(10×101×1.8 = 1818) = 1818 ≥ 下限 1010
    [Fact]
    public void Hotel_ChargesFromLevel101()
    {
        var result = Hotel.Rest(Hero(101, hp: 30));

        Assert.Equal(1818, result.Cost);
        Assert.Equal(100_000 - 1818, result.Character.Money);
    }

    // 裝備 life +2、smart +1、魔石 hpMax +30：HpMax round(40×25)+50+30 = 1080；SpMax (13+10)×3+25 = 94
    [Fact]
    public void Hotel_HealsToEffectiveMaximum()
    {
        var bonus = new EquipmentBonus(StatBonus.Zero with { Life = 2, Intelligence = 1, MaxHealthPoints = 30 });

        var result = Hotel.Rest(Hero(1, hp: 30), bonus);

        Assert.Equal((1080, 94), (result.Character.HealthPoints, result.Character.ManaPoints));
    }

    // 規格 inn：Rest cost 表（HpMax 100：Life 2 → round(2×25) + 50 = 100；經驗 1000）
    [Theory]
    [InlineData(100, 0, 0L, 0L, 1000L)]
    [InlineData(101, 0, 5000L, 2121L, 1000L)]
    [InlineData(120, 5, 5000L, 2160L, 1000L)]
    [InlineData(120, 50, 5000L, 1200L, 1000L)]
    [InlineData(120, 50, 100L, 0L, 800L)]
    public void Hotel_CostTable(int level, int hp, long money, long cost, long experience)
    {
        var hero = Hero(level, hp, money, exp: 1000) with { Base = new BaseStats(Strength: 12, Intelligence: 12, Life: 2, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10) };

        var result = Hotel.Rest(hero);

        Assert.Equal(cost, result.Cost);
        Assert.Equal(money - cost, result.Character.Money);
        Assert.Equal(experience, result.Character.Experience);
        Assert.Equal(100, result.Character.HealthPoints);
    }
}
