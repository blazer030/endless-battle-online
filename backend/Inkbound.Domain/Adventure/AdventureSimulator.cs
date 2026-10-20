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

        while (true)
        {
            var monster = stageMonsters[random.NextInt(stageMonsters.Count)];
            var timeLimit = Math.Min(BattleTimeLimitSeconds, availableSeconds - cursor);
            var battle = BattleSimulator.Simulate(player, monster.Stats, random, timeLimit);

            // 剩下的時間不夠打完這場：捨棄，時間留到下一次
            if (battle.Winner is null && timeLimit < BattleTimeLimitSeconds)
            {
                break;
            }

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

        return new AdventureRun(events, Math.Min(cursor, availableSeconds), experienceGained, spiritStonesGained);
    }
}
