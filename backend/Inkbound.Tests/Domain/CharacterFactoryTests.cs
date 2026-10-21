using Inkbound.Domain.Characters;
using Inkbound.Domain.Combat;
using Inkbound.Domain.Content;

namespace Inkbound.Tests.Domain;

public class CharacterFactoryTests
{
    private const long AccountId = 7;
    private const string StartingStageId = "stage-1";
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly StatRange Flat = new(1, 1);
    private static readonly JobGrowth Growth = new(Flat, Flat, Flat, Flat, Flat, Flat, Flat);
    private static readonly JobDefinition Warrior = new("warrior", "戰士", 1, null, 1, 0, [], Growth);
    private static readonly JobDefinition Berserker = new("berserker", "狂戰士", 2, "warrior", 30, 3500, [new StatRequirement("str", 60)], Growth);

    private static CreateCharacterCommand Command(
        StatAllocation? allocation = null,
        int sex = 2,
        int element = 3,
        int birth = 4,
        string specialAttackName = "烈焰斬") =>
        new("墨客", sex, element, birth, "warrior", allocation ?? new StatAllocation(4, 2, 3, 1), specialAttackName);

    private static Character Create(CreateCharacterCommand command, JobDefinition? job = null) =>
        CharacterFactory.Create(AccountId, command, job ?? Warrior, StartingStageId, Now);

    [Fact]
    public void Create_AppliesAllocationAndDefaults()
    {
        var character = Create(Command());

        Assert.Equal(new BaseStats(Str: 14, Smart: 12, Life: 13, Vit: 10, Agi: 11, Au: 10, Be: 10), character.Base);
        Assert.Equal(50, character.Hp);
        Assert.Equal(25, character.Sp);
        Assert.Equal(2000, character.Money);
        Assert.Equal(1, character.Level);
        Assert.Equal(1000, character.NextExp);
        Assert.Equal(0, character.Exp);
        Assert.Equal("warrior", character.JobId);
        Assert.Equal(2, character.Sex);
        Assert.Equal(3, character.Element);
        Assert.Equal(4, character.Birth);
        Assert.Equal("烈焰斬", character.SpecialAttackName);
        Assert.Equal(AccountId, character.AccountId);
        Assert.Equal("墨客", character.Name);
        Assert.Equal(StartingStageId, character.CurrentStageId);
        Assert.Equal(Now, character.LastSettledAt);
        Assert.Equal(0, character.Version);
    }

    [Fact]
    public void Create_RejectsAllocationOverTen()
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(new StatAllocation(4, 3, 3, 1))));
    }

    [Theory]
    [InlineData(0, 3, 3, 3)]
    [InlineData(3, 0, 3, 3)]
    [InlineData(3, 3, 0, 3)]
    [InlineData(3, 3, 3, 0)]
    [InlineData(-1, 4, 4, 3)]
    public void Create_RejectsZeroInAnyStat(int str, int smart, int life, int agi)
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(new StatAllocation(str, smart, life, agi))));
    }

    [Fact]
    public void Create_AllowsUnderspent()
    {
        var character = Create(Command(new StatAllocation(2, 2, 2, 2)));

        Assert.Equal(new BaseStats(12, 12, 12, 10, 12, 10, 10), character.Base);
    }

    [Theory]
    [InlineData(3, 1, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 7, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 5)]
    public void Create_RejectsInvalidEnums(int sex, int element, int birth)
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(sex: sex, element: element, birth: birth)));
    }

    [Fact]
    public void Create_RejectsNonStartingJob()
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command() with { JobId = "berserker" }, Berserker));
    }

    [Theory]
    [InlineData("  烈焰斬  ", "烈焰斬")]
    [InlineData("斬", "斬")]
    [InlineData("一二三四五六七八", "一二三四五六七八")]
    public void Create_TrimsAndValidatesSpecialAttackName(string input, string expected)
    {
        Assert.Equal(expected, Create(Command(specialAttackName: input)).SpecialAttackName);
    }

    // 最後一筆：4 個組合 emoji 只算 4 字，但佔 20 個 code point，超過資料庫 VARCHAR(16)
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("一二三四五六七八九")]
    [InlineData("👨‍👩‍👧👨‍👩‍👧👨‍👩‍👧👨‍👩‍👧")]
    public void Create_RejectsInvalidSpecialAttackName(string input)
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(specialAttackName: input)));
    }

    [Fact]
    public void CombatBridge_MapsDerivedToCombatStats()
    {
        var combat = CombatBridge.ToCombatStats(new DerivedStats(At: 17, Mat: 12, Df: 10, Mdf: 11, HpMax: 425, SpMax: 91));

        Assert.Equal(new CombatStats(MaxHealth: 425, Attack: 17, Defense: 10, CritChance: 0.10, CritMultiplier: 1.2, AttacksPerSecond: 1.0), combat);
    }
}
