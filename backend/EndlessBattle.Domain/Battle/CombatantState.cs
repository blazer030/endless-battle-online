using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Battle;

// 戰鬥中一方的狀態；Strength／Intelligence 是有效值（裸點 + 裝備），供戰神之怒與治癒計算
public sealed record CombatantState(
    int HealthPoints,
    int MaxHealthPoints,
    int ManaPoints,
    int MaxManaPoints,
    int Attack,
    int MagicAttack,
    int Defense,
    int MagicDefense,
    int Agility,
    int Charisma,
    int Element,
    int Strength,
    int Intelligence,
    int Vitality = 0,
    int Faith = 0,
    int Level = 0)
{
    // 怪物每場都從滿 HP／SP 開始
    public static CombatantState From(MonsterDefinition monster) => new(
        HealthPoints: monster.HealthPoints,
        MaxHealthPoints: monster.HealthPoints,
        ManaPoints: monster.ManaPoints,
        MaxManaPoints: monster.ManaPoints,
        Attack: monster.Attack,
        MagicAttack: monster.MagicAttack,
        Defense: monster.Defense,
        MagicDefense: monster.MagicDefense,
        Agility: monster.Agility,
        Charisma: monster.Charisma,
        Element: monster.Element,
        Strength: monster.Strength,
        Intelligence: monster.Intelligence,
        Vitality: monster.Vitality,
        Faith: monster.Faith,
        Level: monster.Level);
}
