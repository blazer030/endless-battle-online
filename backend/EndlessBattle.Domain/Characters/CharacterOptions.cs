namespace EndlessBattle.Domain.Characters;

public sealed record CharacterOption(int Id, string Name);

// 建角可選的性別、屬性、出生地；id 從 1 開始連號，也是存進角色的值
public static class CharacterOptions
{
    public static readonly IReadOnlyList<CharacterOption> Sexes = [new(1, "男"), new(2, "女")];

    public static readonly IReadOnlyList<CharacterOption> Elements =
        [new(1, "地"), new(2, "水"), new(3, "火"), new(4, "木"), new(5, "風"), new(6, "毒")];

    public static readonly IReadOnlyList<CharacterOption> Births =
        [new(1, "中央大陸"), new(2, "魔法王國"), new(3, "熱帶雨林"), new(4, "末日王城")];
}
