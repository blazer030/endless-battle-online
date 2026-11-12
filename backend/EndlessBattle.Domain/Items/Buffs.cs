using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Items;

public enum BuffKind
{
    Experience,
    JobExperience,
    Attack,
    Magic,
    Defense,
    MagicDefense,
}

// 密藥：喝下後 Seconds 秒內，指定項目乘上 Multiplier
public sealed record ElixirSpec(BuffKind Kind, double Multiplier, int Seconds);

// 開打時刻落在 [StartsAt, EndsAt) 的場次才生效；每個角色每種至多一筆。ItemId 是最後喝的道具，住宿與舊資料為 null
public sealed record CharacterBuff(BuffKind Kind, double Multiplier, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? ItemId = null)
{
    public bool ActiveAt(DateTimeOffset at) => StartsAt <= at && at < EndsAt;

    // 到期時刻（含）起不再列出
    public bool ExpiredAt(DateTimeOffset at) => EndsAt <= at;
}

public static class Buffs
{
    // 沒有生效的 → 1
    public static double Multiplier(IReadOnlyList<CharacterBuff> buffs, BuffKind kind, DateTimeOffset at) =>
        Multiplier(buffs, kind, at, lodging: null);

    // 密藥與房型加成同種同時生效時相乘
    public static double Multiplier(IReadOnlyList<CharacterBuff> buffs, BuffKind kind, DateTimeOffset at, CharacterBuff? lodging)
    {
        var elixir = buffs.Where(buff => buff.Kind == kind && buff.ActiveAt(at)).Select(buff => buff.Multiplier).DefaultIfEmpty(1).Max();

        return lodging is not null && lodging.Kind == kind && lodging.ActiveAt(at) ? elixir * lodging.Multiplier : elixir;
    }

    // 物攻、魔攻、物防、魔防各乘倍率後以 PHP round 取整
    public static CombatantState ApplyTo(CombatantState player, IReadOnlyList<CharacterBuff> buffs, DateTimeOffset at) =>
        ApplyTo(player, buffs, at, lodging: null);

    public static CombatantState ApplyTo(CombatantState player, IReadOnlyList<CharacterBuff> buffs, DateTimeOffset at, CharacterBuff? lodging) => player with
    {
        Attack = Scale(player.Attack, Multiplier(buffs, BuffKind.Attack, at, lodging)),
        MagicAttack = Scale(player.MagicAttack, Multiplier(buffs, BuffKind.Magic, at, lodging)),
        Defense = Scale(player.Defense, Multiplier(buffs, BuffKind.Defense, at, lodging)),
        MagicDefense = Scale(player.MagicDefense, Multiplier(buffs, BuffKind.MagicDefense, at, lodging)),
    };

    public static CharacterBuff Start(string itemId, ElixirSpec elixir, DateTimeOffset now) =>
        new(elixir.Kind, elixir.Multiplier, now, now.AddSeconds(elixir.Seconds), itemId);

    // 經驗與熟練度用：倍率 1 時原值不動
    public static long Scale(long value, double multiplier) =>
        multiplier == 1 ? value : (long)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);

    private static int Scale(int value, double multiplier) =>
        multiplier == 1 ? value : DerivedStats.RoundLikePhp(value * multiplier);
}
