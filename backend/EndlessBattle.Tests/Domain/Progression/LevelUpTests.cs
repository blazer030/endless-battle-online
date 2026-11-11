using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Progression;

public class LevelUpTests
{
    // 預設角色 Lv1、Exp 0、NextExp 1000、HP 50、SP 25、Base (12,12,12,10,12,10,10)
    private static readonly Character Novice = TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);

    private static JobDefinition JobWith(JobGrowth growth) =>
        new("warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [], growth);

    // 1001 > 1000 升一級：Lv2、Exp 1、NextExp 2184
    // 依 Str、Smart、Life、Vit、Agi、Au、Be 擲骰，0.99 取上限、0.0 取下限：
    // Str rand(2,5) 0.99 → 5；Smart rand(0,1) 0.0 → 0；Life rand(3,4) 0.99 → 4；Vit rand(1,1) 0.0 → 1
    // Agi rand(2,6) 0.99 → 6；Au rand(0,2) 0.0 → 0；Be rand(1,2) 0.99 → 2
    // Base (12,12,12,10,12,10,10) + (5,0,4,1,6,0,2) = (17,12,16,11,18,10,12)；HP／SP 不變
    [Fact]
    public void LevelUp_UsesJobGrowthRanges()
    {
        var job = JobWith(new JobGrowth(
            Strength: new StatRange(2, 5),
            Intelligence: new StatRange(0, 1),
            Life: new StatRange(3, 4),
            Vitality: new StatRange(1, 1),
            Agility: new StatRange(2, 6),
            Charisma: new StatRange(0, 2),
            Faith: new StatRange(1, 2)));
        var random = new ScriptedRandom(0.5, 0.99, 0.0, 0.99, 0.0, 0.99, 0.0, 0.99);

        var result = LevelUp.Apply(Novice with { Exp = 1001 }, job, random, atSeconds: 12.0);

        var growth = new BaseStats(Strength: 5, Intelligence: 0, Life: 4, Vitality: 1, Agility: 6, Charisma: 0, Faith: 2);
        Assert.Equal(
            Novice with { Level = 2, Exp = 1, NextExp = 2184, Base = new BaseStats(17, 12, 16, 11, 18, 10, 12) },
            result.Character);
        Assert.Equal([new LevelUpEvent(12.0, 2, growth)], result.Events);
        Assert.Equal(7, random.Consumed);
    }

    // 3684 > 1000 → Lv2、Exp 2684、NextExp 6·4 + 1080·2 = 2184
    // 2684 > 2184 → Lv3、Exp 500、NextExp 6·9 + 1080·3 = 3294；500 ≤ 3294 停止
    // 成長範圍皆 rand(1,3)：Lv2 七骰 0.0 → 全 1；Lv3 七骰 0.99 → 全 3
    // Base (12,12,12,10,12,10,10) + 1 + 3 = (16,16,16,14,16,14,14)
    [Fact]
    public void LevelUp_AppliesMultipleLevelsInOrder()
    {
        var range = new StatRange(1, 3);
        var job = JobWith(new JobGrowth(range, range, range, range, range, range, range));
        var random = new ScriptedRandom(0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99, 0.99);

        var result = LevelUp.Apply(Novice with { Exp = 3684 }, job, random, atSeconds: 5.0);

        Assert.Equal(
            Novice with { Level = 3, Exp = 500, NextExp = 3294, Base = new BaseStats(16, 16, 16, 14, 16, 14, 14) },
            result.Character);
        Assert.Equal(
            [
                new LevelUpEvent(5.0, 2, new BaseStats(1, 1, 1, 1, 1, 1, 1)),
                new LevelUpEvent(5.0, 3, new BaseStats(3, 3, 3, 3, 3, 3, 3)),
            ],
            result.Events);
        Assert.Equal(14, random.Consumed);
    }

    // 嚴格大於：Exp 1000 等於 NextExp 1000，不升級、不擲骰
    [Fact]
    public void LevelUp_RequiresExpStrictlyAboveNextExp()
    {
        var range = new StatRange(1, 3);
        var job = JobWith(new JobGrowth(range, range, range, range, range, range, range));
        var random = new ScriptedRandom(0.5);
        var character = Novice with { Exp = 1000 };

        var result = LevelUp.Apply(character, job, random, atSeconds: 0.0);

        Assert.Equal(character, result.Character);
        Assert.Empty(result.Events);
        Assert.Equal(0, random.Consumed);
    }
}
