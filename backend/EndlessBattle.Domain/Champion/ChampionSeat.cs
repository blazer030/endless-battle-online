using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Champion;

public sealed record ChampionSeat(
    string Name,
    int Level,
    int Element,
    string SpecialAttackName,
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
    bool IsPlayer = false,
    int Defenses = 0,
    DateTimeOffset? CrownedAt = null,
    DateTimeOffset? LockedUntil = null)
{
    // 冠軍沒有技能，Str／Smart 用不到，填 0
    public CombatantState ToCombatant() => new(
        Hp: Hp,
        HpMax: HpMax,
        Sp: Sp,
        SpMax: SpMax,
        At: At,
        Mat: Mat,
        Df: Df,
        Mdf: Mdf,
        Agi: Agi,
        Au: Au,
        Element: Element,
        SpecialAttackName: SpecialAttackName,
        Str: 0,
        Smart: 0);
}
