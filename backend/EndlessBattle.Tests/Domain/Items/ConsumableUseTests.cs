using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Items;

public class ConsumableUseTests
{
    private static readonly ItemDefinition BagExpansion = TestItems.Consumable("bag", ConsumableKind.BagExpansion) with { Amount = 5 };
    private static readonly ItemDefinition DepotExpansion = TestItems.Consumable("depot", ConsumableKind.DepotExpansion) with { Amount = 5 };

    private static readonly StatRange One = new(1, 1);

    private static readonly JobDefinition Warrior =
        new("warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [], new JobGrowth(One, One, One, One, One, One, One));

    private static UseContext Context(Character character) => new(character, Warrior, 0, [], []);

    // 擴充只在加成會變大時可用；目前已 ≥ 擴充格數 → noEffect
    [Theory]
    [InlineData(4, new string[0])]
    [InlineData(5, new[] { "noEffect" })]
    [InlineData(8, new[] { "noEffect" })]
    public void CanUse_ExpansionNeedsEffect(int bonus, string[] expected)
    {
        var character = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch) with { BagBonus = bonus, DepotBonus = bonus };

        Assert.Equal(expected, ConsumableUse.CanUse(Context(character), BagExpansion));
        Assert.Equal(expected, ConsumableUse.CanUse(Context(character), DepotExpansion));
    }

    [Fact]
    public void CanUse_RejectsSupply()
    {
        var character = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);

        Assert.Equal(["notUsable"], ConsumableUse.CanUse(Context(character), TestItems.Consumable("supply")));
    }

    // 魔石寶箱的等級：攻擊／魔力／守護／結界 1，生命／疾風 2，混沌／虹彩 3；boxClass 0 不會從寶箱出現
    private static readonly ItemDefinition[] Stones =
    [
        Stone("attack-stone", 1), Stone("sorcery-stone", 1), Stone("plain-stone", 0), Stone("guard-stone", 1), Stone("ward-stone", 1),
        Stone("vitality-stone", 2), Stone("gale-stone", 2), Stone("chaos-stone", 3), Stone("prism-stone", 3),
    ];

    private static readonly ItemDefinition[] RefineStones =
    [
        .. Enumerable.Range(1, 3).SelectMany(level => Enumerable.Range(1, 3).Select(kind => RefineStone(kind, level))),
    ];

    private static readonly ItemDefinition[] AllItems = [.. Stones, .. RefineStones];

    private static readonly Character Hero = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);

    private static ItemDefinition Stone(string id, int boxClass) =>
        TestItems.Item(id, ItemCategory.Stone) with { Stone = new StoneSpec(false, StatBonus.Zero with { At = 1 }, null, null, boxClass) };

    private static ItemDefinition RefineStone(int kind, int level) =>
        TestItems.Item($"kind{kind}-refine-stone-{level}", ItemCategory.RefineStone) with
        {
            Refine = new RefineStoneSpec(level, kind, null, null, null, null, null, null, null, null, null, null, null),
        };

    private static MonsterDefinition Monster(string id, string? missionId = null) =>
        new(id, $"怪物{id}", Level: 1, Hp: 10, Sp: 1, At: 1, Mat: 1, Df: 1, Mdf: 1, Agi: 1, Au: 1, Element: 0, JobExp: 1, Meet: 0, MissionId: missionId);

    private static UseContext Full(Character character, int petCount = 0, IReadOnlyList<MonsterDefinition>? monsters = null) =>
        new(character, Warrior, petCount, AllItems, monsters ?? []);

    private static ItemDefinition Usable(ConsumableKind kind, int amount) => TestItems.Consumable($"use-{kind}", kind) with { Amount = amount };

    // ScriptedRandom 的值 v 對 rand(1, 100) 取 ⌊100v⌋ + 1、對 NextInt(n) 取 ⌊nv⌋。
    // r = 61 → 等級 2：候選依內容順序 攻擊、魔力、守護、結界、生命、疾風（6 顆）；第一顆 ⌊6 × 0.9⌋ = 5 → 疾風，
    // 第二顆從剩下 5 顆抽 ⌊5 × 0.99⌋ = 4 → 生命（若沒移除，6 顆時會再抽到疾風）
    [Fact]
    public void StoneBox_RollsClassThenDistinctStones()
    {
        var random = new ScriptedRandom(0.0, 0.605, 0.9, 0.99);

        var outcome = ConsumableUse.Apply(Full(Hero), Usable(ConsumableKind.StoneBox, 2), Now, random);

        Assert.Equal(["gale-stone", "vitality-stone"], outcome.GrantedItemIds);
        Assert.Equal(3, random.Consumed);
        Assert.Equal(Hero, outcome.Character);
        Assert.Null(outcome.Pet);
    }

    // r = 91 → 等級 3：8 顆候選，⌊8 × 0.99⌋ = 7 → 虹彩；r = 90 仍是等級 2：⌊6 × 0.99⌋ = 5 → 疾風；r = 60 只有等級 1
    [Fact]
    public void StoneBox_ClassThreeIncludesRandomStones()
    {
        var box = Usable(ConsumableKind.StoneBox, 1);

        Assert.Equal(["prism-stone"], ConsumableUse.Apply(Full(Hero), box, Now, new ScriptedRandom(0.0, 0.905, 0.99)).GrantedItemIds);
        Assert.Equal(["gale-stone"], ConsumableUse.Apply(Full(Hero), box, Now, new ScriptedRandom(0.0, 0.895, 0.99)).GrantedItemIds);
        Assert.Equal(["ward-stone"], ConsumableUse.Apply(Full(Hero), box, Now, new ScriptedRandom(0.0, 0.595, 0.99)).GrantedItemIds);
    }

    // 精鍊寶盒 amount 3：Lv3 的三種精鍊石中 ⌊3 × 0.5⌋ = 1 → 種類 2
    [Fact]
    public void RefineBox_PicksStoneOfItsLevel()
    {
        var random = new ScriptedRandom(0.0, 0.5);

        var outcome = ConsumableUse.Apply(Full(Hero), Usable(ConsumableKind.RefineBox, 3), Now, random);

        Assert.Equal(["kind2-refine-stone-3"], outcome.GrantedItemIds);
        Assert.Equal(1, random.Consumed);
    }

    [Fact]
    public void PetEgg_RejectedWhenPetsFull()
    {
        var egg = Usable(ConsumableKind.PetEgg, 280);

        Assert.Equal(["petsFull"], ConsumableUse.CanUse(Full(Hero, petCount: PetRules.MaxPets), egg));
        Assert.Empty(ConsumableUse.CanUse(Full(Hero, petCount: PetRules.MaxPets - 1), egg));
    }

    // 物種先擲：任務怪不算，⌊2 × 0.5⌋ = 1 → 第二隻非任務怪；接著 NewPet：攻 rand(1,10) = 1、魔 10、防 5 各 ×280，個性 rand(1,4) = 2
    [Fact]
    public void PetEgg_UsesWogMultiplier()
    {
        MonsterDefinition[] monsters = [Monster("first"), Monster("quest", missionId: "hunt"), Monster("second")];
        var random = new ScriptedRandom(0.0, 0.5, 0.0, 0.95, 0.45, 0.3);

        var outcome = ConsumableUse.Apply(Full(Hero, monsters: monsters), Usable(ConsumableKind.PetEgg, 280), Now, random);

        Assert.Equal(new CapturedPet("second", "怪物second", PetPersonality.Cold, 280, 2800, 1400, Now), outcome.Pet);
        Assert.Equal(5, random.Consumed);
        Assert.Equal(Hero, outcome.Character);
        Assert.Empty(outcome.GrantedItemIds);
    }

    // 技能點加在目前職業（Application 依角色的 JobId 寫入）；角色本身不變
    [Fact]
    public void ProficiencyBook_AddsSkillPointsToCurrentJob()
    {
        var outcome = ConsumableUse.Apply(Full(Hero), Usable(ConsumableKind.ProficiencyBook, 300), Now, new ScriptedRandom(0.0));

        Assert.Equal(300, outcome.SkillPoints);
        Assert.Equal(Hero, outcome.Character);
        Assert.Equal(0, outcome.LevelsGained);
    }

    // Lv1（exp 0／1000）加 20000 → 立刻升級，剩下的經驗小於下一級所需
    [Fact]
    public void ExperienceCapsule_LevelsUpImmediately()
    {
        var outcome = ConsumableUse.Apply(Full(Hero), Usable(ConsumableKind.ExperienceCapsule, 20000), Now, new ScriptedRandom(0.0));

        Assert.True(outcome.LevelsGained >= 1);
        Assert.Equal(1 + outcome.LevelsGained, outcome.Character.Level);
        Assert.True(outcome.Character.Exp < outcome.Character.NextExp);
        Assert.Equal(0, outcome.SkillPoints);
    }

    [Fact]
    public void CanUse_NewConsumablesAreUsable()
    {
        Assert.All(
            new[] { ConsumableKind.StoneBox, ConsumableKind.RefineBox, ConsumableKind.ProficiencyBook, ConsumableKind.ExperienceCapsule },
            kind => Assert.Empty(ConsumableUse.CanUse(Full(Hero, petCount: PetRules.MaxPets), Usable(kind, 1))));
    }

    // 密藥從現在起算 Seconds 秒；不看先前同種的密藥（到期時刻不累加），由 repository 以同種覆蓋。角色不變、不擲骰
    [Fact]
    public void Elixir_StartsNowAndReplacesSameKind()
    {
        var elixir = TestItems.Consumable("exp-elixir", ConsumableKind.Elixir) with { Elixir = new ElixirSpec(BuffKind.Exp, 1.5, 3600) };
        var random = new ScriptedRandom(0.0);

        Assert.Empty(ConsumableUse.CanUse(Full(Hero), elixir));
        var outcome = ConsumableUse.Apply(Full(Hero), elixir, Now, random);

        Assert.Equal(new CharacterBuff(BuffKind.Exp, 1.5, Now, Now.AddSeconds(3600)), outcome.Buff);
        Assert.Equal(Buffs.Start(elixir.Elixir!, Now), outcome.Buff);
        Assert.Equal(Hero, outcome.Character);
        Assert.Equal(0, random.Consumed);
    }
}
