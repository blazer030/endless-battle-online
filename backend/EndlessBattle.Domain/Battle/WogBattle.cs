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

    // skills 已由呼叫端整理好（被動欄只放當前職業的技能）
    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, IRandomSource random) =>
        Run(player, monster, playerPreferred, skills, supply: null, random);

    // supply 為補給欄的補給品，null 表示沒有；剩餘數量由 BattleOutcome.Supply 帶回
    public static BattleOutcome Run(
        CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, SupplyState? supply, IRandomSource random) =>
        Run(player, monster, playerPreferred, skills, supply, random, pet: null);

    // pet 為攜帶中的寵物，null 表示沒有（不擲任何寵物的骰）；escapeHpPercent 為 0 表示不逃（不擲逃跑的骰）；
    // money 是玩家身上的錢，給金錢消耗的技能用，null 表示不限（不扣）；monsterSkills 是怪物的技能，沒有技能時不擲任何骰
    public static BattleOutcome Run(
        CombatantState player,
        CombatantState monster,
        AttackKind? playerPreferred,
        IReadOnlyList<EquippedSkill> skills,
        SupplyState? supply,
        IRandomSource random,
        PetCombatant? pet,
        int escapeHpPercent = 0,
        long? money = null,
        IReadOnlyList<EquippedSkill>? monsterSkills = null)
    {
        var events = new List<BattleEvent>();
        var playerHp = player.Hp;
        var playerSp = player.Sp;
        var monsterHp = monster.Hp;
        var monsterSp = monster.Sp;
        var damageDealt = 0;
        var caster = new SkillCaster(player, skills, monster, money);
        var monsterCaster = new SkillCaster(monster, monsterSkills ?? [], player);
        var potionsUsed = 0;
        // 代擋量 ≤ 0 表示寵物不在場（退出這一場），不再代擋也不再出手
        var guard = pet?.Guard ?? 0;

        // 玩家的攻擊方式整場只決定一次（WOG 在開戰時擲 m_type）；怪物每次出手前重擲
        var playerKind = playerPreferred ?? RoundResolver.ChooseKind(player.At, player.Mat, random);

        // 本回合雙方的技能修正與套用後的能力值；每回合開頭都會重新指定，這裡的初值只是讓區域函式能捕捉
        var playerModifiers = caster.Current;
        var monsterModifiers = monsterCaster.Current;
        var fighter = player;
        var opponent = monster;

        for (var round = 1; round <= MaxRounds; round++)
        {
            var roundStart = (round - 1) * BattleTiming.SecondsPerRound;
            caster.ExpireAtRoundStart();
            monsterCaster.ExpireAtRoundStart();
            events.Add(new RoundEvent(roundStart, round));
            DrinkPotion(roundStart);

            // 第 2 回合起，喝完補給後 HP 仍低於門檻才擲逃跑；逃跑率加成看回合開頭失效後的修正
            if (round > 1 && EscapeRules.ShouldTry(playerHp, player.HpMax, escapeHpPercent) && EscapeRules.Succeeds(random, caster.Current.EscapeBonus))
            {
                events.Add(new EscapeEvent(roundStart));
                return Outcome(null, round) with { Escaped = true };
            }

            RecordPlayer(caster.CastBeforeAttack(playerHp, playerSp, random, monsterHp, monsterSp, monster.SpMax), roundStart);
            if (monsterHp == 0)
            {
                events.Add(new DefeatEvent(roundStart, CombatSide.Monster));
                return Outcome(CombatSide.Player, round);
            }

            // 怪物接在玩家之後施放，雙方的敏捷增減都影響這一回合的先攻
            RecordMonster(monsterCaster.CastBeforeAttack(monsterHp, monsterSp, random, playerHp, playerSp, player.SpMax), roundStart);
            if (playerHp == 0)
            {
                events.Add(new DefeatEvent(roundStart, CombatSide.Player));
                return Outcome(CombatSide.Monster, round);
            }

            playerModifiers = caster.Current;
            monsterModifiers = monsterCaster.Current;
            fighter = monsterModifiers.ApplyToOpponent(playerModifiers.ApplyToCaster(player));
            opponent = playerModifiers.ApplyToOpponent(monsterModifiers.ApplyToCaster(monster));
            var firstAttacker = RoundResolver.PlayerStrikesFirst(fighter.Agi, opponent.Agi, random) ? CombatSide.Player : CombatSide.Monster;

            // 先攻者擊倒對手時，後攻者不再出手
            if (Strike(firstAttacker, roundStart) || Strike(Opponent(firstAttacker), roundStart + SecondStrikeOffsetSeconds))
            {
                var loser = monsterHp == 0 ? CombatSide.Monster : CombatSide.Player;
                events.Add(new DefeatEvent(events[^1].AtSeconds, loser));
                return Outcome(Opponent(loser), round);
            }

            var roundEnd = roundStart + PetStrikeOffsetSeconds;
            if (PetStrike(roundEnd) || RoundEndSkills(roundEnd) || DamageOverTime(roundEnd))
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

        // 連打每下各自擲命中／暴擊／傷害，擊倒就停；各下平均分在攻方的半回合內，最後一下早於對手出手
        bool PlayerStrike(double atSeconds)
        {
            if (monsterModifiers.StunChance > 0 && PhpRandom.Between(random, 1, 100) <= monsterModifiers.StunChance)
            {
                events.Add(new AttackEvent(atSeconds, CombatSide.Player, playerKind, Hit: false, Critical: false, Damage: 0,
                    ElementMultiplier: 1.0, monsterHp, Stunned: true));
                return false;
            }

            var hits = playerKind == AttackKind.Physical ? playerModifiers.PhysicalHits : playerModifiers.MagicHits;
            var intervalSeconds = SecondStrikeOffsetSeconds / hits;
            for (var hit = 0; hit < hits; hit++)
            {
                var hitSeconds = atSeconds + hit * intervalSeconds;
                var result = RoundResolver.Strike(playerModifiers.AttackingAs(fighter), monsterModifiers.DefendingAs(opponent), playerKind, random,
                    playerModifiers.Rag + monsterModifiers.EnemyRag, playerModifiers.CriticalChanceFor(playerKind), playerModifiers.BonusDamageFor(playerKind),
                    playerModifiers.CriticalCapFor(playerKind), playerModifiers.ElementAdvantagePercent);
                var damage = monsterModifiers.DamageTaken(playerKind, result.Damage);
                monsterHp = Math.Max(0, monsterHp - damage);
                damageDealt += damage;
                events.Add(new AttackEvent(hitSeconds, CombatSide.Player, playerKind, result.Hit, result.Critical, damage,
                    result.ElementMultiplier, monsterHp));

                if (playerKind == AttackKind.Physical && result.Hit)
                {
                    RecordPlayer(caster.CastAfterPhysicalHit(damage, playerHp, playerSp, random, monsterHp, monsterSp, monster.SpMax), hitSeconds);
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
            if (playerModifiers.StunChance > 0 && PhpRandom.Between(random, 1, 100) <= playerModifiers.StunChance)
            {
                events.Add(new AttackEvent(atSeconds, CombatSide.Monster, AttackKind.Physical, Hit: false, Critical: false, Damage: 0,
                    ElementMultiplier: 1.0, playerHp, Stunned: true));
                return false;
            }

            var monsterKind = RoundResolver.ChooseKind(opponent.At, opponent.Mat, random);
            var hits = monsterKind == AttackKind.Physical ? monsterModifiers.PhysicalHits : monsterModifiers.MagicHits;
            var intervalSeconds = SecondStrikeOffsetSeconds / hits;
            for (var hit = 0; hit < hits; hit++)
            {
                var hitSeconds = atSeconds + hit * intervalSeconds;
                var result = RoundResolver.Strike(monsterModifiers.AttackingAs(opponent), playerModifiers.DefendingAs(fighter), monsterKind, random,
                    playerModifiers.EnemyRag + monsterModifiers.Rag, monsterModifiers.CriticalChanceFor(monsterKind), monsterModifiers.BonusDamageFor(monsterKind),
                    monsterModifiers.CriticalCapFor(monsterKind), monsterModifiers.ElementAdvantagePercent);
                var damage = playerModifiers.DamageTaken(monsterKind, result.Damage);

                // 只有命中的那一下才擲代擋；代擋成功時整下由寵物承受，玩家沒有受傷，也不觸發怪物的命中後技能
                if (result.Hit && pet is not null && guard > 0 && PetBattle.Activates(pet, random))
                {
                    guard -= damage;
                    events.Add(new PetGuardEvent(hitSeconds, pet.Name, damage, GuardAfter: Math.Max(0, guard), Retired: guard <= 0));
                    continue;
                }

                playerHp = Math.Max(0, playerHp - damage);
                events.Add(new AttackEvent(hitSeconds, CombatSide.Monster, monsterKind, result.Hit, result.Critical, damage,
                    result.ElementMultiplier, playerHp));

                if (monsterKind == AttackKind.Physical && result.Hit)
                {
                    RecordMonster(monsterCaster.CastAfterPhysicalHit(damage, monsterHp, monsterSp, random, playerHp, playerSp, player.SpMax), hitSeconds);
                }

                if (playerHp == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // 回傳怪物是否倒下；寵物傷害計入對怪物的累計傷害
        bool PetStrike(double atSeconds)
        {
            if (pet is null || guard <= 0 || !PetBattle.Activates(pet, random))
            {
                return false;
            }

            var result = PetBattle.Strike(pet, fighter, opponent, random);
            var damage = monsterModifiers.DamageTaken(result.Kind, result.Damage);
            monsterHp = Math.Max(0, monsterHp - damage);
            damageDealt += damage;
            events.Add(new PetAttackEvent(atSeconds, pet.Name, result.Kind, result.Hit, result.Critical, damage, monsterHp));
            return monsterHp == 0;
        }

        // 玩家出手前（技能判定之前）喝補給，不佔用這回合的攻擊、不擲骰
        void DrinkPotion(double atSeconds)
        {
            if (supply is null || !PotionRules.ShouldUse(player with { Hp = playerHp, Sp = playerSp }, supply, potionsUsed))
            {
                return;
            }

            var hpGained = Math.Min(supply.Hp, player.HpMax - playerHp);
            var spGained = Math.Min(supply.Sp, player.SpMax - playerSp);
            playerHp += hpGained;
            playerSp += spGained;
            supply = supply with { Quantity = supply.Quantity - 1 };
            potionsUsed++;
            events.Add(new PotionEvent(atSeconds, supply.ItemId, supply.Name, hpGained, spGained, supply.Quantity));
        }

        void RecordPlayer(IReadOnlyList<SkillCast> casts, double atSeconds)
        {
            foreach (var cast in casts)
            {
                playerHp = cast.HpAfter;
                playerSp = cast.SpAfter;
                monsterHp = Math.Max(0, monsterHp - cast.OpponentDamage);
                damageDealt += cast.OpponentDamage;
                monsterSp = Math.Max(0, monsterSp - cast.OpponentSpLoss);
                monsterCaster.Silence(cast.SilenceRounds);
                events.Add(SkillEventOf(cast, CombatSide.Player, monsterHp, atSeconds));
            }
        }

        void RecordMonster(IReadOnlyList<SkillCast> casts, double atSeconds)
        {
            foreach (var cast in casts)
            {
                monsterHp = cast.HpAfter;
                monsterSp = cast.SpAfter;
                playerHp = Math.Max(0, playerHp - cast.OpponentDamage);
                playerSp = Math.Max(0, playerSp - cast.OpponentSpLoss);
                caster.Silence(cast.SilenceRounds);
                events.Add(SkillEventOf(cast, CombatSide.Monster, playerHp, atSeconds));
            }
        }

        // 回合結束時機的技能，玩家先、怪物後；回傳是否有人倒下
        bool RoundEndSkills(double atSeconds)
        {
            RecordPlayer(caster.CastAtRoundEnd(playerHp, playerSp, random, monsterHp, monsterSp, monster.SpMax), atSeconds);
            if (monsterHp == 0)
            {
                return true;
            }

            RecordMonster(monsterCaster.CastAtRoundEnd(monsterHp, monsterSp, random, playerHp, playerSp, player.SpMax), atSeconds);
            return playerHp == 0;
        }

        // 回合結束時持續傷害生效，玩家的先、怪物的後；回傳是否有人倒下
        bool DamageOverTime(double atSeconds)
        {
            foreach (var (equipped, damage) in caster.DamageOverTimeBySkill())
            {
                monsterHp = Math.Max(0, monsterHp - damage);
                damageDealt += damage;
                events.Add(new SkillEvent(atSeconds, CombatSide.Player, equipped.Skill.Id, equipped.Skill.Name, equipped.Level,
                    playerSp, Heal: 0, damage, monsterHp, Lasting: true));
                if (monsterHp == 0)
                {
                    return true;
                }
            }

            foreach (var (equipped, damage) in monsterCaster.DamageOverTimeBySkill())
            {
                playerHp = Math.Max(0, playerHp - damage);
                events.Add(new SkillEvent(atSeconds, CombatSide.Monster, equipped.Skill.Id, equipped.Skill.Name, equipped.Level,
                    monsterSp, Heal: 0, damage, playerHp, Lasting: true));
                if (playerHp == 0)
                {
                    return true;
                }
            }

            return false;
        }

        BattleOutcome Outcome(CombatSide? winner, int rounds) =>
            new(events, winner, rounds, damageDealt, player with { Hp = playerHp, Sp = playerSp }, monster with { Hp = monsterHp, Sp = monsterSp }, supply,
                MoneySpent: money - caster.Money ?? 0);
    }

    private static SkillEvent SkillEventOf(SkillCast cast, CombatSide side, int opponentHp, double atSeconds) =>
        new(atSeconds, side, cast.Equipped.Skill.Id, cast.Equipped.Skill.Name, cast.Equipped.Level,
            cast.SpAfter, cast.Heal, cast.OpponentDamage, cast.OpponentDamage > 0 ? opponentHp : null, cast.HpCost, cast.SpCost, cast.MoneyCost,
            cast.OpponentSpLoss, cast.SpGain);

    private static CombatSide Opponent(CombatSide side) => side == CombatSide.Player ? CombatSide.Monster : CombatSide.Player;
}
