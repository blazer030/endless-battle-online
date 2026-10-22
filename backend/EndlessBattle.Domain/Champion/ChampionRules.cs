using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Champion;

public static class ChampionRules
{
    public const long ChallengeFee = 500;
    public static readonly TimeSpan LockAfterWin = TimeSpan.FromSeconds(604_800);

    public static IReadOnlyList<string> CanChallenge(Character character, ChampionSeat seat, DateTimeOffset now)
    {
        var reasons = new List<string>();
        if (now < seat.LockedUntil)
        {
            reasons.Add("championLocked");
        }

        if (character.Money < ChallengeFee)
        {
            reasons.Add("notEnoughMoney");
        }

        if (character.Hp <= 0)
        {
            reasons.Add("noHp");
        }

        return reasons;
    }

    // 挑戰者的含裝備有效能力，滿 HP／SP
    public static ChampionSeat Snapshot(Character character, EquipmentBonus equipment, DateTimeOffset now)
    {
        var stats = CombatBridge.ToCombatant(character, equipment);
        return new ChampionSeat(
            Name: character.Name,
            Level: character.Level,
            Element: character.Element,
            SpecialAttackName: character.SpecialAttackName,
            Hp: stats.HpMax,
            HpMax: stats.HpMax,
            Sp: stats.SpMax,
            SpMax: stats.SpMax,
            At: stats.At,
            Mat: stats.Mat,
            Df: stats.Df,
            Mdf: stats.Mdf,
            Agi: stats.Agi,
            Au: stats.Au,
            IsPlayer: true,
            Defenses: 0,
            CrownedAt: now,
            LockedUntil: now + LockAfterWin);
    }
}
