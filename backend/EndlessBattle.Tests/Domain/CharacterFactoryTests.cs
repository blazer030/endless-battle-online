using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain;

public class CharacterFactoryTests
{
    private const long AccountId = 7;
    private const int StartingPlaceId = 1;
    private const int StartingSubPlace = 1;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly StatRange Flat = new(1, 1);
    private static readonly JobGrowth Growth = new(Flat, Flat, Flat, Flat, Flat, Flat, Flat);
    private static readonly JobDefinition Warrior = new("warrior", "戰士", 1, null, 1, 0, [], Growth);
    private static readonly JobDefinition Berserker = new("berserker", "狂戰士", 2, "warrior", 30, 3500, [new StatRequirement(BaseStat.Strength, 60)], Growth);

    private static CreateCharacterCommand Command(
        StatAllocation? allocation = null,
        int sex = 2,
        int element = 3,
        int birth = 4) =>
        new("墨客", sex, element, birth, "warrior", allocation ?? new StatAllocation(4, 2, 3, 1));

    private static Character Create(CreateCharacterCommand command, JobDefinition? job = null) =>
        CharacterFactory.Create(AccountId, command, job ?? Warrior, StartingPlaceId, StartingSubPlace, Now);

    [Fact]
    public void Create_AppliesAllocationAndDefaults()
    {
        var character = Create(Command());

        Assert.Equal(new BaseStats(Strength: 14, Intelligence: 12, Life: 13, Vitality: 10, Agility: 11, Charisma: 10, Faith: 10), character.Base);
        Assert.Equal(50, character.HealthPoints);
        Assert.Equal(25, character.ManaPoints);
        Assert.Equal(2000, character.Money);
        Assert.Equal(1, character.Level);
        Assert.Equal(1000, character.NextLevelExperience);
        Assert.Equal(0, character.Experience);
        Assert.Equal("warrior", character.JobId);
        Assert.Equal(2, character.Sex);
        Assert.Equal(3, character.Element);
        Assert.Equal(4, character.Birth);
        Assert.Equal(AccountId, character.AccountId);
        Assert.Equal("墨客", character.Name);
        Assert.Equal(Now, character.LastSettledAt);
        Assert.Equal(0, character.Version);
    }

    // 起始地由呼叫端從內容檔傳入：1 中央平原、子地區 1 香茅綠地
    [Fact]
    public void Factory_StartsAtCentralPlainsSubOne()
    {
        var character = Create(Command());

        Assert.Equal(1, character.PlaceId);
        Assert.Equal(1, character.SubPlace);
    }

    [Fact]
    public void Create_RejectsAllocationOverTen()
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(new StatAllocation(4, 3, 3, 1))));
    }

    [Fact]
    public void Create_RejectsAllocationThatOverflows()
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command(new StatAllocation(int.MaxValue, int.MaxValue, 1, 1))));
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

    [Theory]
    [InlineData(1, 1, 1, 3)]
    [InlineData(4, 2, 3, 2)]
    public void Create_RejectsUnspentPoints(int str, int smart, int life, int agi)
    {
        var exception = Assert.Throws<CharacterCreationException>(() => Create(Command(new StatAllocation(str, smart, life, agi))));

        Assert.Equal("能力點數必須剛好分完 10 點", exception.Message);
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

    [Theory]
    [InlineData(2, 6, 4)]
    [InlineData(1, 1, 1)]
    public void Create_AcceptsEnumBounds(int sex, int element, int birth)
    {
        var character = Create(Command(sex: sex, element: element, birth: birth));

        Assert.Equal((sex, element, birth), (character.Sex, character.Element, character.Birth));
    }

    [Fact]
    public void Create_RejectsNonStartingJob()
    {
        Assert.Throws<CharacterCreationException>(() => Create(Command() with { JobId = "berserker" }, Berserker));
    }

    // TestCharacters.Default：Base (12,12,12,10,12,10,10) → At 12、Mat 12、Df 10、Mdf 10、
    // HpMax 12×25+50 = 350、SpMax (12+10)×3+25 = 91；戰鬥從目前的 Hp 60／Sp 25 開始
    [Fact]
    public void CombatBridge_MapsCharacterToCombatant()
    {
        var character = TestCharacters.Default(AccountId, "測試者", StartingPlaceId, StartingSubPlace, Now);

        var combatant = CombatBridge.ToCombatant(character);

        Assert.Equal(
            new CombatantState(HealthPoints: 60, MaxHealthPoints: 350, ManaPoints: 25, MaxManaPoints: 91, Attack: 12, MagicAttack: 12, Defense: 10, MagicDefense: 10, Agility: 12, Charisma: 10, Element: 1, Strength: 12, Intelligence: 12, Vitality: 10, Faith: 10, Level: 1),
            combatant);
    }

    // 加成同 DerivedStatsTests：有效 (14,13,16,13,17,16,12)；敏捷、魅力、力量、智力都用有效值
    [Fact]
    public void CombatBridge_UsesEquipmentBonus()
    {
        var character = TestCharacters.Default(AccountId, "測試者", StartingPlaceId, StartingSubPlace, Now);
        var bonus = new EquipmentBonus(new StatBonus(
            Strength: 2, Intelligence: 1, Life: 4, Vitality: 3, Agility: 5, Charisma: 6, Faith: 2, Attack: 28, MagicAttack: 7, Defense: 9, MagicDefense: 11, MaxHealthPoints: 50));

        var combatant = CombatBridge.ToCombatant(character, bonus);

        Assert.Equal(
            new CombatantState(HealthPoints: 60, MaxHealthPoints: 500, ManaPoints: 25, MaxManaPoints: 100, Attack: 42, MagicAttack: 20, Defense: 22, MagicDefense: 23, Agility: 17, Charisma: 16, Element: 1, Strength: 14, Intelligence: 13, Vitality: 13, Faith: 12, Level: 1),
            combatant);
    }

    // 裝備的 agi +5、au +5 要帶進戰鬥：怪物 agi 15、au 12。
    // 先攻：有效 agi 17 > 15 → 門檻 70，擲 0.5 → 51 ≤ 70 玩家先（裸點 12 時門檻 30，怪物先）；
    // 魔法暴擊比 au：15/12 → 機率 12.5，擲 0.11 → 12 < 12.5 暴擊（裸點 10 ≤ 12 時機率 10，不暴擊）
    [Fact]
    public void CombatBridge_CarriesEquipmentAgiAndAuIntoBattle()
    {
        var character = TestCharacters.Default(AccountId, "測試者", StartingPlaceId, StartingSubPlace, Now);
        var bonus = new EquipmentBonus(StatBonus.Zero with { Agility = 5, Charisma = 5 });
        var monster = new CombatantState(
            HealthPoints: 1, MaxHealthPoints: 1, ManaPoints: 0, MaxManaPoints: 0, Attack: 0, MagicAttack: 0, Defense: 1, MagicDefense: 1, Agility: 15, Charisma: 12, Element: 0, Strength: 0, Intelligence: 0);

        var outcome = WogBattle.Run(CombatBridge.ToCombatant(character, bonus), monster, AttackKind.Magic, new ScriptedRandom(0.99, 0.5, 0.11));

        var first = outcome.Events.OfType<AttackEvent>().First();
        Assert.Equal((CombatSide.Player, AttackKind.Magic, true), (first.Attacker, first.Kind, first.Critical));
    }

    // 卸下加 life／smart／be 的裝備後 Hp／Sp 可能高於新上限：進戰鬥時夾回 350／91
    [Fact]
    public void CombatBridge_ClampsHpToEffectiveMax()
    {
        var character = TestCharacters.Default(AccountId, "測試者", StartingPlaceId, StartingSubPlace, Now) with { HealthPoints = 850, ManaPoints = 200 };

        var combatant = CombatBridge.ToCombatant(character, EquipmentBonus.Zero);

        Assert.Equal((350, 350, 91, 91), (combatant.HealthPoints, combatant.MaxHealthPoints, combatant.ManaPoints, combatant.MaxManaPoints));
    }
}
