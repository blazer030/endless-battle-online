using System.Text.Json;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 從 docs/research/wog45/data 匯入的正式內容：抽樣紀錄要等於原版值
public class WogImportTests
{
    private static readonly string ContentDirectory = Path.Combine(BackendPaths.BackendDirectory, "content");

    private static readonly JsonGameContent Content = JsonGameContent.Load(ContentDirectory);

    private static readonly string Gaps = File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/gaps.md"));

    [Fact]
    public void OfficialContent_Loads()
    {
        var content = JsonGameContent.Load(ContentDirectory);

        Assert.NotEmpty(content.AllItems);
    }

    [Fact]
    public void Items_KeepOriginalValues()
    {
        var cloth = Content.GetItem("item-1");

        Assert.Equal(
            ("布衣", ItemCategory.Body, 3, 1, 6L, 1, 3),
            (cloth.Name, cloth.Category, cloth.Bonus.Df, cloth.Bonus.Mdf, cloth.Price, cloth.Holes, cloth.RefineMax));
    }

    [Fact]
    public void Items_KeepVariantsSeparate()
    {
        var swords = new[] { Content.GetItem("item-660"), Content.GetItem("item-928") };

        Assert.All(swords, sword => Assert.Equal(("木刀", ItemCategory.Weapon), (sword.Name, sword.Category)));
    }

    [Fact]
    public void Jobs_KeepOriginalGrowth()
    {
        var warrior = Content.GetJob("job-6");

        Assert.Equal(("戰士", 1, (string?)null), (warrior.Name, warrior.Tier, warrior.ParentId));
        Assert.Equal(new StatRange(1, 3), warrior.Growth.Str);
        Assert.Equal(("job-6", 150, 3500), (Content.GetJob("job-10").ParentId!, Content.GetJob("job-10").UnlockLevel, Content.GetJob("job-10").ProficiencyRequired));
    }

    [Fact]
    public void Monsters_KeepOriginalValues()
    {
        var slime = Content.GetMonster("monster-1");

        Assert.Equal(("史萊姆", 2, 32, 5, 1), (slime.Name, slime.Level, slime.Hp, slime.Element, slime.PlaceId));
        Assert.Equal([1], slime.SubPlaces);
        Assert.Contains(new DropEntry("item-204", 90), slime.Drops);
    }

    [Fact]
    public void Monsters_LeaveOutInvalidRecords()
    {
        using var source = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data/monsters.json")));
        var invalid = source.RootElement.EnumerateArray()
            .Where(monster => monster.GetProperty("level").GetInt32() < 1 || monster.GetProperty("hp").GetInt32() < 1)
            .Select(monster => monster.GetProperty("id").GetInt32())
            .ToList();

        Assert.NotEmpty(invalid);
        Assert.All(invalid, id =>
        {
            Assert.DoesNotContain(Content.AllMonsters, monster => monster.Id == $"monster-{id}");
            Assert.Contains($"| {id} |", Gaps);
        });
    }

    [Fact]
    public void Monsters_ListUnsupportedSkillsInGaps()
    {
        Assert.Matches(@"\| 1 \| 史萊姆 \| [^\n]*猛烈一擊[^\n]*爆氣 Lv2 rate 10", Gaps);
    }

    [Fact]
    public void Skills_FollowDescriptions()
    {
        var burst = Content.GetSkill("skill-1");

        Assert.Equal(("爆氣", SkillType.Active, SkillTiming.BeforeAttack, 10), (burst.Name, burst.Type, burst.Timing, burst.MaxLevel));
        Assert.All(Enumerable.Range(1, 10), level =>
        {
            var values = burst.LevelAt(level);
            Assert.Equal([new SkillCondition(SkillConditionKind.Chance, Math.Min(100, 30 + 10 * level))], values.Conditions);
            Assert.Equal([new SkillCost(SkillCostKind.SkillPoints, 2 * level)], values.Costs);
            Assert.Equal([new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, level, null, null, 3)], values.Effects);
        });
    }

    [Fact]
    public void Skills_ActivationFollowsCurrentRules()
    {
        var stoneWall = Content.GetSkill("skill-550");

        Assert.Equal(("石人陣", SkillType.Passive, true), (stoneWall.Name, stoneWall.Type, stoneWall.IsAlwaysOn));
        Assert.Empty(stoneWall.LevelAt(1).Costs);
        Assert.Equal([new StatChangeEffect(SkillTarget.Caster, SkillStat.Defense, null, 150, null, 0)], stoneWall.LevelAt(1).Effects);
        Assert.Contains("| 550 | 石人陣 | 原版被動發動率 28%，本作常駐 |", Gaps);
    }

    [Fact]
    public void Skills_StunChanceIsEffectChance()
    {
        var stunStrike = Content.GetSkill("skill-142").LevelAt(1);

        Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 40)], stunStrike.Conditions);
        Assert.Equal([new StunEffect(3, 1)], stunStrike.Effects);
    }

    [Fact]
    public void Skills_BonusDamageChanceIsEffectChance()
    {
        var hiddenWeapon = Content.GetSkill("skill-248").LevelAt(1);

        Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 40)], hiddenWeapon.Conditions);
        Assert.Equal(3, hiddenWeapon.Effects.Count);
        Assert.Equal(new StatChangeEffect(SkillTarget.Caster, SkillStat.PhysicalCriticalChance, null, 40, null, 1), hiddenWeapon.Effects[0]);
        var bonus = Assert.IsType<BonusDamageEffect>(hiddenWeapon.Effects[1]);
        Assert.Equal((SkillAttackKind.Physical, new SkillScaling(SkillScalingBasis.Agility, 0.15), 20), (bonus.AttackKind, Assert.Single(bonus.Scaling), bonus.Chance));
        Assert.Equal(new StatChangeEffect(SkillTarget.Caster, SkillStat.Evasion, null, -80, null, 1), hiddenWeapon.Effects[2]);
    }

    [Fact]
    public void Skills_PetSkillsAreListed()
    {
        Assert.DoesNotContain(Content.AllSkills, skill => skill.Id == "skill-320");
        Assert.Contains("| 320 | 物攻命令 | 寵物技能：等寵物系統的調整確定後再做 |", Gaps);
    }

    [Fact]
    public void JobSkills_FollowOriginalJobs()
    {
        Assert.True(Content.Teaches("job-6", "skill-1"));
        Assert.Equal(new SkillPrerequisite("skill-21", 3), Content.GetSkill("skill-31").Prerequisite);
    }

    [Fact]
    public void Recipes_KeepOriginalMaterials()
    {
        var shield = Content.GetRecipe("recipe-1");

        Assert.Equal(("item-308", 1, 100), (shield.ResultItemId, shield.ResultQuantity, shield.SuccessRate));
        Assert.Equal([("item-125", 1), ("item-306", 1)], shield.Materials);
    }

    [Fact]
    public void HonourShop_UsesOriginalExchanges()
    {
        var exchange = Content.HonourShop.Single(entry => entry.Id == "honour-1");

        Assert.Equal(("mercenary-4", "C級傭兵", (long?)null), (exchange.MercenaryId!, Content.GetMercenary("mercenary-4").Name, Content.GetMercenary("mercenary-4").Price));
        Assert.Equal([new ItemQuantity("item-1304", 13), new ItemQuantity("item-1305", 13)], exchange.Cost);
    }

    [Fact]
    public void Keys_ReplaceCustomKeys()
    {
        var caveKey = Content.GetItem("item-1338");

        Assert.Equal((ItemCategory.Key, 17, 86400), (caveKey.Category, caveKey.Key!.PlaceId, caveKey.Key.Seconds));
        Assert.DoesNotContain(Content.AllItems, item => item.Id == "abandoned-cave-key");
    }

    [Fact]
    public void Missions_KeepOriginalGoals()
    {
        var fishingNet = Content.GetMission("mission-1");
        var mile = Content.GetMission("mission-2");

        Assert.Equal(("漁人碼頭村長 周漁民", "製作漁網", 20), (fishingNet.Giver, fishingNet.Title, fishingNet.Level));
        Assert.Equal([new ItemQuantity("item-331", 1)], fishingNet.Deliver);
        Assert.Equal([new ItemQuantity("item-342", 1)], fishingNet.Rewards.Items);
        Assert.Equal((new KillTarget("monster-212", 1), "mission-1", 4), (mile.Kill!, mile.Requirements.PrerequisiteId!, mile.PlaceId!.Value));
        Assert.Equal("mission-2", Content.GetMonster("monster-212").MissionId);
    }

    [Fact]
    public void Missions_DropReferencesToExcludedRecords()
    {
        var petCapture = Content.GetMission("mission-223");

        Assert.All(petCapture.DailyRewards, reward => Assert.DoesNotContain(reward.Items, item => item.ItemId == "item-1422"));
        Assert.Contains("| 223 | [每日]寵物捕捉任務 | 報酬池的 item-1422 沒有匯入，不給 |", Gaps);
        Assert.Null(Content.GetMission("mission-182").Rewards.Pet);
        Assert.Contains("| 182 | 變髮革命 | 寵物獎勵 monster-232 沒有匯入或是任務怪，不給 |", Gaps);
    }
}
