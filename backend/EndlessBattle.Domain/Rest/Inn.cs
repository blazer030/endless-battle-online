using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Rest;

// 房型：入住期間 Kind 乘上 Multiplier；價格 = 等級 × PricePerLevel × PriceRate
public sealed record InnRoom(string Id, string Name, BuffKind Kind, double Multiplier, int PriceRate);

public sealed record InnDefinition(IReadOnlyList<InnRoom> Rooms, int Seconds, int PricePerLevel);

// 入住中的房型；每個角色至多一筆，新的整筆取代舊的
public sealed record Lodging(string RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public sealed record InnBooking(Character Character, Lodging Lodging, long Cost);

public static class Inn
{
    public static long Price(InnDefinition inn, InnRoom room, int level) =>
        (long)level * inn.PricePerLevel * room.PriceRate;

    public static IReadOnlyList<string> CanBook(Character character, long price) =>
        character.Money >= price ? [] : ["notEnoughMoney"];

    // 只看 now，不管之前住哪間；HP、SP 回滿到含裝備的上限
    public static InnBooking Book(Character character, EquipmentBonus equipment, InnDefinition inn, InnRoom room, DateTimeOffset now)
    {
        var cost = Price(inn, room, character.Level);
        var stats = DerivedStats.From(character.Base, equipment);
        var booked = character with { Money = character.Money - cost, Hp = stats.MaxHealthPoints, Sp = stats.MaxManaPoints };

        return new InnBooking(booked, new Lodging(room.Id, now, now.AddSeconds(inn.Seconds)), cost);
    }

    // 房型已不在內容裡 → 視為沒有加成
    public static CharacterBuff? ToBuff(Lodging? lodging, InnDefinition inn)
    {
        var room = lodging is null ? null : inn.Rooms.FirstOrDefault(candidate => candidate.Id == lodging.RoomId);

        return room is null ? null : new CharacterBuff(room.Kind, room.Multiplier, lodging!.StartsAt, lodging.EndsAt);
    }
}
