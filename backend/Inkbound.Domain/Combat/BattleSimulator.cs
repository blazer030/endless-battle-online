using Inkbound.Domain.Randomness;

namespace Inkbound.Domain.Combat;

public static class BattleSimulator
{
    public static BattleResult Simulate(CombatStats player, CombatStats monster, IRandomSource random, double timeLimitSeconds)
    {
        var events = new List<BattleEvent>();
        var playerHealth = player.MaxHealth;
        var monsterHealth = monster.MaxHealth;
        var playerInterval = 1 / player.AttacksPerSecond;
        var monsterInterval = 1 / monster.AttacksPerSecond;
        var playerNextAttack = playerInterval;
        var monsterNextAttack = monsterInterval;

        while (true)
        {
            // 同一時間出手時玩家先
            var playerActs = playerNextAttack <= monsterNextAttack;
            var attackTime = playerActs ? playerNextAttack : monsterNextAttack;
            if (attackTime > timeLimitSeconds)
            {
                return new BattleResult(events, null, timeLimitSeconds);
            }

            if (playerActs)
            {
                var (damage, isCritical) = DamageCalculator.Calculate(player, monster, random);
                monsterHealth = Math.Max(0, monsterHealth - damage);
                events.Add(new AttackEvent(attackTime, CombatSide.Player, damage, isCritical, monsterHealth));
                if (monsterHealth == 0)
                {
                    events.Add(new DefeatEvent(attackTime, CombatSide.Monster));
                    return new BattleResult(events, CombatSide.Player, attackTime);
                }

                playerNextAttack += playerInterval;
            }
            else
            {
                var (damage, isCritical) = DamageCalculator.Calculate(monster, player, random);
                playerHealth = Math.Max(0, playerHealth - damage);
                events.Add(new AttackEvent(attackTime, CombatSide.Monster, damage, isCritical, playerHealth));
                if (playerHealth == 0)
                {
                    events.Add(new DefeatEvent(attackTime, CombatSide.Player));
                    return new BattleResult(events, CombatSide.Monster, attackTime);
                }

                monsterNextAttack += monsterInterval;
            }
        }
    }
}
