using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Characters;

public static class CombatBridge
{
    public static CombatantState ToCombatant(Character character) => ToCombatant(character, EquipmentBonus.Zero);

    // 能力一律用有效值（裸點 + 裝備）；攻擊屬性就是角色屬性
    public static CombatantState ToCombatant(Character character, EquipmentBonus equipment)
    {
        var total = equipment.Effective(character.Base);
        var derived = DerivedStats.From(character.Base, equipment);
        return new CombatantState(
            Hp: character.Hp,
            HpMax: derived.HpMax,
            Sp: character.Sp,
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
            Smart: total.Smart);
    }
}
