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
        IReadOnlyList<CharacterBuff> buffs = [Buff(BuffKind.Exp, 1.5)];

        Assert.Equal(1.5, Buffs.Multiplier(buffs, BuffKind.Exp, StartsAt));
        Assert.Equal(1.5, Buffs.Multiplier(buffs, BuffKind.Exp, StartsAt.AddMinutes(59)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.Exp, StartsAt.AddHours(1)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.Exp, StartsAt.AddTicks(-1)));
        Assert.Equal(1, Buffs.Multiplier(buffs, BuffKind.JobExp, StartsAt));
        Assert.Equal(1, Buffs.Multiplier([], BuffKind.Exp, StartsAt));
    }

    // at 25 × 1.2 = 30；df 11 × 1.2 = 13.2 → 13；沒有密藥的 mat／mdf 不變
    [Fact]
    public void ApplyTo_MultipliesAttackWithPhpRounding()
    {
        var player = new CombatantState(
            Hp: 60, HpMax: 350, Sp: 25, SpMax: 91, At: 25, Mat: 17, Df: 11, Mdf: 9, Agi: 12, Au: 10, Element: 1,
            SpecialAttackName: "必殺", Str: 12, Smart: 12);

        var buffed = Buffs.ApplyTo(player, [Buff(BuffKind.Attack, 1.2), Buff(BuffKind.Defense, 1.2)], StartsAt);

        Assert.Equal(player with { At = 30, Df = 13 }, buffed);
        Assert.Equal(player, Buffs.ApplyTo(player, [Buff(BuffKind.Attack, 1.2)], StartsAt.AddHours(1)));
    }
}
