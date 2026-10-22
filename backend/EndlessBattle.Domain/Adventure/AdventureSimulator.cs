using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Domain.Stamina;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Domain.Adventure;

public static class AdventureSimulator
{
    public const double CatchUpWindowSeconds = 30;
    public const double MaxSimulatedSeconds = 86_400;

    // spec §7.1 的 HP 門檻，先固定；死亡（HP ≤ 0）一定在門檻之下
    public const double AutoRestHpRatio = 0.15;

    // SP 只有住宿才會回復，所以只在裝備了技能時才看 SP 門檻；沒有技能的角色 SP 低也不影響戰鬥
    public const double AutoRestSpRatio = 0.15;

    // 超限時 WOG 以怪物等級 × 0.6、累計傷害 / 1.5 計算部分經驗
    private const double TimeoutLevelFactor = 0.6;
    private const double TimeoutDamageDivisor = 1.5;

    public static AdventureRun Run(
        Character character,
        JobDefinition job,
        PlaceDefinition place,
        IReadOnlyList<MonsterDefinition> placeMonsters,
        Func<string, ItemDefinition> items,
        IReadOnlyList<EquippedSkill> skills,
        StaminaState stamina,
        DateTimeOffset from,
        DateTimeOffset to,
        IRandomSource random,
        EquipmentBonus? equipment = null,
        SupplyState? supply = null,
        IReadOnlyList<ActiveMission>? missions = null)
    {
        missions ??= [];
        // 裝備在整段推進中不變；null 表示沒有裝備
        var bonus = equipment ?? EquipmentBonus.Zero;
        var totalSeconds = (to - from).TotalSeconds;
        var windowStart = totalSeconds - CatchUpWindowSeconds;
        var events = new List<BattleEvent>();
        var droppedItemIds = new List<string>();
        var eventsCoverAll = true;
        int fights = 0, wins = 0, losses = 0, timeouts = 0, levelsGained = 0, skillsCast = 0, potionsUsed = 0, missionKills = 0;
        long expTotal = 0, moneyTotal = 0, jobExpTotal = 0;
        var cursor = 0.0;

        // 只要還有可用時間就開新的一場，開打的戰鬥一律打完（可能超過可用時間）
        while (cursor < totalSeconds)
        {
            // 事件以整場為單位取捨；窗口外的場次照樣結算，只是不輸出事件
            var keepEvents = cursor >= windowStart;
            stamina = StaminaRules.Regenerate(stamina, from.AddSeconds(cursor));

            // 死亡、HP 低於門檻，或（有技能時）SP 低於門檻，先住宿再開打
            var derived = DerivedStats.From(character.Base, bonus);
            if (character.Hp < derived.HpMax * AutoRestHpRatio
                || (skills.Count > 0 && character.Sp < derived.SpMax * AutoRestSpRatio))
            {
                var rest = Hotel.Rest(character, bonus);
                character = rest.Character;
                if (keepEvents)
                {
                    events.Add(new RestEvent(cursor, rest.Cost, rest.ExpPenalty, rest.WaitSeconds));
                }

                cursor += rest.WaitSeconds;
                continue;
            }

            (stamina, var fDown, var intervalSeconds) = StaminaRules.Consume(stamina);

            // 任務怪只在其任務尚未達成且未過期時出現；任務邏輯不擲骰，沒有任務怪時擲骰序列不變
            var spawnable = KillCounter.SpawnableMissionIds(missions, from.AddSeconds(cursor));
            var definition = MonsterPool.Pick(placeMonsters, character.PlaceId, character.SubPlace, random, spawnable);
            var player = CombatBridge.ToCombatant(character, bonus);
            // 補給的剩餘數量一場接一場帶下去；每場的 9 次上限由戰鬥自己重新計算
            var outcome = WogBattle.Run(player, CombatantState.From(definition), playerPreferred: null, skills, supply, random);
            supply = outcome.Supply;
            var fightStart = cursor;
            var fightEnd = fightStart + outcome.Events[^1].AtSeconds;
            fights++;
            // 只有略去戰鬥才算事件不完整；略去的若只是住宿，前端從戰鬥事件就能接上
            eventsCoverAll &= keepEvents;

            if (keepEvents)
            {
                events.Add(new EncounterEvent(fightStart, definition.Id, definition.Hp, definition.Hp,
                    player.Hp, player.HpMax, player.Sp, player.SpMax, StaminaDepleted: fDown < 1));
                foreach (var battleEvent in outcome.Events)
                {
                    events.Add(battleEvent with { AtSeconds = battleEvent.AtSeconds + fightStart });
                }
            }

            // 施放與喝補給次數不論事件是否保留都要計入
            foreach (var battleEvent in outcome.Events)
            {
                if (battleEvent is SkillEvent { Caster: CombatSide.Player })
                {
                    skillsCast++;
                }
                else if (battleEvent is PotionEvent)
                {
                    potionsUsed++;
                }
            }

            character = character with { Hp = outcome.Player.Hp, Sp = outcome.Player.Sp };

            long exp = 0, money = 0, jobExp = 0;
            IReadOnlyList<string> dropped = [];
            switch (outcome.Winner)
            {
                case CombatSide.Player:
                    wins++;
                    exp = RewardFormulas.Exp(definition, definition.Hp, character.Level, killed: true, fDown, random);
                    money = RewardFormulas.Money(definition, fDown, random);
                    jobExp = RewardFormulas.JobExp(definition, fDown);
                    // 擲骰順序：經驗 → 金錢 → 掉落，升級的擲骰在掉落之後
                    dropped = DropRoller.Roll(definition.Drops, fDown, random);
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
                    IReadOnlyList<DropResult> results = dropped.Count == 0
                        ? Array.Empty<DropResult>()
                        : dropped.Select(itemId => new DropResult(itemId, items(itemId).Name, 1, Discarded: false)).ToList();
                    events.Add(new RewardEvent(fightEnd, exp, money, jobExp, results));
                }

                droppedItemIds.AddRange(dropped);

                // 只有勝場計入任務；擊殺時刻以打完的時間判斷每日任務是否過期
                if (outcome.Winner == CombatSide.Player)
                {
                    var kill = KillCounter.Apply(missions, definition.Id, from.AddSeconds(fightEnd));
                    missions = kill.Missions;
                    missionKills += kill.Advanced.Count;
                    if (keepEvents)
                    {
                        events.AddRange(kill.Advanced.Select(mission => new MissionProgressEvent(
                            fightEnd, mission.MissionId, mission.Title, mission.MonsterId, mission.Progress, mission.Required)));
                    }
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

        var summary = new AdvanceSummary(fights, wins, losses, timeouts, expTotal, moneyTotal, jobExpTotal, levelsGained, skillsCast, PotionsUsed: potionsUsed, MissionKills: missionKills);
        IReadOnlyList<JobGain> jobGains = jobExpTotal > 0 ? [new JobGain(job.Id, jobExpTotal, jobExpTotal)] : [];
        return new AdventureRun(events, summary, character, stamina, jobGains, cursor, eventsCoverAll, droppedItemIds, supply?.Quantity, missions);
    }
}
