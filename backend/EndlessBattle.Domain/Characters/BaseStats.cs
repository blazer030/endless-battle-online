namespace EndlessBattle.Domain.Characters;

// WOG 七項能力：力量、智力、生命、體質、敏捷、魅力、信仰
public enum BaseStat
{
    Strength,
    Intelligence,
    Life,
    Vitality,
    Agility,
    Charisma,
    Faith,
}

public sealed record BaseStats(int Strength, int Intelligence, int Life, int Vitality, int Agility, int Charisma, int Faith)
{
    public int Of(BaseStat stat) => stat switch
    {
        BaseStat.Strength => Strength,
        BaseStat.Intelligence => Intelligence,
        BaseStat.Life => Life,
        BaseStat.Vitality => Vitality,
        BaseStat.Agility => Agility,
        BaseStat.Charisma => Charisma,
        BaseStat.Faith => Faith,
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "未知的能力"),
    };
}

// 對外的能力代碼（原因代碼 statTooLow:<代碼>、轉職需求），集中在這裡產生
public static class BaseStatCode
{
    public static string Of(BaseStat stat) => stat switch
    {
        BaseStat.Strength => "strength",
        BaseStat.Intelligence => "intelligence",
        BaseStat.Life => "life",
        BaseStat.Vitality => "vitality",
        BaseStat.Agility => "agility",
        BaseStat.Charisma => "charisma",
        BaseStat.Faith => "faith",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, "未知的能力"),
    };

    public static string StatTooLow(BaseStat stat) => $"statTooLow:{Of(stat)}";
}
