using Inkbound.Domain.Combat;
using Inkbound.Domain.Content;
using Inkbound.Domain.Randomness;

namespace Inkbound.Domain.Adventure;

public static class AdventureSimulator
{
    public const double BattleTimeLimitSeconds = 60;
    public const double RestSecondsBetweenFights = 1;

    public static AdventureRun Run(
        CombatStats player,
        IReadOnlyList<MonsterDefinition> stageMonsters,
        double availableSeconds,
        IRandomSource random)
    {
        var events = new List<BattleEvent>();
        var cursor = 0.0;
        long experienceGained = 0;
        long spiritStonesGained = 0;

        // 只要還有可用時間就開新的一場，而且開打的戰鬥一律打完（可能超過可用時間）。
        // 若改成捨棄打不完的那場、下次重抽，收下的戰鬥會偏向打得快的，結果會受輪詢頻率影響。
        while (cursor < availableSeconds)
        {
            var monster = stageMonsters[random.NextInt(stageMonsters.Count)];
            var battle = BattleSimulator.Simulate(player, monster.Stats, random, BattleTimeLimitSeconds);

            var fightStart = cursor;
            events.Add(new EncounterEvent(fightStart, monster.Id));
            events.AddRange(battle.Events.Select(battleEvent => battleEvent with { AtSeconds = battleEvent.AtSeconds + fightStart }));

            if (battle.Winner == CombatSide.Player)
            {
                events.Add(new RewardEvent(fightStart + battle.DurationSeconds, monster.ExperienceReward, monster.SpiritStoneReward));
                experienceGained += monster.ExperienceReward;
                spiritStonesGained += monster.SpiritStoneReward;
            }

            cursor += battle.DurationSeconds + RestSecondsBetweenFights;
        }

        return new AdventureRun(events, cursor, experienceGained, spiritStonesGained);
    }
}
