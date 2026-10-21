using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public static class WogBattle
{
    public const int MaxRounds = 40;

    // 同一回合的後攻者晚半回合出手
    private const double SecondStrikeOffsetSeconds = BattleTiming.SecondsPerRound / 2;

    // 連打時同一次出手內每下的間隔
    private const double MultiHitIntervalSeconds = 0.1;

    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IRandomSource random) =>
        Run(player, monster, playerPreferred, [], random);

    // skills 已由呼叫端整理好（被動欄只放當前職業的技能）；怪物沒有技能
    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, IRandomSource random)
    {
        var events = new List<BattleEvent>();
        var playerHp = player.Hp;
        var playerSp = player.Sp;
        var monsterHp = monster.Hp;
        var damageDealt = 0;
        var caster = new SkillCaster(player, skills);

        // 玩家的攻擊方式整場只決定一次（WOG 在開戰時擲 m_type）；怪物每次出手前重擲
        var playerKind = playerPreferred ?? RoundResolver.ChooseKind(player.At, player.Mat, random);

        // 本回合套用技能修正後的雙方能力值
        var modifiers = caster.Current;
        var fighter = player;
        var opponent = monster;

        for (var round = 1; round <= MaxRounds; round++)
        {
            var roundStart = (round - 1) * BattleTiming.SecondsPerRound;
            caster.ExpireAtRoundStart();
            events.Add(new RoundEvent(roundStart, round));
            Record(caster.CastBeforeAttack(playerHp, playerSp, random), roundStart);

            modifiers = caster.Current;
            fighter = modifiers.ApplyToPlayer(player);
            opponent = modifiers.ApplyToMonster(monster);
            var firstAttacker = RoundResolver.PlayerStrikesFirst(fighter.Agi, opponent.Agi, random) ? CombatSide.Player : CombatSide.Monster;

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
        bool Strike(CombatSide attacker, double atSeconds) =>
            attacker == CombatSide.Player ? PlayerStrike(atSeconds) : MonsterStrike(atSeconds);

        // 連打每下各自擲命中／暴擊／傷害，擊倒就停
        bool PlayerStrike(double atSeconds)
        {
            var hits = playerKind == AttackKind.Physical ? modifiers.PhysicalHits : modifiers.MagicHits;
            for (var hit = 0; hit < hits; hit++)
            {
                var hitSeconds = atSeconds + hit * MultiHitIntervalSeconds;
                var result = RoundResolver.Strike(fighter, opponent, playerKind, random, modifiers.Rag, modifiers.CritChanceBonus, modifiers.BonusDamage);
                monsterHp = Math.Max(0, monsterHp - result.Damage);
                damageDealt += result.Damage;
                events.Add(new AttackEvent(hitSeconds, CombatSide.Player, playerKind, result.Hit, result.Critical, result.Damage,
                    result.ElementMultiplier, monsterHp, result.Critical ? player.SpecialAttackName : null));

                if (playerKind == AttackKind.Physical && result.Hit)
                {
                    Record(caster.CastAfterPhysicalHit(result.Damage, playerHp, playerSp, random), hitSeconds);
                }

                if (monsterHp == 0)
                {
                    return true;
                }
            }

            return false;
        }

        bool MonsterStrike(double atSeconds)
        {
            if (modifiers.StunChance > 0 && PhpRandom.Between(random, 1, 100) <= modifiers.StunChance)
            {
                events.Add(new AttackEvent(atSeconds, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0,
                    ElementMultiplier: 1.0, playerHp, SpecialAttackName: null, Stunned: true));
                return false;
            }

            var monsterKind = RoundResolver.ChooseKind(opponent.At, opponent.Mat, random);
            var result = RoundResolver.Strike(opponent, fighter, monsterKind, random);
            var damage = monsterKind == AttackKind.Physical ? modifiers.PhysicalDamageTaken(result.Damage) : result.Damage;
            playerHp = Math.Max(0, playerHp - damage);
            events.Add(new AttackEvent(atSeconds, CombatSide.Monster, monsterKind, result.Hit, result.Critical, damage,
                result.ElementMultiplier, playerHp, SpecialAttackName: null));
            return playerHp == 0;
        }

        void Record(IReadOnlyList<SkillCast> casts, double atSeconds)
        {
            foreach (var cast in casts)
            {
                playerHp = cast.HpAfter;
                playerSp = cast.SpAfter;
                events.Add(new SkillEvent(atSeconds, CombatSide.Player, cast.Equipped.Skill.Id, cast.Equipped.Skill.Name, cast.Equipped.Level,
                    cast.SpAfter, cast.Heal));
            }
        }

        BattleOutcome Outcome(CombatSide? winner, int rounds) =>
            new(events, winner, rounds, damageDealt, player with { Hp = playerHp, Sp = playerSp }, monster with { Hp = monsterHp });
    }

    private static CombatSide Opponent(CombatSide side) => side == CombatSide.Player ? CombatSide.Monster : CombatSide.Player;
}
