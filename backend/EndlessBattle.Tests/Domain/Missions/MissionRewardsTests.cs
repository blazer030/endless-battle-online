using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Missions;

public class MissionRewardsTests
{
    // 預設角色 Lv1、Exp 0、NextExp 1000
    private static readonly Character Hero = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with { Id = 7 };

    private static readonly StatRange One = new(1, 1);

    private static readonly JobDefinition Warrior =
        new("warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [], new JobGrowth(One, One, One, One, One, One, One));

    private static readonly Dictionary<string, ItemDefinition> Items = new[]
    {
        TestItems.Item("test-ore", ItemCategory.Material),
        TestItems.Item("item-1378", ItemCategory.Material),
        TestItems.Item("safe-refine-stone", ItemCategory.Material),
        TestItems.Consumable("test-supply"),
    }.ToDictionary(item => item.Id);

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static readonly MonsterDefinition Wolf = new("test-wolf", "測試寒狼", 5, 95, 10, 20, 6, 9, 5, 13, 7, 2, 18, 30, 1, [1, 3], []);

    private static MissionObjective Objective(
        int level, long money, IReadOnlyList<ItemQuantity>? items = null, SkillReward? skill = null, PetReward? pet = null) =>
        DailyMissions.Resolve(
            new MissionDefinition(
                "m", "村長", "標題", "說明", "謝謝", level, 1, false, [new KillTarget("test-sprite", 3)], [],
                new MissionRequirements(), new MissionReward(money, items ?? [], skill, pet), [], []),
            Hero.Id,
            null);

    private static MissionRewardOutcome Apply(
        Character character,
        MissionObjective objective,
        IReadOnlyList<InventoryEntry>? bag = null,
        IReadOnlyList<LearnedSkill>? learned = null,
        int petCount = 0,
        IRandomSource? random = null) =>
        MissionRewards.Apply(
            character, Warrior, objective, bag ?? [], id => Items[id], _ => Wolf, learned ?? [], petCount, Now, random ?? new SeededRandom(1));

    [Fact]
    public void Apply_GrantsLevelTimes190AndMoney()
    {
        var veteran = Hero with { Level = 20, Exp = 100, NextExp = 1_000_000 };

        var outcome = Apply(veteran, Objective(level: 15, money: 750));

        Assert.Empty(outcome.Reasons);
        Assert.Equal(100 + 2850, outcome.Character.Exp);
        Assert.Equal(veteran.Money + 750, outcome.Character.Money);
        Assert.Equal(0, outcome.LevelsGained);
    }

    // 900 + 190 = 1090 > 1000 → Lv2、剩 90；成長每項固定 +1
    [Fact]
    public void Apply_LevelsUpImmediately()
    {
        var outcome = Apply(Hero with { Exp = 900 }, Objective(level: 1, money: 0));

        Assert.Equal(1, outcome.LevelsGained);
        Assert.Equal((2, 90L, 2184L), (outcome.Character.Level, outcome.Character.Exp, outcome.Character.NextExp));
        Assert.Equal(Hero.Base.Str + 1, outcome.Character.Base.Str);
    }

    // 材料分類 15 格滿、沒有可疊的 safe-refine-stone
    [Fact]
    public void Apply_RejectsWhenRewardItemsDoNotFit()
    {
        var bag = Enumerable.Range(1, 15).Select(id => TestItems.Entry(id, "item-1378", 99)).ToList();

        var outcome = Apply(Hero, Objective(level: 1, money: 50, [new ItemQuantity("safe-refine-stone", 1)]), bag);

        Assert.Equal(["bagFull"], outcome.Reasons);
    }

    // 先疊到同種道具上再開新格；同一次獎勵裡後面的道具可以疊到前面新開的格子
    [Fact]
    public void Apply_AddsRewardItemsInOrder()
    {
        IReadOnlyList<InventoryEntry> bag = [TestItems.Entry(1, "test-ore", 98)];
        var objective = Objective(level: 1, money: 0,
            [new ItemQuantity("test-ore", 3), new ItemQuantity("test-supply", 2), new ItemQuantity("test-ore", 1)]);

        var outcome = Apply(Hero, objective, bag);

        Assert.Empty(outcome.Reasons);
        Assert.Equal([TestItems.Entry(1, "test-ore", 99)], outcome.UpdatedEntries);
        Assert.Equal([TestItems.Entry(0, "test-ore", 3), TestItems.Entry(0, "test-supply", 2)], outcome.NewEntries);
    }

    [Fact]
    public void Apply_GrantsSkillLevelOne()
    {
        var outcome = Apply(Hero, Objective(level: 1, money: 0, skill: new SkillReward("iron-skin", 1)), learned: [new LearnedSkill("power-strike", 3)]);

        Assert.Equal(new LearnedSkill("iron-skin", 1), outcome.GrantedSkill);
    }

    // 已會的技能略過，金錢與經驗照給
    [Fact]
    public void Apply_SkipsKnownSkill()
    {
        var outcome = Apply(Hero, Objective(level: 1, money: 250, skill: new SkillReward("iron-skin", 1)), learned: [new LearnedSkill("iron-skin", 2)]);

        Assert.Empty(outcome.Reasons);
        Assert.Null(outcome.GrantedSkill);
        Assert.Equal((Hero.Exp + 190, Hero.Money + 250), (outcome.Character.Exp, outcome.Character.Money));
    }

    // ScriptedRandom 的 rand(1, n) = ⌊擲骰 × n⌋ + 1：0.45 → rand(1,10) 5、0.0 → 個性 1 積極；不升級所以不擲成長
    [Fact]
    public void Apply_PetRewardRollsStatsTimesLevel()
    {
        var random = new ScriptedRandom(0.0, 0.45, 0.45, 0.45, 0.0);

        var outcome = Apply(Hero, Objective(level: 1, money: 0, pet: new PetReward("test-wolf", 2)), random: random);

        Assert.Empty(outcome.Reasons);
        var pet = outcome.NewPet!;
        Assert.Equal(("test-wolf", "測試寒狼", PetPersonality.Aggressive), (pet.MonsterId, pet.Name, pet.Personality));
        Assert.Equal((10, 10, 10), (pet.Attack, pet.Magic, pet.Defense));
        Assert.Equal(Now, pet.CapturedAt);
        Assert.Equal(4, random.Consumed);
    }

    // 900 + 190 > 1000 升一級：先擲 7 個成長（0.95，範圍都是 1～1），再擲寵物 at／mt／def 與個性（fallback 0.0 → 1 × 等級 2 = 2）。
    // 順序反過來的話寵物會拿到 0.95 → rand(1,10) = 10
    [Fact]
    public void Reward_RollsLevelUpBeforePetStats()
    {
        var random = new ScriptedRandom(0.0, 0.95, 0.95, 0.95, 0.95, 0.95, 0.95, 0.95);

        var outcome = Apply(Hero with { Exp = 900 }, Objective(level: 1, money: 0, pet: new PetReward("test-wolf", 2)), random: random);

        Assert.Equal(1, outcome.LevelsGained);
        var pet = outcome.NewPet!;
        Assert.Equal((2, 2, 2, PetPersonality.Aggressive), (pet.Attack, pet.Magic, pet.Defense, pet.Personality));
        Assert.Equal(11, random.Consumed);
    }

    [Fact]
    public void Apply_PetRewardRejectedWhenPetsFull()
    {
        var objective = Objective(level: 1, money: 50, [new ItemQuantity("safe-refine-stone", 1)], pet: new PetReward("test-wolf", 2));
        var fullBag = Enumerable.Range(1, 15).Select(id => TestItems.Entry(id, "item-1378", 99)).ToList();

        Assert.Equal(["petsFull"], Apply(Hero, objective, petCount: PetRules.MaxPets).Reasons);
        Assert.Equal(["bagFull", "petsFull"], Apply(Hero, objective, fullBag, petCount: PetRules.MaxPets).Reasons);
        Assert.Empty(Apply(Hero, objective, petCount: PetRules.MaxPets - 1).Reasons);
    }

    [Fact]
    public void Apply_WithoutPetRewardHasNoPet()
    {
        Assert.Null(Apply(Hero, Objective(level: 1, money: 0), petCount: PetRules.MaxPets).NewPet);
        Assert.Empty(Apply(Hero, Objective(level: 1, money: 0), petCount: PetRules.MaxPets).Reasons);
    }
}
