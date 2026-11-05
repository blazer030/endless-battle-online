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
            Hp: Math.Min(character.Hp, derived.HpMax),
            HpMax: derived.HpMax,
            Sp: Math.Min(character.Sp, derived.SpMax),
            SpMax: derived.SpMax,
            At: derived.At,
            Mat: derived.Mat,
            Df: derived.Df,
            Mdf: derived.Mdf,
            Agi: total.Agi,
            Au: total.Au,
            Element: character.Element,
            SpecialAttackName: character.SpecialAttackName,
            Str: total.Str,
            Smart: total.Smart,
            Vit: total.Vit,
            Be: total.Be,
            Level: character.Level);
    }
}
