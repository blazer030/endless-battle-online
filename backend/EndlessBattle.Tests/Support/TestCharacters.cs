using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Tests.Support;

public static class TestCharacters
{
    // 戰士、配點 2/2/2/2：Base (12,12,12,10,12,10,10) → At 12、Df 10、HpMax 350。
    // Hp 60 而非新角色的 50：高於自動住宿門檻 350 × 0.15 = 52.5，開打前不住宿
    public static Character Default(long accountId, string name, int placeId, int? subPlace, DateTimeOffset lastSettledAt) => new(
        Id: 0,
        AccountId: accountId,
        Name: name,
        Level: 1,
        Exp: 0,
        NextExp: 1000,
        Money: 2000,
        Sex: 1,
        Element: 1,
        Birth: 1,
        JobId: "warrior",
        SpecialAttackName: "必殺",
        Base: new BaseStats(Str: 12, Smart: 12, Life: 12, Vit: 10, Agi: 12, Au: 10, Be: 10),
        Hp: 60,
        Sp: 25,
        PlaceId: placeId,
        SubPlace: subPlace,
        LastSettledAt: lastSettledAt,
        Bank: 0,
        BagBonus: 0,
        DepotBonus: 0,
        Version: 0);
}
