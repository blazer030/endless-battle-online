using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Rest;

// 房型：入住期間 Kind 乘上 Multiplier；價格 = 等級 × PricePerLevel × PriceRate
public sealed record InnRoom(string Id, string Name, BuffKind Kind, double Multiplier, int PriceRate);

public sealed record InnDefinition(IReadOnlyList<InnRoom> Rooms, int Seconds, int PricePerLevel);

// 一次入住；每次入住一筆，開打時刻在 [StartsAt, EndsAt) 的場次才有加成
public sealed record Lodging(string RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

// Ended：入住時還沒到期、被截斷到入住時刻的那筆
public sealed record InnBooking(Character Character, Lodging Booked, Lodging? Ended, long Cost);

public static class Inn
{
    public static long Price(InnDefinition inn, InnRoom room, int level) =>
        (long)level * inn.PricePerLevel * room.PriceRate;

    public static IReadOnlyList<string> CanBook(Character character, long price) =>
        character.Money >= price ? [] : ["notEnoughMoney"];

    // 只扣錢、不回滿；還沒到期的那筆在 now 結束（不退錢），新的一筆從 now 開始
    public static InnBooking Book(Character character, InnDefinition inn, InnRoom room, Lodging? active, DateTimeOffset now)
    {
        var cost = Price(inn, room, character.Level);
        var ended = active is not null && active.EndsAt > now ? active with { EndsAt = now } : null;

        return new InnBooking(character with { Money = character.Money - cost }, new Lodging(room.Id, now, now.AddSeconds(inn.Seconds)), ended, cost);
    }

    // 離線補算最多往前 MaxSimulatedSeconds，比這更早結束的住宿用不到
    public static DateTimeOffset PruneBefore(DateTimeOffset now) => now.AddSeconds(-AdventureSimulator.MaxSimulatedSeconds);

    // 房型已不在內容裡 → 視為沒有加成
    public static CharacterBuff? ToBuff(Lodging? lodging, InnDefinition inn)
    {
        var room = lodging is null ? null : inn.Rooms.FirstOrDefault(candidate => candidate.Id == lodging.RoomId);

        return room is null ? null : new CharacterBuff(room.Kind, room.Multiplier, lodging!.StartsAt, lodging.EndsAt);
    }
}
