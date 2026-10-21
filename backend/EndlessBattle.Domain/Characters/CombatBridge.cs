using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Characters;

public static class CombatBridge
{
    // 本計畫沒有裝備：能力值直接由裸點推導，攻擊屬性就是角色屬性
    public static CombatantState ToCombatant(Character character)
    {
        var derived = DerivedStats.From(character.Base);
        return new CombatantState(
            Hp: character.Hp,
            HpMax: derived.HpMax,
            Sp: character.Sp,
            SpMax: derived.SpMax,
            At: derived.At,
            Mat: derived.Mat,
            Df: derived.Df,
            Mdf: derived.Mdf,
            Agi: character.Base.Agi,
            Au: character.Base.Au,
            Element: character.Element,
            SpecialAttackName: character.SpecialAttackName);
    }
}
