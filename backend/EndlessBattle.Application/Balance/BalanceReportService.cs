using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.Balance;

// 平衡報告：用內容與 Domain 的冒險模擬算出數字，只列數字、不判好壞。
// 參考角色：初階職業、固定配點、每級成長取期望值 (min + max) / 2 累計後捨去、地區推薦等級下限、
// 每欄穿得上的最高商店等級裝備、固定亂數種子、從 0 元開始冒險固定時數；每格分不帶技能與帶職業技能各算一次
public sealed class BalanceReportService
{
    public const int Seed = 20261104;
    public const double SimulatedHours = 8;
    public const int MaxReportedLevel = 100;

    // 同 BalanceTests 的最弱新角色配點
    public static readonly StatAllocation Allocation = new(Strength: 4, Intelligence: 4, Life: 1, Agility: 1);

    private static readonly DateTimeOffset Start = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

    private static readonly (string Stat, Func<BaseStats, int> Have, Func<EquipmentRequirement, int> Need)[] RequirementStats =
    [
        ("str", stats => stats.Strength, requires => requires.Strength),
        ("smart", stats => stats.Intelligence, requires => requires.Intelligence),
        ("agi", stats => stats.Agility, requires => requires.Agility),
        ("au", stats => stats.Charisma, requires => requires.Charisma),
    ];

    public BalanceReport Build(IGameContent content, BalanceReportOptions options)
    {
        var jobs = content.AllJobs.Where(job => job.Tier == CharacterFactory.StartingJobTier).ToList();
        var equipment = content.AllItems.Where(item => item.IsEquipment && item.ShopSellable).ToList();
        var shopLevels = equipment.Select(item => item.ShopLevel).Distinct().Order().ToList();

        return new BalanceReport(
            new BalanceAssumptions(Allocation, Seed, SimulatedHours, MaxReportedLevel, options.SkillLevel),
            shopLevels,
            WearLevels(content, jobs, equipment, shopLevels),
            Adventures(content, jobs, equipment, options.SkillLevel));
    }

    private static List<WearLevelRow> WearLevels(
        IGameContent content, IReadOnlyList<JobDefinition> jobs, IReadOnlyList<ItemDefinition> equipment, IReadOnlyList<int> shopLevels)
    {
        var rows = new List<WearLevelRow>();
        foreach (var job in jobs)
        {
            var start = Create(content, job).Base;
            foreach (var (stat, have, need) in RequirementStats)
            {
                var cells = shopLevels.Select(shopLevel =>
                {
                    var requirements = equipment
                        .Where(item => item.ShopLevel == shopLevel)
                        .Select(item => need(item.Requires))
                        .Where(value => value > 0)
                        .ToList();
                    if (requirements.Count == 0)
                    {
                        return new WearLevelCell(shopLevel, Required: null, Level: null);
                    }

                    var required = requirements.Min();
                    var level = Enumerable.Range(1, MaxReportedLevel)
                        .Cast<int?>()
                        .FirstOrDefault(candidate => have(StatsAt(start, job.Growth, candidate!.Value)) >= required);
                    return new WearLevelCell(shopLevel, required, level);
                }).ToList();
                rows.Add(new WearLevelRow(job.Id, job.Name, stat, cells));
            }
        }

        return rows;
    }

    private static List<AdventureRow> Adventures(
        IGameContent content, IReadOnlyList<JobDefinition> jobs, IReadOnlyList<ItemDefinition> equipment, BalanceSkillLevel skillLevel)
    {
        var rows = new List<AdventureRow>();
        foreach (var place in content.AllPlaces)
        {
            var monsters = content.MonstersOf(place.Id);
            if (monsters.Count == 0)
            {
                continue;
            }

            var level = Math.Max(1, place.Recommended.Min);
            foreach (var job in jobs)
            {
                var character = ReferenceAt(content, job, place, level);
                var gear = BestWearable(character, equipment);
                var skills = Loadout(content.SkillsOf(job.Id), skillLevel);
                rows.Add(new AdventureRow(
                    place.Id,
                    place.Name,
                    place.Restricted,
                    level,
                    job.Id,
                    job.Name,
                    gear.Select(item => item.ShopLevel).ToList(),
                    Simulate(content, job, place, monsters, character, gear, equipment, skills: []),
                    Simulate(content, job, place, monsters, character, gear, equipment, skills)));
            }
        }

        return rows;
    }

    private static AdventureResult Simulate(
        IGameContent content,
        JobDefinition job,
        PlaceDefinition place,
        IReadOnlyList<MonsterDefinition> monsters,
        Character character,
        IReadOnlyList<ItemDefinition> gear,
        IReadOnlyList<ItemDefinition> equipment,
        IReadOnlyList<EquippedSkill> skills)
    {
        var bonus = new EquipmentBonus(gear.Aggregate(StatBonus.Zero, (total, item) => total + item.Bonus));
        var derived = DerivedStats.From(character.Base, bonus);
        character = character with { Hp = derived.MaxHealthPoints, Sp = derived.MaxManaPoints };

        var run = AdventureSimulator.Run(
            character,
            job,
            place,
            monsters,
            content.GetItem,
            skills,
            Start,
            Start.AddHours(SimulatedHours),
            new SeededRandom(Seed),
            bonus);

        var summary = run.Summary;
        var netMoney = run.Character.Money - character.Money;
        var weaponTier = gear.FirstOrDefault(item => item.Category == ItemCategory.Weapon)?.ShopLevel ?? 0;
        var nextSet = equipment
            .Where(item => item.ShopLevel == weaponTier + 1)
            .GroupBy(item => item.Category)
            .Sum(group => group.Min(item => item.Price));
        var netPerHour = netMoney / SimulatedHours;
        return new AdventureResult(
            summary.Fights,
            summary.Wins,
            summary.Fights == 0 ? 0 : (double)summary.Wins / summary.Fights,
            summary.Losses,
            summary.Timeouts,
            summary.Exp / SimulatedHours,
            summary.Money / SimulatedHours,
            netPerHour,
            summary.LevelsGained,
            nextSet > 0 && netPerHour > 0 ? nextSet / netPerHour : null,
            NextTierUnaffordable: nextSet > 0 && netPerHour <= 0);
    }

    // 主動技能依內容順序放主動欄、被動放被動欄，超過欄位上限的不帶
    private static List<EquippedSkill> Loadout(IReadOnlyList<SkillDefinition> skills, BalanceSkillLevel skillLevel)
    {
        int LevelOf(SkillDefinition skill) => skillLevel == BalanceSkillLevel.First ? 1 : skill.MaxLevel;
        var active = skills.Where(skill => skill.Type == SkillType.Active).Take(SkillLoadout.ActiveSlots)
            .Select((skill, index) => new EquippedSkill(skill, LevelOf(skill), Slot: index + 1));
        var passive = skills.Where(skill => skill.Type == SkillType.Passive).Take(SkillLoadout.PassiveSlots)
            .Select((skill, index) => new EquippedSkill(skill, LevelOf(skill), Slot: SkillLoadout.ActiveSlots + index + 1));
        return active.Concat(passive).ToList();
    }

    private static Character ReferenceAt(IGameContent content, JobDefinition job, PlaceDefinition place, int level)
    {
        var character = Create(content, job);
        return character with
        {
            Level = level,
            NextExp = level == 1 ? character.NextExp : ExperienceCurve.NextExpFor(level),
            Base = StatsAt(character.Base, job.Growth, level),
            Money = 0,
            PlaceId = place.Id,
            SubPlace = place.HasSubPlaces ? place.SubPlaces[0].Index : null,
        };
    }

    // 每個分類取裸點穿得上的最高商店等級；同等級時取需求能力較高的那件（劍或杖、物防或魔防）
    private static List<ItemDefinition> BestWearable(Character character, IReadOnlyList<ItemDefinition> equipment) =>
        equipment
            .Where(item => EquipmentRules.CanEquip(character, item).Count == 0)
            .GroupBy(item => item.Category)
            .OrderBy(group => group.Key)
            .Select(group => group
                .OrderByDescending(item => item.ShopLevel)
                .ThenByDescending(item => RequirementStats.Where(stat => stat.Need(item.Requires) > 0).Sum(stat => stat.Have(character.Base)))
                .First())
            .ToList();

    private static Character Create(IGameContent content, JobDefinition job) => CharacterFactory.Create(
        accountId: 1,
        new CreateCharacterCommand("基準", Sex: 1, Element: 1, Birth: 1, job.Id, Allocation),
        job,
        content.StartingPlaceId,
        content.StartingSubPlace,
        Start);

    private static BaseStats StatsAt(BaseStats start, JobGrowth growth, int level)
    {
        var levelsGained = level - 1;
        return new BaseStats(
            Strength: start.Strength + Expected(growth.Strength, levelsGained),
            Intelligence: start.Intelligence + Expected(growth.Intelligence, levelsGained),
            Life: start.Life + Expected(growth.Life, levelsGained),
            Vitality: start.Vitality + Expected(growth.Vitality, levelsGained),
            Agility: start.Agility + Expected(growth.Agility, levelsGained),
            Charisma: start.Charisma + Expected(growth.Charisma, levelsGained),
            Faith: start.Faith + Expected(growth.Faith, levelsGained));
    }

    private static int Expected(StatRange range, int levelsGained) => levelsGained * (range.Minimum + range.Maximum) / 2;
}
