using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Domain.Items;

public class BuffsTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static CharacterBuff Buff(BuffKind kind, double multiplier) => new(kind, multiplier, StartsAt, StartsAt.AddHours(1));

    // 窗口是 [StartsAt, EndsAt)：開始那一刻生效、到期那一刻不生效；別種一律 1
    [Fact]
    public void Multiplier_InsideWindowOnly()
    {
        IReadOnlyList<CharacterBuff> buffs = [Buff(BuffKind.Experience, 1.5)];

        Assert.Equal(1.5, Buffs.Multiplier(buffs, BuffKind.Experience, StartsAt));
        Assert.Equal(1.5, Buffs.Multiplier(buffs, BuffKind.Experience, StartsAt.AddMinutes(59)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.Experience, StartsAt.AddHours(1)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.Experience, StartsAt.AddTicks(-1)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.JobExperience, StartsAt));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Experience, StartsAt));
    }

    // 到期那一刻起算過期；還沒開始的不算過期
    [Fact]
    public void ExpiredAt_FromEndsAtOn()
    {
        var buff = Buff(BuffKind.Experience, 1.5);

        Assert.False(buff.ExpiredAt(StartsAt.AddTicks(-1)));
        Assert.False(buff.ExpiredAt(StartsAt.AddHours(1).AddTicks(-1)));
        Assert.True(buff.ExpiredAt(StartsAt.AddHours(1)));
    }

    // at 25 × 1.2 = 30；df 11 × 1.2 = 13.2 → 13；沒有密藥的 mat／mdf 不變
    [Fact]
    public void ApplyTo_MultipliesAttackWithPhpRounding()
    {
        var player = new CombatantState(
            HealthPoints: 60, MaxHealthPoints: 350, ManaPoints: 25, MaxManaPoints: 91, Attack: 25, MagicAttack: 17, Defense: 11, MagicDefense: 9, Agility: 12, Charisma: 10, Element: 1,
            Strength: 12, Intelligence: 12);

        var buffed = Buffs.ApplyTo(player, [Buff(BuffKind.Attack, 1.2), Buff(BuffKind.Defense, 1.2)], StartsAt);

        Assert.Equal(player with { Attack = 30, Defense = 13 }, buffed);
        Assert.Equal(player, Buffs.ApplyTo(player, [Buff(BuffKind.Attack, 1.2)], StartsAt.AddHours(1)));
    }

    // 四項戰鬥數值各不相同，乘 1.2 後也各不相同：任兩種對應互換都會被抓到
    private static readonly CombatantState Player = new(
        HealthPoints: 60, MaxHealthPoints: 350, ManaPoints: 25, MaxManaPoints: 91, Attack: 25, MagicAttack: 40, Defense: 11, MagicDefense: 15, Agility: 12, Charisma: 10, Element: 1,
        Strength: 12, Intelligence: 12);

    // mat 40 × 1.2 = 48，其他不變
    [Fact]
    public void ApplyTo_MultipliesMagicOnly() =>
        Assert.Equal(Player with { MagicAttack = 48 }, Buffs.ApplyTo(Player, [Buff(BuffKind.Magic, 1.2)], StartsAt));

    // mdf 15 × 1.2 = 18，其他不變
    [Fact]
    public void ApplyTo_MultipliesMagicDefenseOnly() =>
        Assert.Equal(Player with { MagicDefense = 18 }, Buffs.ApplyTo(Player, [Buff(BuffKind.MagicDefense, 1.2)], StartsAt));

    // 每一種密藥只動自己那一項：at 25 → 30、mat 40 → 48、df 11 → 13、mdf 15 → 18；經驗與熟練密藥不動戰鬥數值
    [Theory]
    [InlineData(BuffKind.Attack, 30, 40, 11, 15)]
    [InlineData(BuffKind.Magic, 25, 48, 11, 15)]
    [InlineData(BuffKind.Defense, 25, 40, 13, 15)]
    [InlineData(BuffKind.MagicDefense, 25, 40, 11, 18)]
    [InlineData(BuffKind.Experience, 25, 40, 11, 15)]
    [InlineData(BuffKind.JobExperience, 25, 40, 11, 15)]
    public void ApplyTo_MultipliesOnlyItsOwnStat(BuffKind kind, int at, int mat, int df, int mdf) =>
        Assert.Equal(Player with { Attack = at, MagicAttack = mat, Defense = df, MagicDefense = mdf }, Buffs.ApplyTo(Player, [Buff(kind, 1.2)], StartsAt));

    // 規格 inn：Settling after switching rooms——鬥士通鋪（物攻 ×1.05）06:00–10:00、學徒閣樓（經驗 ×1.10）10:00–18:00
    [Theory]
    [InlineData(5, 30, 1.0, 1.0)]
    [InlineData(9, 0, 1.05, 1.0)]
    [InlineData(10, 0, 1.0, 1.10)]
    [InlineData(11, 0, 1.0, 1.10)]
    public void Multiplier_PicksTheStayContainingTheMoment(int hour, int minute, double attack, double experience)
    {
        var day = new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero);
        IReadOnlyList<CharacterBuff> stays =
        [
            new(BuffKind.Attack, 1.05, day.AddHours(6), day.AddHours(10)),
            new(BuffKind.Experience, 1.10, day.AddHours(10), day.AddHours(18)),
        ];
        var at = day.AddHours(hour).AddMinutes(minute);

        Assert.Equal(attack, Buffs.Multiplier([], BuffKind.Attack, at, stays));
        Assert.Equal(experience, Buffs.Multiplier([], BuffKind.Experience, at, stays));
    }
}
