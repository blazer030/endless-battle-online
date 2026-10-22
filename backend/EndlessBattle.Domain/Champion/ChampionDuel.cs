using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Champion;

public sealed record DuelResult(Character Character, ChampionSeat Seat, BattleOutcome Battle, bool Won);

public static class ChampionDuel
{
    // 呼叫前先以 ChampionRules.CanChallenge 確認。挑戰費勝負都扣、沒有任何報酬；不用補給、沒有寵物。
    // 勝：席位換成挑戰者的快照；敗或回合用完：冠軍留著戰後 HP（消耗戰）、衛冕 +1
    public static DuelResult Fight(
        Character character, EquipmentBonus equipment, IReadOnlyList<EquippedSkill> skills, ChampionSeat seat, DateTimeOffset now, IRandomSource random)
    {
        var battle = WogBattle.Run(CombatBridge.ToCombatant(character, equipment), seat.ToCombatant(), playerPreferred: null, skills, supply: null, random,
            pet: null);
        var won = battle.Winner == CombatSide.Player;
        var after = character with
        {
            Money = character.Money - ChampionRules.ChallengeFee,
            Hp = battle.Player.Hp,
            Sp = battle.Player.Sp,
        };
        var seatAfter = won
            ? ChampionRules.Snapshot(character, equipment, now)
            : seat with { Hp = battle.Monster.Hp, Defenses = seat.Defenses + 1 };
        return new DuelResult(after, seatAfter, battle, won);
    }
}
