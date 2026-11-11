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
        Hp: monster.HealthPoints,
        HpMax: monster.HealthPoints,
        Sp: monster.ManaPoints,
        SpMax: monster.ManaPoints,
        At: monster.Attack,
        Mat: monster.MagicAttack,
        Df: monster.Defense,
        Mdf: monster.MagicDefense,
        Agi: monster.Agility,
        Au: monster.Charisma,
        Element: monster.Element,
        Str: monster.Strength,
        Smart: monster.Intelligence,
        Vit: monster.Vitality,
        Be: monster.Faith,
        Level: monster.Level);
}
