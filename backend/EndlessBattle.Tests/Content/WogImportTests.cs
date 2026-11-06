using System.Text.Json;
using EndlessBattle.Domain.Battle;
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

    // 怪物必殺技是決定不搬，不是規則還不支援，所以不逐隻列；只有必殺技的白蛇龍也就不在清單上
    [Fact]
    public void Gaps_DoesNotListMonsterSpecialAttacks()
    {
        Assert.DoesNotContain("猛烈一擊", Gaps);
        Assert.DoesNotContain("| 13 | 白蛇龍 |", Gaps);
        Assert.Contains("- 必殺技名稱只是暴擊時顯示的文字，決定不搬；原始資料留在 `docs/research/wog45/data/monsters.json`。", Gaps);
    }

    [Fact]
    public void Import_WritesEveryRegularMissionExcept130()
    {
        var missionsInSource = JsonDocument.Parse(File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data/missions.json")))
            .RootElement.GetArrayLength();

        Assert.Equal(missionsInSource - 1, Content.AllMissions.Count);
        Assert.DoesNotContain(Content.AllMissions, mission => mission.Id == "mission-130");
        Assert.Contains("| 130 | 被盜走的清水精靈珠 | 擊殺目標是全域怪", Gaps);
    }

    [Fact]
    public void Import_WritesEveryMonsterExceptThree()
    {
        var excluded = new[] { "monster-261", "monster-287", "monster-324" };
        var monstersInSource = JsonDocument.Parse(File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data/monsters.json")))
            .RootElement.GetArrayLength();

        Assert.Equal(monstersInSource - excluded.Length, Content.AllMonsters.Count);
        Assert.DoesNotContain(Content.AllMonsters, monster => excluded.Contains(monster.Id));
    }

    [Fact]
    public void Import_KeepsSeveralKillTargetsInOrder()
    {
        Assert.Equal(
            [new KillTarget("monster-111", 3), new KillTarget("monster-112", 3)],
            Content.GetMission("mission-29").Kills);
    }

    [Fact]
    public void Import_WritesPetObjective()
    {
        Assert.Equal("monster-1", Content.GetMission("mission-16").DeliverPet);
    }

    [Fact]
    public void Import_WritesSkillCondition()
    {
        Assert.Equal(new RequiredSkill("skill-414", 2), Content.GetMission("mission-318").RequiredSkill);
    }

    [Fact]
    public void Import_ListsSkillConditionThatCannotBeUsed()
    {
        Assert.Null(Content.GetMission("mission-325").RequiredSkill);
        Assert.Contains("| 325 | 背叛與憎恨(1) | needSkillId 324", Gaps);
    }

    [Fact]
    public void Import_WritesNpcWithCleanedMessage()
    {
        var npc = Content.GetMonster("monster-265");

        Assert.Equal((true, "mission-133", 0), (npc.Npc, npc.MissionId, npc.Level));
        Assert.Equal("我是倫魯 你想要前往熔炎火山打敗熔炎巨龍？ 那你一定會需要我的協助，但是你也必須拿到召喚 希瓦", npc.NpcMessage);
    }

    [Fact]
    public void Import_WritesMonsterWithoutSubPlaces()
    {
        Assert.Empty(Content.GetMonster("monster-399").SubPlaces);
    }

    [Fact]
    public void Import_WritesMissionMonsterThatIsNotTarget()
    {
        Assert.Equal("mission-267", Content.GetMonster("monster-434").MissionId);
    }

    [Fact]
    public void Import_ListsNpcThatIsNotTarget()
    {
        Assert.Contains("| 324 | 錫得里克 | NPC 必須是任務的擊殺目標", Gaps);
    }

    [Fact]
    public void Import_WritesRecipePrerequisite()
    {
        Assert.Equal("mission-317", Content.GetRecipe("recipe-174").RequiredMissionId);
        Assert.DoesNotContain("| 174 | 剛龍巨斧．極 |", Gaps);
    }

    [Fact]
    public void Import_WritesPetRewardAboveOldLimit()
    {
        Assert.Equal(new PetReward("monster-417", 530), Content.GetMission("mission-312").Rewards.Pet);
    }

    [Fact]
    public void Import_ListsPrerequisiteOfMission131()
    {
        Assert.Null(Content.GetMission("mission-131").Requirements.PrerequisiteId);
        Assert.Contains("| 131 | 失去光芒的精靈珠 | requiresMissionId 130 沒有匯入", Gaps);
    }

    [Fact]
    public void Import_ListsSkillRewardAboveLevelOne()
    {
        Assert.Null(Content.GetMission("mission-240").Rewards.Skill);
        Assert.Contains("| 240 | 狂戰士修煉(無我) | 技能獎勵 356", Gaps);
    }

    // 規則不支援時才拿掉的前置條件，現在都還原了
    [Fact]
    public void Import_RestoresPrerequisitesOfMissionsNowImported()
    {
        Assert.Equal("mission-111", Content.GetMission("mission-112").Requirements.PrerequisiteId);
        Assert.Equal("mission-317", Content.GetMission("mission-318").Requirements.PrerequisiteId);
    }

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
    public void Monsters_ZeroStatRecordsAreNpcs()
    {
        using var source = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data/monsters.json")));
        var zeroStats = source.RootElement.EnumerateArray()
            .Where(monster => monster.GetProperty("level").GetInt32() < 1 || monster.GetProperty("hp").GetInt32() < 1)
            .Select(monster => monster.GetProperty("id").GetInt32())
            .ToList();

        Assert.Equal(28, zeroStats.Count);
        Assert.All(zeroStats.Where(id => id != 324), id => Assert.True(Content.GetMonster($"monster-{id}").Npc));
        Assert.DoesNotContain(Content.AllMonsters, monster => !monster.Npc && (monster.Level < 1 || monster.Hp < 1));
    }

    private static (string SkillId, int Level, int Rate) Row(EquippedSkill skill) =>
        (skill.Skill.Id, skill.Level, skill.Skill.LevelAt(skill.Level).Conditions.Single(condition => condition.Kind == SkillConditionKind.Chance).Percent);

    // 已匯入的怪物原版共 576 格，8 格是同一隻怪重複的技能
    [Fact]
    public void Import_MonsterSkills_MatchOriginal()
    {
        Assert.Equal(("skill-1", 2, 10), Row(Assert.Single(Content.GetMonster("monster-1").Skills)));
        // 魔盾-水 原版寫 Lv2，本作只有 1 級
        Assert.Equal(("skill-198", 1, 10), Row(Assert.Single(Content.GetMonster("monster-88").Skills)));
        Assert.Equal(("skill-235", 1, 6), Row(Assert.Single(Content.GetMonster("monster-95").Skills)));
        Assert.Equal(("skill-31", 2, 16), Row(Assert.Single(Content.GetMonster("monster-139").Skills, skill => skill.Skill.Name == "急影")));
        Assert.Contains("急影 Lv1 rate 19", Gaps);
        Assert.Equal(664, Content.AllMonsters.Sum(monster => monster.Skills.Count));
    }

    [Fact]
    public void Import_WeaponSkills_MatchOriginal()
    {
        Assert.Equal(new WeaponSkillSpec("skill-1", 2, 12) { SkillName = "爆氣" }, Content.GetItem("item-1823").WeaponSkill);
        // 鋒針陷阱第 2 級的 Chance 18 來自說明：12 × 18% = 2.16 → 2
        Assert.Equal(new WeaponSkillSpec("skill-314", 2, 2) { SkillName = "鋒針陷阱" }, Content.GetItem("item-1828").WeaponSkill);
        Assert.Matches(@"\| 1828 \| 神經系毒氣 \| [^\n]*發動率 12 × Chance 18 換算成 2", Gaps);
        Assert.Null(Content.GetItem("item-1834").WeaponSkill);
        Assert.Equal(33, Content.AllItems.Count(item => item.WeaponSkill is not null));
    }

    [Fact]
    public void Gaps_ListsOnlySkillsNotMoved()
    {
        Assert.DoesNotMatch(@"\| 1 \| 史萊姆 \|", Gaps);
        Assert.DoesNotMatch(@"\| 1823 \| 爆氣火斧 \|", Gaps);
        Assert.DoesNotContain("爆氣 Lv2 rate 10", Gaps);
        Assert.Matches(@"\| 1834 \| 翼龍獸矛 \| 附帶技能 skillId 323、發動率 10", Gaps);
        Assert.Matches(@"\| 426 \| 魔王史萊姆 \| 掉落 2277", Gaps);
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

    // 假死 7 級：預設 Chance 30 + 10·lv，Stun 機率 2·lv，智力比對手高 +5、否則 −5，持續 1 回合；由刺客（job-14）學
    [Fact]
    public void Import_ApparentDeath_MatchesOriginal()
    {
        var skill = Content.GetSkill("skill-235");

        Assert.Equal(("假死", 7), (skill.Name, skill.MaxLevel));
        var first = skill.LevelAt(1);
        Assert.Equal([new SkillCondition(SkillConditionKind.Chance, 40)], first.Conditions);
        Assert.Equal([new SkillCost(SkillCostKind.SkillPoints, 9)], first.Costs);
        Assert.Equal(new StunEffect(2, 1) { ChanceByStat = new SkillChanceAdjustment(SkillScalingBasis.Intelligence, 5) }, Assert.Single(first.Effects));
        Assert.Equal(14, Assert.IsType<StunEffect>(Assert.Single(skill.LevelAt(7).Effects)).Chance);
        Assert.True(Content.Teaches("job-14", "skill-235"));
        // 只剩「沒有搬的欄位」裡的說明，不在「沒有匯入的紀錄」
        Assert.DoesNotContain("智力比較會改變機率", Gaps);
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
        Assert.Equal((new KillTarget("monster-212", 1), "mission-1", 4), (mile.Kills[0], mile.Requirements.PrerequisiteId!, mile.PlaceId!.Value));
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

    [Fact]
    public void CustomContent_WithoutCounterpartIsRemoved()
    {
        Assert.DoesNotContain(Content.AllMonsters, monster => monster.Id == "wandering-puff");
        Assert.DoesNotContain(Content.AllJobs, job => job.Id == "warrior");
        Assert.DoesNotContain(Content.AllSkills, skill => skill.Id == "burst");
        Assert.DoesNotContain(Content.AllItems, item => item.Id == "iron-sword");
    }

    [Fact]
    public void CustomContent_KeptRecordsAreListed()
    {
        var kept = Gaps[Gaps.IndexOf("## 保留的自訂內容", StringComparison.Ordinal)..];
        var customIds = Content.AllItems.Select(item => item.Id)
            .Concat(Content.AllMonsters.Select(monster => monster.Id))
            .Concat(Content.AllJobs.Select(job => job.Id))
            .Concat(Content.AllSkills.Select(skill => skill.Id))
            .Concat(Content.AllMissions.Select(mission => mission.Id))
            .Concat(Content.AllRecipes.Select(recipe => recipe.Id))
            .Concat(Content.AllMercenaries.Select(mercenary => mercenary.Id))
            .Concat(Content.HonourShop.Select(exchange => exchange.Id))
            .Where(CustomContent.IsCustom)
            .ToList();

        Assert.NotEmpty(customIds);
        Assert.All(customIds, id => Assert.Contains($"`{id}`", kept));
        Assert.Contains("`inn.json`", kept);
    }

    // 每種內容匯入的筆數 = 原版筆數 − gaps.md 該節「沒有匯入的紀錄」的列數（技能以名稱分組，一個技能多列）
    [Theory]
    [InlineData("道具", "items.json")]
    [InlineData("怪物", "monsters.json")]
    [InlineData("技能", "skills.json")]
    [InlineData("任務", "missions.json")]
    [InlineData("配方", "recipes.json")]
    [InlineData("勳章所", "honour-shop.json")]
    public void Imported_CountsMatchSourceMinusGaps(string section, string sourceFile)
    {
        using var source = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data", sourceFile)));
        var records = source.RootElement.EnumerateArray().ToList();
        var sourceCount = section == "技能"
            ? records.Select(record => record.GetProperty("name").GetString()).Distinct().Count()
            : records.Count;
        var imported = section switch
        {
            "道具" => Content.AllItems.Count(item => !CustomContent.IsCustom(item.Id)),
            "怪物" => Content.AllMonsters.Count,
            "技能" => Content.AllSkills.Count,
            "任務" => Content.AllMissions.Count,
            "配方" => Content.AllRecipes.Count,
            _ => Content.HonourShop.Count(exchange => !CustomContent.IsCustom(exchange.Id)),
        };

        Assert.Equal(sourceCount - ExcludedRows(section), imported);
    }

    private static int ExcludedRows(string section)
    {
        var start = Gaps.IndexOf($"\n## {section}\n", StringComparison.Ordinal);
        var end = Gaps.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        var body = Gaps[start..(end < 0 ? Gaps.Length : end)];
        var excluded = body.IndexOf("### 沒有匯入的紀錄", StringComparison.Ordinal);
        if (excluded < 0)
        {
            return 0;
        }

        var next = body.IndexOf("\n### ", excluded + 1, StringComparison.Ordinal);
        return body[excluded..(next < 0 ? body.Length : next)].Split('\n').Count(line => line.StartsWith("| ") && char.IsDigit(line[2]));
    }
}
