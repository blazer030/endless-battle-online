using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Battle;

// 戰鬥中一方的狀態；SpecialAttackName 只有玩家有，暴擊時顯示
public sealed record CombatantState(
    int Hp,
    int HpMax,
    int Sp,
    int SpMax,
    int At,
    int Mat,
    int Df,
    int Mdf,
    int Agi,
    int Au,
    int Element,
    string? SpecialAttackName)
{
    // 怪物每場都從滿 HP／SP 開始
    public static CombatantState From(MonsterDefinition monster) => new(
        Hp: monster.Hp,
        HpMax: monster.Hp,
        Sp: monster.Sp,
        SpMax: monster.Sp,
        At: monster.At,
        Mat: monster.Mat,
        Df: monster.Df,
        Mdf: monster.Mdf,
        Agi: monster.Agi,
        Au: monster.Au,
        Element: monster.Element,
        SpecialAttackName: null);
}
