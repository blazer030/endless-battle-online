using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Battle;

// 戰鬥中一方的狀態；Str／Smart 是有效值（裸點 + 裝備），供戰神之怒與治癒計算
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
    int Str,
    int Smart,
    int Vit = 0,
    int Be = 0,
    int Level = 0)
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
        Str: monster.Str,
        Smart: monster.Smart,
        Vit: monster.Vit,
        Be: monster.Be,
        Level: monster.Level);
}
