using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Domain.Adventure;

public static class AdventureSimulator
{
    public const double CatchUpWindowSeconds = 30;
    public const double MaxSimulatedSeconds = 86_400;

    // 超限時 WOG 以怪物等級 × 0.6、累計傷害 / 1.5 計算部分經驗
    private const double TimeoutLevelFactor = 0.6;
    private const double TimeoutDamageDivisor = 1.5;

    public static AdventureRun Run(
        Character character,
        JobDefinition job,
        StageDefinition stage,
        Func<string, MonsterDefinition> monsters,
        StaminaState stamina,
        DateTimeOffset from,
        DateTimeOffset to,
        IRandomSource random)
    {
        var stageMonsters = stage.MonsterIds.Select(monsters).ToArray();
        var totalSeconds = (to - from).TotalSeconds;
        var windowStart = totalSeconds - CatchUpWindowSeconds;
        var events = new List<BattleEvent>();
        var eventsCoverAll = true;
        int fights = 0, wins = 0, losses = 0, timeouts = 0, levelsGained = 0;
        long expTotal = 0, moneyTotal = 0, jobExpTotal = 0;
        var cursor = 0.0;

        // 只要還有可用時間就開新的一場，開打的戰鬥一律打完（可能超過可用時間）
        while (cursor < totalSeconds)
        {
            // 事件以整場為單位取捨；窗口外的場次照樣結算，只是不輸出事件
            var keepEvents = cursor >= windowStart;
            eventsCoverAll &= keepEvents;
            stamina = StaminaRules.Regenerate(stamina, from.AddSeconds(cursor));

            // 死亡後必須先住宿才能再戰
            if (character.Hp <= 0)
            {
                var rest = Hotel.Rest(character);
                character = rest.Character;
                if (keepEvents)
                {
                    events.Add(new RestEvent(cursor, rest.Cost, rest.ExpPenalty, rest.WaitSeconds));
                }

                cursor += rest.WaitSeconds;
                continue;
            }

            (stamina, var fDown, var intervalSeconds) = StaminaRules.Consume(stamina);

            var definition = PickMonster(stageMonsters, random);
            var player = CombatBridge.ToCombatant(character);
            var outcome = WogBattle.Run(player, CombatantState.From(definition), playerPreferred: null, random);
            var fightStart = cursor;
            var fightEnd = fightStart + outcome.Events[^1].AtSeconds;
            fights++;

            if (keepEvents)
            {
                events.Add(new EncounterEvent(fightStart, definition.Id, definition.Hp, definition.Hp,
                    player.Hp, player.HpMax, player.Sp, player.SpMax, StaminaDepleted: fDown < 1));
                foreach (var battleEvent in outcome.Events)
                {
                    events.Add(battleEvent with { AtSeconds = battleEvent.AtSeconds + fightStart });
                }
            }

            character = character with { Hp = outcome.Player.Hp, Sp = outcome.Player.Sp };

            long exp = 0, money = 0, jobExp = 0;
            switch (outcome.Winner)
            {
                case CombatSide.Player:
                    wins++;
                    exp = RewardFormulas.Exp(definition, definition.Hp, character.Level, killed: true, fDown, random);
                    money = RewardFormulas.Money(definition, fDown, random);
                    jobExp = RewardFormulas.JobExp(definition, fDown);
                    break;
                case CombatSide.Monster:
                    // 死亡沒有懲罰，HP 留在 0，下一場前住宿
                    losses++;
                    break;
                default:
                    // 超限：部分經驗，沒有金錢與熟練度，不計敗場
                    timeouts++;
                    var damageValue = (int)Math.Round(
                        Math.Min(outcome.DamageDealtToMonster, definition.Hp) / TimeoutDamageDivisor, MidpointRounding.AwayFromZero);
                    exp = RewardFormulas.Exp(definition, damageValue, character.Level, killed: false, fDown, random,
                        levelOverride: definition.Level * TimeoutLevelFactor);
                    break;
            }

            if (outcome.Winner == CombatSide.Player || exp > 0)
            {
                if (keepEvents)
                {
                    events.Add(new RewardEvent(fightEnd, exp, money, jobExp));
                }

                expTotal += exp;
                moneyTotal += money;
                jobExpTotal += jobExp;
                character = character with { Exp = character.Exp + exp, Money = character.Money + money };
                var levelUp = LevelUp.Apply(character, job, random, fightEnd);
                character = levelUp.Character;
                levelsGained += levelUp.Events.Count;
                if (keepEvents)
                {
                    events.AddRange(levelUp.Events);
                }
            }

            cursor += outcome.Rounds * BattleTiming.SecondsPerRound + intervalSeconds;
        }

        var summary = new AdvanceSummary(fights, wins, losses, timeouts, expTotal, moneyTotal, jobExpTotal, levelsGained);
        IReadOnlyList<JobGain> jobGains = jobExpTotal > 0 ? [new JobGain(job.Id, jobExpTotal, jobExpTotal)] : [];
        return new AdventureRun(events, summary, character, stamina, jobGains, cursor, eventsCoverAll);
    }

    // WOG：擲一次 rand(0,100)，遇敵率 Meet 不高於它的怪物入選，候選中均勻抽；沒有候選時退回全部怪物
    private static MonsterDefinition PickMonster(MonsterDefinition[] stageMonsters, IRandomSource random)
    {
        var roll = PhpRandom.Between(random, 0, 100);
        var candidateCount = 0;
        foreach (var monster in stageMonsters)
        {
            if (monster.Meet <= roll)
            {
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            return stageMonsters[random.NextInt(stageMonsters.Length)];
        }

        var remaining = random.NextInt(candidateCount);
        foreach (var monster in stageMonsters)
        {
            if (monster.Meet <= roll && remaining-- == 0)
            {
                return monster;
            }
        }

        throw new InvalidOperationException("候選怪物計數不一致");
    }
}
