using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Rest;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Domain.Adventure;

public static class AdventureSimulator
{
    public const double CatchUpWindowSeconds = 30;
    public const double MaxSimulatedSeconds = 86_400;

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
        IReadOnlyList<ActiveMission>? missions = null,
        AdventureCompanions? companions = null,
        IReadOnlyList<CharacterBuff>? buffs = null)
    {
        missions ??= [];
        // 密藥依每場的開打時刻判斷是否生效，不擲骰；沒有密藥時擲骰序列不變
        buffs ??= [];
        var carriedPet = companions?.CarriedPet;
        var capture = companions?.Capture;
        var petCount = companions?.PetCount ?? 0;
        var captured = new List<CapturedPet>();
        // 裝備在整段推進中不變；null 表示沒有裝備
        var bonus = equipment ?? EquipmentBonus.Zero;
        var totalSeconds = (to - from).TotalSeconds;
        var windowStart = totalSeconds - CatchUpWindowSeconds;
        var events = new List<BattleEvent>();
        var droppedItemIds = new List<string>();
        var eventsCoverAll = true;
        int fights = 0, wins = 0, losses = 0, timeouts = 0, escapes = 0, levelsGained = 0, skillsCast = 0, potionsUsed = 0, missionKills = 0;
        long expTotal = 0, moneyTotal = 0, jobExpTotal = 0;
        var cursor = 0.0;
        const double fDown = 1;

        // 只要還有可用時間就開新的一場，開打的戰鬥一律打完（可能超過可用時間）
        while (cursor < totalSeconds)
        {
            // 事件以整場為單位取捨；窗口外的場次照樣結算，只是不輸出事件
            var keepEvents = cursor >= windowStart;

            // 死亡、HP 低於門檻，或（有主動技能時）SP 低於門檻，先住宿再開打；被動不耗 SP，不看 SP 門檻
            if (RestRules.ShouldRest(character, DerivedStats.From(character.Base, bonus), skills.Any(skill => skill.Skill.Type == SkillType.Active)))
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

            // 任務怪只在其任務尚未達成且未過期時出現；任務邏輯不擲骰，沒有任務怪時擲骰序列不變
            var fightStartsAt = from.AddSeconds(cursor);
            var spawnable = KillCounter.SpawnableMissionIds(missions, fightStartsAt);
            var definition = MonsterPool.Pick(placeMonsters, character.PlaceId, character.SubPlace, random, spawnable);
            var player = Buffs.ApplyTo(CombatBridge.ToCombatant(character, bonus), buffs, fightStartsAt);
            var monster = CombatantState.From(definition);
            // 寵物每場重新建立，代擋量不帶到下一場
            var pet = carriedPet is null ? null : PetBattle.ToCombatant(carriedPet, player, monster);
            // 補給的剩餘數量一場接一場帶下去；每場的 9 次上限由戰鬥自己重新計算
            var outcome = WogBattle.Run(player, monster, character.Settings.Attack, skills, supply, random, pet, character.Settings.EscapeHpPercent);
            supply = outcome.Supply;
            var fightStart = cursor;
            var fightEnd = fightStart + outcome.Events[^1].AtSeconds;
            fights++;
            // 只有略去戰鬥才算事件不完整；略去的若只是住宿，前端從戰鬥事件就能接上
            eventsCoverAll &= keepEvents;

            if (keepEvents)
            {
                events.Add(new EncounterEvent(fightStart, definition.Id, definition.Hp, definition.Hp,
                    player.Hp, player.HpMax, player.Sp, player.SpMax, StaminaDepleted: false, carriedPet?.Name, carriedPet?.MonsterId));
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
                case null when outcome.Escaped:
                    // 逃跑：計入敗場、沒有任何報酬；HP 不歸零，不算死亡
                    losses++;
                    escapes++;
                    break;
                case CombatSide.Player:
                    wins++;
                    // 密藥只乘勝場的經驗與熟練度，在擲骰之後套用
                    exp = Buffs.Scale(
                        RewardFormulas.Exp(definition, definition.Hp, character.Level, killed: true, fDown, random),
                        Buffs.Multiplier(buffs, BuffKind.Exp, fightStartsAt));
                    money = RewardFormulas.Money(definition, fDown, random);
                    jobExp = Buffs.Scale(RewardFormulas.JobExp(definition, fDown), Buffs.Multiplier(buffs, BuffKind.JobExp, fightStartsAt));
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
                    exp = RewardFormulas.TimeoutExp(definition, outcome.DamageDealtToMonster, character.Level, fDown, random);
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

                // 捕捉在掉落之後、任務與升級之前；攜帶寵物時不捕捉。這次捕到的也算進寵物上限
                if (outcome.Winner == CombatSide.Player && carriedPet is null && capture is { Quantity: > 0 })
                {
                    var roll = PetCapture.Roll(capture, definition, petCount + captured.Count, from.AddSeconds(fightEnd), random);
                    capture = roll.Capture;
                    if (roll.Captured is not null)
                    {
                        captured.Add(roll.Captured);
                    }

                    if (keepEvents && (roll.ItemBroken || roll.Captured is not null))
                    {
                        events.Add(new CaptureEvent(fightEnd, capture.Name, roll.ItemBroken, roll.Captured?.MonsterId, capture.Quantity));
                    }
                }

                // 只有勝場計入任務；擊殺時刻以打完的時間判斷每日任務是否過期
                if (outcome.Winner == CombatSide.Player)
                {
                    var kill = KillCounter.Apply(missions, definition.Id, from.AddSeconds(fightEnd));
                    missions = kill.Missions;
                    missionKills += kill.Advanced.Count;
                    if (keepEvents)
                    {
                        events.AddRange(kill.Advanced.Select(mission => new MissionProgressEvent(
                            fightEnd, mission.MissionId, mission.Title, mission.MonsterId, mission.Progress, mission.Required,
                            mission.RequiresDelivery)));
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

            cursor += outcome.Rounds * BattleTiming.SecondsPerRound + BattleTiming.FightIntervalSeconds;
        }

        var summary = new AdvanceSummary(fights, wins, losses, timeouts, expTotal, moneyTotal, jobExpTotal, levelsGained, skillsCast, PotionsUsed: potionsUsed, MissionKills: missionKills, PetsCaptured: captured.Count, Escapes: escapes);
        IReadOnlyList<JobGain> jobGains = jobExpTotal > 0 ? [new JobGain(job.Id, jobExpTotal, jobExpTotal)] : [];
        return new AdventureRun(
            events, summary, character, stamina, jobGains, cursor, eventsCoverAll, droppedItemIds, supply?.Quantity ?? capture?.Quantity, missions, captured);
    }
}
