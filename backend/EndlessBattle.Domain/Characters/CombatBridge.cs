using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Characters;

public static class CombatBridge
{
    public static CombatantState ToCombatant(Character character) => ToCombatant(character, EquipmentBonus.Zero);

    // 能力一律用有效值（裸點 + 裝備）；攻擊屬性就是角色屬性。
    // 卸下加 life／smart／be 的裝備後存檔的 Hp／Sp 可能超過新上限，這裡夾回，戰鬥與顯示都從這裡取
    public static CombatantState ToCombatant(Character character, EquipmentBonus equipment)
    {
        var total = equipment.Effective(character.Base);
        var derived = DerivedStats.From(character.Base, equipment);
        return new CombatantState(
            HealthPoints: Math.Min(character.HealthPoints, derived.MaxHealthPoints),
            MaxHealthPoints: derived.MaxHealthPoints,
            ManaPoints: Math.Min(character.ManaPoints, derived.MaxManaPoints),
            MaxManaPoints: derived.MaxManaPoints,
            Attack: derived.Attack,
            MagicAttack: derived.MagicAttack,
            Defense: derived.Defense,
            MagicDefense: derived.MagicDefense,
            Agility: total.Agility,
            Charisma: total.Charisma,
            Element: character.Element,
            Strength: total.Strength,
            Intelligence: total.Intelligence,
            Vitality: total.Vitality,
            Faith: total.Faith,
            Level: character.Level);
    }
}
