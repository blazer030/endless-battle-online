using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public static class WogBattle
{
    public const int MaxRounds = 40;

    // 同一回合的後攻者晚半回合出手
    private const double SecondStrikeOffsetSeconds = BattleTiming.SecondsPerRound / 2;

    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IRandomSource random)
    {
        var events = new List<BattleEvent>();
        var playerHp = player.Hp;
        var monsterHp = monster.Hp;
        var damageDealt = 0;

        // 玩家的攻擊方式整場只決定一次（WOG 在開戰時擲 m_type）；怪物每次出手前重擲
        var playerKind = playerPreferred ?? RoundResolver.ChooseKind(player.At, player.Mat, random);

        for (var round = 1; round <= MaxRounds; round++)
        {
            var roundStart = (round - 1) * BattleTiming.SecondsPerRound;
            events.Add(new RoundEvent(roundStart, round));
            var firstAttacker = RoundResolver.PlayerStrikesFirst(player.Agi, monster.Agi, random) ? CombatSide.Player : CombatSide.Monster;

            // 先攻者擊倒對手時，後攻者不再出手
            if (Strike(firstAttacker, roundStart) || Strike(Opponent(firstAttacker), roundStart + SecondStrikeOffsetSeconds))
            {
                var loser = monsterHp == 0 ? CombatSide.Monster : CombatSide.Player;
                events.Add(new DefeatEvent(events[^1].AtSeconds, loser));
                return Outcome(Opponent(loser), round);
            }
        }

        events.Add(new TimeoutEvent(MaxRounds * BattleTiming.SecondsPerRound, MaxRounds, damageDealt));
        return Outcome(null, MaxRounds);

        // 回傳守方是否倒下
        bool Strike(CombatSide attacker, double atSeconds)
        {
            if (attacker == CombatSide.Player)
            {
                var result = RoundResolver.Strike(player, monster, playerKind, random);
                monsterHp = Math.Max(0, monsterHp - result.Damage);
                damageDealt += result.Damage;
                events.Add(new AttackEvent(atSeconds, CombatSide.Player, playerKind, result.Hit, result.Critical, result.Damage,
                    result.ElementMultiplier, monsterHp, result.Critical ? player.SpecialAttackName : null));
                return monsterHp == 0;
            }

            var monsterKind = RoundResolver.ChooseKind(monster.At, monster.Mat, random);
            var monsterResult = RoundResolver.Strike(monster, player, monsterKind, random);
            playerHp = Math.Max(0, playerHp - monsterResult.Damage);
            events.Add(new AttackEvent(atSeconds, CombatSide.Monster, monsterKind, monsterResult.Hit, monsterResult.Critical, monsterResult.Damage,
                monsterResult.ElementMultiplier, playerHp, SpecialAttackName: null));
            return playerHp == 0;
        }

        BattleOutcome Outcome(CombatSide? winner, int rounds) =>
            new(events, winner, rounds, damageDealt, player with { Hp = playerHp }, monster with { Hp = monsterHp });
    }

    private static CombatSide Opponent(CombatSide side) => side == CombatSide.Player ? CombatSide.Monster : CombatSide.Player;
}
