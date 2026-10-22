using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public static class WogBattle
{
    public const int MaxRounds = 40;

    // 同一回合的後攻者晚半回合出手
    private const double SecondStrikeOffsetSeconds = BattleTiming.SecondsPerRound / 2;

    // 寵物在雙方出手之後、回合結束前出手
    private const double PetStrikeOffsetSeconds = BattleTiming.SecondsPerRound * 0.95;

    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IRandomSource random) =>
        Run(player, monster, playerPreferred, [], random);

    // skills 已由呼叫端整理好（被動欄只放當前職業的技能）；怪物沒有技能
    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, IRandomSource random) =>
        Run(player, monster, playerPreferred, skills, supply: null, random);

    // supply 為補給欄的補給品，null 表示沒有；剩餘數量由 BattleOutcome.Supply 帶回
    public static BattleOutcome Run(
        CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, SupplyState? supply, IRandomSource random) =>
        Run(player, monster, playerPreferred, skills, supply, random, pet: null);

    // pet 為攜帶中的寵物，null 表示沒有（不擲任何寵物的骰）；escapeHpPercent 為 0 表示不逃（不擲逃跑的骰）
    public static BattleOutcome Run(
        CombatantState player,
        CombatantState monster,
        AttackKind? playerPreferred,
        IReadOnlyList<EquippedSkill> skills,
        SupplyState? supply,
        IRandomSource random,
        PetCombatant? pet,
        int escapeHpPercent = 0)
    {
        var events = new List<BattleEvent>();
        var playerHp = player.Hp;
        var playerSp = player.Sp;
        var monsterHp = monster.Hp;
        var damageDealt = 0;
        var caster = new SkillCaster(player, skills);
        var potionsUsed = 0;
        // 代擋量 ≤ 0 表示寵物不在場（退出這一場），不再代擋也不再出手
        var guard = pet?.Guard ?? 0;

        // 玩家的攻擊方式整場只決定一次（WOG 在開戰時擲 m_type）；怪物每次出手前重擲
        var playerKind = playerPreferred ?? RoundResolver.ChooseKind(player.At, player.Mat, random);

        // 本回合套用技能修正後的雙方能力值；每回合開頭都會重新指定，這裡的初值只是讓區域函式能捕捉
        var modifiers = caster.Current;
        var fighter = player;
        var opponent = monster;

        for (var round = 1; round <= MaxRounds; round++)
        {
            var roundStart = (round - 1) * BattleTiming.SecondsPerRound;
            caster.ExpireAtRoundStart();
            events.Add(new RoundEvent(roundStart, round));
            DrinkPotion(roundStart);

            // 第 2 回合起，喝完補給後 HP 仍低於門檻才擲逃跑
            if (round > 1 && EscapeRules.ShouldTry(playerHp, player.HpMax, escapeHpPercent) && EscapeRules.Succeeds(random))
            {
                events.Add(new EscapeEvent(roundStart));
                return Outcome(null, round) with { Escaped = true };
            }

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

            if (PetStrike(roundStart + PetStrikeOffsetSeconds))
            {
                events.Add(new DefeatEvent(events[^1].AtSeconds, CombatSide.Monster));
                return Outcome(CombatSide.Player, round);
            }
        }

        events.Add(new TimeoutEvent(MaxRounds * BattleTiming.SecondsPerRound, MaxRounds, damageDealt));
        return Outcome(null, MaxRounds);

        // 回傳守方是否倒下
        bool Strike(CombatSide attacker, double atSeconds) =>
            attacker == CombatSide.Player ? PlayerStrike(atSeconds) : MonsterStrike(atSeconds);

        // 連打每下各自擲命中／暴擊／傷害，擊倒就停；各下平均分在攻方的半回合內，最後一下早於對手出手
        bool PlayerStrike(double atSeconds)
        {
            var hits = playerKind == AttackKind.Physical ? modifiers.PhysicalHits : modifiers.MagicHits;
            var intervalSeconds = SecondStrikeOffsetSeconds / hits;
            for (var hit = 0; hit < hits; hit++)
            {
                var hitSeconds = atSeconds + hit * intervalSeconds;
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

            // 只有命中的那一下才擲代擋；代擋成功時整下由寵物承受
            if (result.Hit && pet is not null && guard > 0 && PetBattle.Activates(pet, random))
            {
                guard -= damage;
                events.Add(new PetGuardEvent(atSeconds, pet.Name, damage, GuardAfter: Math.Max(0, guard), Retired: guard <= 0));
                return false;
            }

            playerHp = Math.Max(0, playerHp - damage);
            events.Add(new AttackEvent(atSeconds, CombatSide.Monster, monsterKind, result.Hit, result.Critical, damage,
                result.ElementMultiplier, playerHp, SpecialAttackName: null));
            return playerHp == 0;
        }

        // 回傳怪物是否倒下；寵物傷害計入對怪物的累計傷害
        bool PetStrike(double atSeconds)
        {
            if (pet is null || guard <= 0 || !PetBattle.Activates(pet, random))
            {
                return false;
            }

            var result = PetBattle.Strike(pet, fighter, opponent, random);
            monsterHp = Math.Max(0, monsterHp - result.Damage);
            damageDealt += result.Damage;
            events.Add(new PetAttackEvent(atSeconds, pet.Name, result.Kind, result.Hit, result.Critical, result.Damage, monsterHp));
            return monsterHp == 0;
        }

        // 玩家出手前（技能判定之前）喝補給，不佔用這回合的攻擊、不擲骰
        void DrinkPotion(double atSeconds)
        {
            if (!PotionRules.ShouldUse(player with { Hp = playerHp, Sp = playerSp }, supply, potionsUsed))
            {
                return;
            }

            var hpGained = Math.Min(supply!.Hp, player.HpMax - playerHp);
            var spGained = Math.Min(supply.Sp, player.SpMax - playerSp);
            playerHp += hpGained;
            playerSp += spGained;
            supply = supply with { Quantity = supply.Quantity - 1 };
            potionsUsed++;
            events.Add(new PotionEvent(atSeconds, supply.ItemId, supply.Name, hpGained, spGained, supply.Quantity));
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
            new(events, winner, rounds, damageDealt, player with { Hp = playerHp, Sp = playerSp }, monster with { Hp = monsterHp }, supply);
    }

    private static CombatSide Opponent(CombatSide side) => side == CombatSide.Player ? CombatSide.Monster : CombatSide.Player;
}
