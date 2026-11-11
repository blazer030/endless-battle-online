using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Pets;

public enum PetStat
{
    Attack,
    Magic,
    Defense,
    Spirit,
}

public sealed record PetFood(string Id, string Name, PetStat Stat, int Amount, long Cost, int SatietyMin, int SatietyMax);

// 餵食（wog_act_pet.php）、飢餓（wog_etc_peo.php）與歲數
public static class PetCare
{
    public const int FeedCooldownSeconds = 10, SatietyFull = 100, HungerIntervalSeconds = 900, SecondsPerYear = 864_000;

    private const int IntimacyGainMin = 2, IntimacyGainMax = 6, HungerSatietyMin = 8, HungerSatietyMax = 12;

    // WOG 寫死在程式裡的選單，依選單順序
    public static IReadOnlyList<PetFood> Foods { get; } =
    [
        Small("fruit-vigor-small", "初級活力果實", PetStat.Attack),
        Small("fruit-aroma-small", "初級香氣果實", PetStat.Magic),
        Small("fruit-sweet-small", "初級甜味果實", PetStat.Defense),
        Small("fruit-wild-small", "初級野性果實", PetStat.Spirit),
        Medium("fruit-vigor-medium", "中級活力果實", PetStat.Attack),
        Medium("fruit-aroma-medium", "中級香氣果實", PetStat.Magic),
        Medium("fruit-sweet-medium", "中級甜味果實", PetStat.Defense),
        Medium("fruit-wild-medium", "中級野性果實", PetStat.Spirit),
    ];

    // 依序 tooSoon、full、overOwner、notEnoughMoney，只回第一個成立的；owner 是含裝備的有效能力
    public static IReadOnlyList<string> CanFeed(Pet pet, PetFood food, DerivedStats owner, long money, DateTimeOffset now)
    {
        if (now < NextFeedAt(pet))
        {
            return ["tooSoon"];
        }

        if (pet.Satiety >= SatietyFull)
        {
            return ["full"];
        }

        if (ExceedsOwner(Raise(pet, food), owner))
        {
            return ["overOwner"];
        }

        return money < food.Cost ? ["notEnoughMoney"] : [];
    }

    // 先擲飽腹再擲親密度；飽腹可以超過 100
    public static Pet Feed(Pet pet, PetFood food, DateTimeOffset now, IRandomSource random)
    {
        var satiety = PhpRandom.Between(random, food.SatietyMin, food.SatietyMax);
        var intimacy = PhpRandom.Between(random, IntimacyGainMin, IntimacyGainMax);
        return Raise(pet, food) with
        {
            Satiety = pet.Satiety + satiety,
            Intimacy = pet.Intimacy + intimacy,
            HungerSince = now,
        };
    }

    // 只看攜帶中的寵物；超過 900 秒扣一次（不論離開多久），飽腹與親密度下限 0
    public static Pet Hunger(Pet pet, DateTimeOffset now, IRandomSource random)
    {
        if (!pet.Carried || (now - pet.HungerSince).TotalSeconds <= HungerIntervalSeconds)
        {
            return pet;
        }

        var satietyLoss = PhpRandom.Between(random, HungerSatietyMin, HungerSatietyMax);
        return pet with
        {
            Intimacy = Math.Max(0, pet.Intimacy - 1),
            Satiety = Math.Max(0, pet.Satiety - satietyLoss),
            HungerSince = now,
        };
    }

    // 只顯示，不影響任何數值
    public static int Age(Pet pet, DateTimeOffset now) =>
        DerivedStats.RoundLikePhp((now - pet.BornAt).TotalSeconds / SecondsPerYear);

    public static DateTimeOffset NextFeedAt(Pet pet) => pet.HungerSince.AddSeconds(FeedCooldownSeconds);

    private static bool ExceedsOwner(Pet pet, DerivedStats owner) =>
        pet.Attack > owner.Attack || pet.Magic > owner.MagicAttack || pet.Defense > Math.Max(owner.Defense, owner.MagicDefense);

    private static Pet Raise(Pet pet, PetFood food) => food.Stat switch
    {
        PetStat.Attack => pet with { Attack = pet.Attack + food.Amount },
        PetStat.Magic => pet with { Magic = pet.Magic + food.Amount },
        PetStat.Defense => pet with { Defense = pet.Defense + food.Amount },
        _ => pet with { Spirit = pet.Spirit + food.Amount },
    };

    private static PetFood Small(string id, string name, PetStat stat) => new(id, name, stat, 2, 20, 10, 20);

    private static PetFood Medium(string id, string name, PetStat stat) => new(id, name, stat, 4, 50, 18, 28);
}
