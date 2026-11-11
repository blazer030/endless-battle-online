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
        Experience: 0,
        NextLevelExperience: 1000,
        Money: 2000,
        Sex: 1,
        Element: 1,
        Birth: 1,
        JobId: "warrior",
        Base: new BaseStats(Strength: 12, Intelligence: 12, Life: 12, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10),
        HealthPoints: 60,
        ManaPoints: 25,
        PlaceId: placeId,
        SubPlace: subPlace,
        LastSettledAt: lastSettledAt,
        Bank: 0,
        BagBonus: 0,
        DepotBonus: 0,
        Version: 0);
}
