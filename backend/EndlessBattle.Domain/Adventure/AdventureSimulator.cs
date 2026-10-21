using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Adventure;

// 計畫 4 Task 5 重寫：加入獎勵、行動力與住宿
public static class AdventureSimulator
{
    public static AdventureRun Run(
        CombatantState player,
        IReadOnlyList<MonsterDefinition> stageMonsters,
        double availableSeconds,
        IRandomSource random)
    {
        var events = new List<BattleEvent>();
        var cursor = 0.0;

        // 只要還有可用時間就開新的一場，而且開打的戰鬥一律打完（可能超過可用時間）。
        // 若改成捨棄打不完的那場、下次重抽，收下的戰鬥會偏向打得快的，結果會受輪詢頻率影響。
        while (cursor < availableSeconds)
        {
            // 暫時規則：HP 歸零就滿血再戰，Task 4／5 改為住宿
            if (player.Hp == 0)
            {
                player = player with { Hp = player.HpMax };
            }

            var definition = stageMonsters[random.NextInt(stageMonsters.Count)];
            var monster = CombatantState.From(definition);
            var fightStart = cursor;
            events.Add(new EncounterEvent(fightStart, definition.Id, monster.Hp, monster.HpMax,
                player.Hp, player.HpMax, player.Sp, player.SpMax, StaminaDepleted: false));

            var battle = WogBattle.Run(player, monster, playerPreferred: null, random);
            events.AddRange(battle.Events.Select(battleEvent => battleEvent with { AtSeconds = battleEvent.AtSeconds + fightStart }));

            player = battle.Player;
            cursor += battle.Rounds * BattleTiming.SecondsPerRound + BattleTiming.FightIntervalSeconds;
        }

        return new AdventureRun(events, cursor, ExperienceGained: 0, SpiritStonesGained: 0);
    }
}
