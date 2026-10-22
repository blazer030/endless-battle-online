using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
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
        TestItems.Item("stone-remover", ItemCategory.Material),
        TestItems.Item("safe-refine-stone", ItemCategory.Material),
        TestItems.Consumable("test-supply"),
    }.ToDictionary(item => item.Id);

    private static MissionObjective Objective(int level, long money, IReadOnlyList<ItemQuantity>? items = null, SkillReward? skill = null) =>
        DailyMissions.Resolve(
            new MissionDefinition(
                "m", "村長", "標題", "說明", "謝謝", level, 1, false, new KillTarget("test-sprite", 3), [],
                new MissionRequirements(), new MissionReward(money, items ?? [], skill), [], []),
            Hero.Id,
            null);

    private static MissionRewardOutcome Apply(
        Character character, MissionObjective objective, IReadOnlyList<InventoryEntry>? bag = null, IReadOnlyList<LearnedSkill>? learned = null) =>
        MissionRewards.Apply(character, Warrior, objective, bag ?? [], id => Items[id], learned ?? [], new SeededRandom(1));

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
        var bag = Enumerable.Range(1, 15).Select(id => TestItems.Entry(id, "stone-remover", 99)).ToList();

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
}
