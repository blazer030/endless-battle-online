using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Domain.Mercenaries;

public enum MercenaryFightResult
{
    Won,
    Lost,
    Timeout,
}

public sealed record MercenaryFight(DateTimeOffset At, string MonsterId, MercenaryFightResult Result, long Exp, long Money, IReadOnlyList<string> Drops);

// Contract 為 null：次數用完、已解除僱用。DroppedItemIds 依場次順序，尚未套用到背包；Missions 是結算後各擊殺任務的狀態
public sealed record MercenaryRun(
    Character Character,
    int LevelsGained,
    MercenaryContract? Contract,
    IReadOnlyList<MercenaryFight> Fights,
    long JobExp,
    IReadOnlyList<string> DroppedItemIds,
    IReadOnlyList<ActiveMission> Missions);

// WOG fire_mercenary：每 100 秒一場，以角色當下的能力滿 HP／SP 出戰，不用補給、不住宿、不捕捉
public static class MercenarySimulator
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(MercenaryRules.FightIntervalSeconds);

    // contract 必須是出擊中；placeMonsters 為該地區與全域怪，這裡排除全域怪、不分子地區
    public static MercenaryRun Run(
        Character character,
        JobDefinition job,
        MercenaryContract contract,
        MercenaryDefinition definition,
        IReadOnlyList<MonsterDefinition> placeMonsters,
        IReadOnlyList<EquippedSkill> skills,
        EquipmentBonus equipment,
        Pet? carriedPet,
        IReadOnlyList<ActiveMission> missions,
        DateTimeOffset now,
        IRandomSource random)
    {
        if (contract is not { PlaceId: { } placeId, NextFightAt: { } fightAt })
        {
            throw new InvalidOperationException("傭兵不在出擊中");
        }

        var ownMonsters = placeMonsters.Where(monster => monster.PlaceId != MonsterPool.GlobalPlaceId).ToList();
        var dropFactor = MercenaryRules.DropDenominatorFactor(definition.DropPercent);
        var fights = new List<MercenaryFight>();
        var droppedItemIds = new List<string>();
        var remaining = contract.Remaining;
        var levelsGained = 0;
        long jobExpTotal = 0;

        // 補算最多回溯 MaxSimulatedSeconds，更早的場次跳過：推到第一個不早於該時刻的格點
        var earliest = now.AddSeconds(-AdventureSimulator.MaxSimulatedSeconds);
        if (fightAt < earliest)
        {
            var skipped = ((earliest - fightAt).Ticks + Interval.Ticks - 1) / Interval.Ticks;
            fightAt = fightAt.AddTicks(skipped * Interval.Ticks);
        }

        for (; fightAt <= now && remaining > 0; fightAt += Interval)
        {
            var spawnable = KillCounter.SpawnableMissionIds(missions, fightAt);
            var monster = MonsterPool.Pick(ownMonsters, placeId, subPlace: null, random, spawnable);

            // NPC 不戰鬥、不扣次數、不列入場次，只推進任務（WOG 只在打完才扣次數）
            if (monster.Npc)
            {
                missions = KillCounter.Apply(missions, monster.Id, fightAt).Missions;
                continue;
            }

            var derived = DerivedStats.From(character.Base, equipment);
            var player = CombatBridge.ToCombatant(character with { Hp = derived.MaxHealthPoints, Sp = derived.MaxManaPoints }, equipment);
            var opponent = CombatantState.From(monster);
            var pet = carriedPet is null ? null : PetBattle.ToCombatant(carriedPet, player, opponent);
            var outcome = WogBattle.Run(player, opponent, playerPreferred: null, skills, supply: null, random, pet, monsterSkills: monster.Skills);

            long exp = 0, money = 0;
            IReadOnlyList<string> dropped = [];
            MercenaryFightResult result;
            switch (outcome.Winner)
            {
                case CombatSide.Player:
                    result = MercenaryFightResult.Won;
                    exp = MercenaryRules.Discount(
                        RewardFormulas.Exp(monster, monster.HealthPoints, character.Level, killed: true, random), definition.ExpPercent);
                    money = MercenaryRules.Discount(RewardFormulas.Money(monster, random), definition.MoneyPercent);
                    // 熟練度不打折（PHP win_check）
                    jobExpTotal += RewardFormulas.JobExp(monster);
                    dropped = DropRoller.Roll(monster.Drops, random, dropFactor);
                    missions = KillCounter.Apply(missions, monster.Id, fightAt).Missions;
                    remaining--;
                    break;
                case CombatSide.Monster:
                    result = MercenaryFightResult.Lost;
                    remaining -= definition.DeathCost;
                    break;
                default:
                    // PHP 超限 win=0 一律算敗，但仍給部分經驗
                    result = MercenaryFightResult.Timeout;
                    exp = MercenaryRules.Discount(
                        RewardFormulas.TimeoutExp(monster, outcome.DamageDealtToMonster, character.Level, random), definition.ExpPercent);
                    remaining -= definition.DeathCost;
                    break;
            }

            fights.Add(new MercenaryFight(fightAt, monster.Id, result, exp, money, dropped));
            droppedItemIds.AddRange(dropped);
            character = character with { Exp = character.Exp + exp, Money = character.Money + money };
            var levelUp = LevelUp.Apply(character, job, random, atSeconds: 0);
            character = levelUp.Character;
            levelsGained += levelUp.Events.Count;
        }

        var next = remaining > 0 ? contract with { Remaining = remaining, NextFightAt = fightAt } : null;
        return new MercenaryRun(character, levelsGained, next, fights, jobExpTotal, droppedItemIds, missions);
    }
}
