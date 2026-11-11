using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Adventure;

// 除非另外指定，擲骰一律 0.0：rand 取下限、NextInt 取 0、遇敵擲骰 rand(0,100) = 0 → 只有 Meet 0 的怪物入選。
// 預設角色（TestCharacters.Default）：Lv1、Base (12,12,12,10,12,10,10)、地屬性、Hp 60／350、Sp 25／91。
// 對上 dummy（agi 12、df 1、無屬性）：agi 相同 → 30% 先攻，擲 1 ≤ 30 玩家先攻；必中；暴擊 1 < 10、倍率 1.2 → A 14.4；
// (14.4·14.4 − 1) = 206.36，加 rand(⌊14.33/3⌋, ⌊14.33/2⌋) = 4 → 210 ≥ 100，第 1 回合在 0 秒擊倒，怪物不出手。
public class AdventureSimulatorTests
{
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly JobDefinition Warrior = new(
        "warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [],
        new JobGrowth(new(2, 3), new(0, 1), new(1, 2), new(1, 2), new(1, 2), new(0, 1), new(0, 1)));

    private static MonsterDefinition Monster(
        string id, int level = 5, int hp = 100, int at = 10, int df = 1, int jobExp = 12, int meet = 0,
        int placeId = 0, params int[] subPlaces) =>
        new(id, id, level, hp, ManaPoints: 5, at, MagicAttack: 0, df, MagicDefense: 1, Agility: 12, Charisma: 10, Element: 0, jobExp, meet, placeId, subPlaces);

    private static ItemDefinition ItemOf(string id) => TestItems.Item(id, ItemCategory.Material) with { Name = $"名稱-{id}" };

    private static readonly MonsterDefinition Dummy = Monster("dummy");

    private static readonly PlaceDefinition Place = new(1, "平原", "central", [], new LevelRange(1, 10), Restricted: false);

    private static Character Hero() => TestCharacters.Default(accountId: 1, "勇者", 1, 1, From);

    private static readonly EquippedSkill Burst = new(TestSkills.Create("burst", SkillType.Active), Level: 1, Slot: 1);

    private static AdventureRun Run(Character character, double seconds, IRandomSource? random = null, params MonsterDefinition[] monsters) =>
        Run(character, seconds, skills: [], random, monsters);

    private static AdventureRun Run(
        Character character, double seconds, IReadOnlyList<EquippedSkill> skills, IRandomSource? random = null, params MonsterDefinition[] monsters)
    {
        return AdventureSimulator.Run(
            character,
            Warrior,
            Place,
            monsters.Length == 0 ? [Dummy] : monsters,
            ItemOf,
            skills,
            From,
            From.AddSeconds(seconds),
            random ?? new ScriptedRandom(0.0));
    }

    // 擊殺 dummy（Lv5、at 10、mat 0、hp 100、jobExp 12）、玩家 Lv1：
    // 經驗 base 5 → rand(5, 10) = 5；×1.3 = 6.5；5 × 6.5 × 5 × 1 × 0.18 = 29.25；Lv ≤ 15 ×2.5 = 73.125；×2 = 146.25 → 146
    // 金錢 rand(5/2.5, 5) = 2 × rand(10/5, 100/10×0.4) = 2 → 2 × 2 × 0.4 = 1.6 → 2；熟練度 ⌊12⌋ = 12
    [Fact]
    public void Adventure_AppliesLevelUpsAndJobGains()
    {
        var run = Run(Hero() with { Experience = 999 }, seconds: 1);

        // 999 + 146 = 1145 > 1000 → Lv2，剩 145，下一級 6·4 + 1080·2 = 2184；成長各取下限 (2,0,1,1,1,0,0)
        var growth = new BaseStats(2, 0, 1, 1, 1, 0, 0);
        Assert.Equal(new RewardEvent(0, Experience: 146, Money: 2, JobExperience: 12), run.Events.OfType<RewardEvent>().Single());
        Assert.Equal(new LevelUpEvent(0, NewLevel: 2, growth), run.Events.OfType<LevelUpEvent>().Single());
        Assert.Equal(2, run.Character.Level);
        Assert.Equal(145, run.Character.Experience);
        Assert.Equal(2184, run.Character.NextLevelExperience);
        Assert.Equal(2002, run.Character.Money);
        Assert.Equal(new BaseStats(14, 12, 13, 11, 13, 10, 10), run.Character.Base);
        Assert.Equal(new AdvanceSummary(Fights: 1, Wins: 1, Losses: 0, Timeouts: 0, Experience: 146, Money: 2, JobExperience: 12, LevelsGained: 1, SkillsCast: 0), run.Summary);
        Assert.Equal([new JobGain("warrior", 12, 12)], run.JobGains);
        Assert.True(run.EventsCoverAll);
    }

    [Fact]
    public void Adventure_EncounterCarriesHpAndSpOfBothSides()
    {
        var run = Run(Hero(), seconds: 1);

        Assert.Equal(
            new EncounterEvent(0, "dummy", MonsterName: "dummy", MonsterHealthPoints: 100, MonsterMaxHealthPoints: 100, PlayerHealthPoints: 60, PlayerMaxHealthPoints: 350, PlayerManaPoints: 25, PlayerMaxManaPoints: 91),
            run.Events[0]);
        Assert.Contains(run.Events, battleEvent => battleEvent is RoundEvent);
        Assert.Contains(run.Events, battleEvent => battleEvent is AttackEvent);
    }

    [Fact]
    public void AdventureSimulator_EncounterCarriesMonsterName()
    {
        var slime = Monster("monster-1") with { Name = "史萊姆" };

        var run = Run(Hero(), seconds: 1, random: null, slime);

        var encounter = Assert.IsType<EncounterEvent>(run.Events[0]);
        Assert.Equal(("monster-1", "史萊姆"), (encounter.MonsterId, encounter.MonsterName));
    }

    // 住宿：Lv ≤ 100 免費、回滿 HP 350／SP 91、死亡等 15 秒；第一場在 15 秒以滿血開打
    [Fact]
    public void Adventure_RestsBeforeNextFight_WhenDead()
    {
        var run = Run(Hero() with { HealthPoints = 0 }, seconds: 16);

        Assert.Equal(new RestEvent(0, Cost: 0, ExperiencePenalty: false, WaitSeconds: 15), run.Events[0]);
        Assert.Equal(
            new EncounterEvent(15, "dummy", MonsterName: "dummy", MonsterHealthPoints: 100, MonsterMaxHealthPoints: 100, PlayerHealthPoints: 350, PlayerMaxHealthPoints: 350, PlayerManaPoints: 91, PlayerMaxManaPoints: 91),
            run.Events[1]);
        Assert.Equal(1, run.Summary.Fights);
    }

    // 生命 2 → HpMax 100、門檻 15：14 先住宿（未死亡等 5 秒）再以滿血開打；剛好 15 不住宿
    [Theory]
    [InlineData(14, true)]
    [InlineData(15, false)]
    public void Adventure_RestsBeforeFight_WhenHpBelowThreshold(int hp, bool rests)
    {
        var hero = Hero() with { Base = Hero().Base with { Life = 2 }, HealthPoints = hp };

        var run = Run(hero, seconds: 6);

        if (rests)
        {
            Assert.Equal(new RestEvent(0, Cost: 0, ExperiencePenalty: false, WaitSeconds: 5), run.Events[0]);
            var encounter = Assert.IsType<EncounterEvent>(run.Events[1]);
            Assert.Equal(5, encounter.AtSeconds);
            Assert.Equal(100, encounter.PlayerHealthPoints);
            Assert.Equal(100, encounter.PlayerMaxHealthPoints);
        }
        else
        {
            var encounter = Assert.IsType<EncounterEvent>(run.Events[0]);
            Assert.Equal(0, encounter.AtSeconds);
            Assert.Equal(15, encounter.PlayerHealthPoints);
            Assert.Equal(100, encounter.PlayerMaxHealthPoints);
        }
    }

    // 最小重現：31 秒、窗口從 1 秒起；0 秒的住宿在窗口外被略去，但 15 秒起的每一場戰鬥都在窗口內 → 戰鬥沒有缺漏
    [Fact]
    public void Adventure_EventsCoverAll_WhenOnlyARestIsSkipped()
    {
        var run = Run(Hero() with { HealthPoints = 0 }, seconds: 31);

        Assert.Empty(run.Events.OfType<RestEvent>());
        Assert.Equal(15, Assert.IsType<EncounterEvent>(run.Events[0]).AtSeconds);
        Assert.Equal(run.Summary.Fights, run.Events.OfType<EncounterEvent>().Count());
        Assert.True(run.EventsCoverAll);
    }

    // brute（at 40、df 1000）：玩家每下 1 點打不死；怪物暴擊 A 48 對 df 10 → 48·4.8 − 10 = 220.4 + rand(1, 2) = 1 → 221 ≥ 50，
    // 玩家在 0.4 秒倒下。敗場沒有獎勵，HP 留在 0（下一場前才住宿）
    [Fact]
    public void Adventure_CountsLossWithoutReward()
    {
        var run = Run(Hero(), seconds: 1, monsters: Monster("brute", hp: 60, at: 40, df: 1000));

        Assert.Equal(new DefeatEvent(0.4, CombatSide.Player), run.Events.OfType<DefeatEvent>().Single());
        Assert.Empty(run.Events.OfType<RewardEvent>());
        Assert.Equal(0, run.Character.HealthPoints);
        Assert.Equal(new AdvanceSummary(1, Wins: 0, Losses: 1, Timeouts: 0, Experience: 0, Money: 0, JobExperience: 0, LevelsGained: 0, SkillsCast: 0), run.Summary);
        Assert.Empty(run.JobGains);
        Assert.Equal(2.8, run.ConsumedSeconds, 9);
    }

    // 智力 5 → Mat 5 < At 12，自動會用物理；設定指定魔法後整場都用魔法
    [Fact]
    public void Adventure_UsesPreferredAttackKind()
    {
        var mage = Hero() with
        {
            Base = Hero().Base with { Intelligence = 5 },
            Settings = AdventureSettings.Default with { Attack = AttackKind.Magic },
        };

        var run = Run(mage, seconds: 1);

        var attacks = run.Events.OfType<AttackEvent>().Where(attack => attack.Attacker == CombatSide.Player).ToList();
        Assert.NotEmpty(attacks);
        Assert.All(attacks, attack => Assert.Equal(AttackKind.Magic, attack.Kind));
    }

    // wall（df 1000）打不死、怪物 at 10 對 Df 10 傷害很小：第 2 回合開頭 HP 仍低於 50%（60／350），擲 1 ≤ 10 逃跑成功。
    // 逃跑算敗場、沒有任何報酬，但不是死亡（HP 不歸零）；用掉 2 回合 1.6 秒 + 場間 2 秒
    [Fact]
    public void Adventure_EscapeCountsAsLossWithoutRewards()
    {
        var coward = Hero() with { Settings = AdventureSettings.Default with { EscapeHealthPointsPercent = 50 } };

        var run = Run(coward, seconds: 1, monsters: Monster("wall", hp: 60, at: 10, df: 1000));

        Assert.Equal(new EscapeEvent(0.8), run.Events.OfType<EscapeEvent>().Single());
        Assert.Empty(run.Events.OfType<RewardEvent>());
        Assert.Empty(run.Events.OfType<DefeatEvent>());
        Assert.Equal(
            new AdvanceSummary(1, Wins: 0, Losses: 1, Timeouts: 0, Experience: 0, Money: 0, JobExperience: 0, LevelsGained: 0, SkillsCast: 0, Escapes: 1),
            run.Summary);
        Assert.True(run.Character.HealthPoints > 0);
        Assert.Empty(run.JobGains);
        Assert.Equal(3.6, run.ConsumedSeconds, 9);
    }

    // 每場 1 回合 0.8 秒 + 場間 2 秒 = 2.8 秒；120 秒內在 0、2.8、…、117.6 開打共 43 場。
    // 只保留開打時間 ≥ 120 − 30 = 90 的場次：33 × 2.8 = 92.4 起的 10 場；摘要仍計入全部
    [Fact]
    public void Adventure_KeepsOnlyLastThirtySecondsOfEvents()
    {
        var run = Run(Hero(), seconds: 120);

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        Assert.Equal(10, encounters.Count);
        Assert.Equal(92.4, encounters[0].AtSeconds, 6);
        Assert.Same(encounters[0], run.Events[0]);
        Assert.All(run.Events, battleEvent => Assert.True(battleEvent.AtSeconds >= 90));
        Assert.Equal(43, run.Summary.Fights);
        Assert.Equal(43, run.Summary.Wins);
        Assert.False(run.EventsCoverAll);
        Assert.Equal(120.4, run.ConsumedSeconds, 6);
    }

    // 200 秒內每場 0.8 + 2 = 2.8 秒，在 0、2.8、…、198.8 開打共 72 場，
    // 每場都全額 146／2／12，場間一直是 2 秒。NextExp 設得很大，避免升級改變獎勵（手算見 Adventure_AppliesLevelUpsAndJobGains 上方）
    [Fact]
    public void Adventure_EveryFightPaysFullAtNormalInterval()
    {
        var run = Run(Hero() with { NextLevelExperience = 1_000_000 }, seconds: 200);

        Assert.Equal(72, run.Summary.Fights);
        Assert.Equal((Experience: 72 * 146L, Money: 72 * 2L, JobExperience: 72 * 12L), (run.Summary.Experience, run.Summary.Money, run.Summary.JobExperience));
        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        Assert.All(encounters.Zip(encounters.Skip(1)), pair => Assert.Equal(2.8, pair.Second.AtSeconds - pair.First.AtSeconds, 9));
        Assert.Equal(201.6, run.ConsumedSeconds, 6);
    }

    // rock（hp 60、at 40、df 1000）對 Df 1000 的角色：雙方每下 1 點，40 回合後超限（32 秒）。
    // 累計傷害 40 → round(40 / 1.5) = 27；等級以 5 × 0.6 = 3 計：base 20 → rand(20, 23) = 20；×1.3 = 26；
    // 3 × 26 × 3 × (27/60) × 0.18 = 18.954；未擊殺再 ×0.45 = 8.5293；×2.5 = 21.32325；×2 = 42.6465 → 43。沒有金錢與熟練度、不算敗場
    [Fact]
    public void Adventure_TimeoutGivesPartialExpOnly()
    {
        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 } };
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);

        var run = Run(tough, seconds: 1, monsters: rock);

        Assert.Equal(new TimeoutEvent(32, 40, 40), run.Events.OfType<TimeoutEvent>().Single());
        Assert.Equal(new RewardEvent(32, Experience: 43, Money: 0, JobExperience: 0), run.Events.OfType<RewardEvent>().Single());
        Assert.Equal(43, RewardFormulas.Experience(rock, 27, playerLevel: 1, killed: false, new ScriptedRandom(0.0), levelOverride: 3));
        Assert.Equal(new AdvanceSummary(1, Wins: 0, Losses: 0, Timeouts: 1, Experience: 43, Money: 0, JobExperience: 0, LevelsGained: 0, SkillsCast: 0), run.Summary);
        Assert.Equal(20, run.Character.HealthPoints);
        Assert.Empty(run.JobGains);
        Assert.Equal(34, run.ConsumedSeconds, 9);
    }

    // 遇敵擲骰 rand(0,100) = ⌊擲骰 × 101⌋：0.5 → 50，兩隻都入選，0.7 → NextInt(2) = 1 → wolf；
    // 0.3 → 30 恰好等於 wolf 的 Meet 30，m_meet ≤ rand 仍入選 → wolf；
    // 0.25 → 25，只有 Meet 0 的 sprite；全部 Meet 50 而擲出 0 → 沒有候選，退回全部怪物中均勻抽
    [Fact]
    public void Adventure_PicksMonstersWithinMeetRoll()
    {
        var sprite = Monster("sprite", meet: 0);
        var wolf = Monster("wolf", meet: 30);

        Assert.Equal("wolf", FirstMonster(Run(Hero(), 1, new ScriptedRandom(0.0, 0.5, 0.7), sprite, wolf)));
        Assert.Equal("wolf", FirstMonster(Run(Hero(), 1, new ScriptedRandom(0.0, 0.3, 0.7), sprite, wolf)));
        Assert.Equal("sprite", FirstMonster(Run(Hero(), 1, new ScriptedRandom(0.0, 0.25, 0.7), sprite, wolf)));
        Assert.Equal("wolf", FirstMonster(Run(Hero(), 1, new ScriptedRandom(0.0, 0.0, 0.7), Monster("bat", meet: 50), Monster("wolf", meet: 50))));

        static string FirstMonster(AdventureRun run) => run.Events.OfType<EncounterEvent>().First().MonsterId;
    }

    // 英雄 HP 滿（350）、SP 13／91（< 15% = 13.65；14 則不住宿）。有主動技能 → 開打前先住宿（0 秒），住宿後 SP 回滿再開打；
    // 沒有技能或只有被動（不耗 SP）→ 直接開打
    [Fact]
    public void Adventure_RestsWhenSpBelowThreshold_OnlyWithActiveSkills()
    {
        var hero = Hero() with { HealthPoints = 350, ManaPoints = 13 };
        var ironSkin = new EquippedSkill(TestSkills.Create("iron-skin", SkillType.Passive), Level: 1, Slot: 4);

        Assert.IsType<RestEvent>(Run(hero, seconds: 1, skills: [Burst]).Events[0]);
        Assert.IsType<EncounterEvent>(Run(hero, seconds: 1).Events[0]);
        Assert.IsType<EncounterEvent>(Run(hero, seconds: 1, skills: [ironSkin]).Events[0]);
    }

    // SP 13 低於門檻，但唯一的主動技能是武器附帶、不耗 SP，所以不因 SP 住宿
    [Fact]
    public void Run_WeaponSkillOnly_IgnoresSpRestThreshold()
    {
        var hero = Hero() with { HealthPoints = 350, ManaPoints = 13 };
        var weapon = new EquippedSkill(TestSkills.Create("burst", SkillType.Active), Level: 1, SkillLoadout.WeaponSlot);

        Assert.IsType<EncounterEvent>(Run(hero, seconds: 1, skills: [weapon]).Events[0]);
    }

    // 英雄在子地區 2：怪物 first 只在子地區 1、second 只在子地區 2，擲骰 0 時只有 second 入選
    [Fact]
    public void Adventure_PicksBySubPlace()
    {
        var first = Monster("first", placeId: 1, subPlaces: 1);
        var second = Monster("second", placeId: 1, subPlaces: 2);

        var run = Run(Hero() with { SubPlace = 2 }, seconds: 30, monsters: [first, second]);

        Assert.True(run.Summary.Fights > 1);
        Assert.All(run.Events.OfType<EncounterEvent>(), encounter => Assert.Equal("second", encounter.MonsterId));
    }

    // burst lv1 耗 SP 5、發動率 100（擲 1）：0 秒那場 25 → 20，2.8 秒那場從 20 開始 → 15；住宿以外不回 SP
    [Fact]
    public void Adventure_CarriesSpBetweenFights()
    {
        var run = Run(Hero(), seconds: 3, skills: [Burst]);

        Assert.Equal(2, run.Summary.Fights);
        Assert.Equal([20, 15], run.Events.OfType<SkillEvent>().Select(skill => skill.ManaPointsAfter));
        Assert.Equal([25, 20], run.Events.OfType<EncounterEvent>().Select(encounter => encounter.PlayerManaPoints));
        Assert.Equal(15, run.Character.ManaPoints);
    }

    // 冷卻只在一場戰鬥內有效：冷卻 99 的 burst 在 0 秒與 2.8 秒兩場都各施放一次
    [Fact]
    public void Adventure_ResetsCooldownEachFight()
    {
        var longCooldown = Burst with { Skill = TestSkills.Battle("burst", "爆氣", SkillPreset.AtMultiplier, cooldown: 99) };

        var run = Run(Hero(), seconds: 3, skills: [longCooldown]);

        Assert.Equal(2, run.Summary.Fights);
        Assert.Equal([20, 15], run.Events.OfType<SkillEvent>().Select(skill => skill.ManaPointsAfter));
    }

    // 每場耗 SP 5，開打前 SP < 91 × 15% = 13.65 就住宿（5 秒，回滿 91）。時間線（每場 2.8 秒）：
    // 0～8.4 秒 3 場（SP 25 → 10）、住宿到 13.4；16 場到 58.2（91 → 11）、住宿到 63.2；16 場到 108、住宿到 113；113、115.8、118.6 各 1 場。
    // 共 38 場、38 次施放、結束於 121.4 秒。只保留 90 秒起開打的場次：63.2 起第 11 場（91.2）起 6 場 + 最後 3 場 = 9 場的施放事件；其餘施放仍要計入
    [Fact]
    public void Adventure_CountsSkillsCastAcrossDroppedFights()
    {
        var run = Run(Hero(), seconds: 120, skills: [Burst]);

        Assert.Equal(38, run.Summary.Fights);
        Assert.Equal(9, run.Events.OfType<SkillEvent>().Count());
        Assert.Equal(38, run.Summary.SkillsCast);
        Assert.Equal(91 - 3 * 5, run.Character.ManaPoints);
    }

    // 施毒每回合結束的持續傷害不算施放：施放次數只數發動（怪物 1000 血，第 1 回合打不倒，才有持續傷害）
    [Fact]
    public void Adventure_DoesNotCountDamageOverTimeAsCasts()
    {
        var poison = new SkillDefinition("poison", "施毒", SkillType.Active, SkillTiming.BeforeAttack, "說明",
        [
            new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 100)], [],
                [new DamageOverTimeEffect([new SkillScaling(SkillScalingBasis.Intelligence, 0.1)], Rounds: 3)]),
        ]);

        var run = Run(Hero(), seconds: 1, skills: [new EquippedSkill(poison, Level: 1, Slot: 1)], monsters: Monster("tough", hp: 1000));

        var events = run.Events.OfType<SkillEvent>().ToList();
        Assert.Contains(events, skill => skill.Lasting);
        Assert.Equal(events.Count(skill => !skill.Lasting), run.Summary.SkillsCast);
    }

    // 擲骰一律 0.5 → 發動擲骰 rand(1,100) = 51：發動率是技能等級固定的 30 + 10·lv，lv1（40）不施放、lv3（60）施放
    [Theory]
    [InlineData(1, false)]
    [InlineData(3, true)]
    public void Adventure_UsesSkillChanceForCasting(int level, bool casts)
    {
        var run = Run(Hero(), seconds: 1, skills: [Burst with { Level = level }], new ScriptedRandom(0.5));

        Assert.Equal(1, run.Summary.Fights);
        Assert.Equal(casts, run.Summary.SkillsCast > 0);
    }

    [Fact]
    public void Adventure_ConsumesNothing_WhenNoTimeAvailable()
    {
        var hero = Hero();

        var run = Run(hero, seconds: 0);

        Assert.Empty(run.Events);
        Assert.Equal(0, run.ConsumedSeconds);
        Assert.Equal(AdvanceSummary.Empty, run.Summary);
        Assert.Equal(hero, run.Character);
        Assert.True(run.EventsCoverAll);
    }

    // 裝備 life +20 → HpMax 32×25+50 = 850，門檻 127.5：Hp 60 低於門檻 → 先住宿並回滿到 850（沒裝備時 60 ≥ 52.5 直接開打）
    [Fact]
    public void Adventure_RestsAgainstEffectiveMaximum()
    {
        var bonus = new EquipmentBonus(StatBonus.Zero with { Life = 20 });

        var run = AdventureSimulator.Run(
            Hero(), Warrior, Place, [Dummy], ItemOf, [], From, From.AddSeconds(1), new ScriptedRandom(0.0), bonus);

        Assert.IsType<RestEvent>(run.Events[0]);
        Assert.Equal(850, run.Character.HealthPoints);
    }

    // pebble（hp 2、at 40、df 1000）對 Df 1000 的角色：雙方每下 1 點，玩家先攻，第 2 回合擊倒，每場 1.6 秒 + 場間 2 秒。
    // HP 53 ≥ 52.5 不住宿；第 1 回合被打到 52 < 52.5 → 第 2 回合開頭喝（+1 → 53）。第二場在 3.6 秒開打，同樣在第 2 回合符合門檻，
    // 但補給只剩 0 → 不喝、以 52 收場
    [Fact]
    public void Adventure_CarriesSupplyAcrossFights()
    {
        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 }, HealthPoints = 53 };
        var pebble = Monster("pebble", hp: 2, at: 40, df: 1000);
        var supply = new SupplyState("tonic", "補給", HealthPoints: 1, ManaPoints: 0, Quantity: 1);

        var run = AdventureSimulator.Run(
            tough, Warrior, Place, [pebble], ItemOf, [], From, From.AddSeconds(4), new ScriptedRandom(0.0), supply: supply);

        Assert.Equal(2, run.Summary.Fights);
        Assert.Equal(new PotionEvent(0.8, "tonic", "補給", HealthPoints: 1, ManaPoints: 0, Remaining: 0), run.Events.OfType<PotionEvent>().Single());
        Assert.Equal(1, run.Summary.PotionsUsed);
        Assert.Equal(0, run.SupplyRemaining);
        Assert.Equal(52, run.Character.HealthPoints);
    }

    // pebble 改成 hp 10：第 1 回合被打到 52，第 2～10 回合開頭各喝一次（+1 → 53），第 10 回合擊倒時停在 53，不住宿。
    // 每場 10 回合 8 秒 + 場間 2 秒，第二場在 10 秒開打、過程相同。每場的 9 次上限各自計算 → 兩場共喝 18 次
    [Fact]
    public void Adventure_PotionCounterResetsEachFight()
    {
        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 }, HealthPoints = 53 };
        var pebble = Monster("pebble", hp: 10, at: 40, df: 1000);
        var supply = new SupplyState("tonic", "補給", HealthPoints: 1, ManaPoints: 0, Quantity: 20);

        var run = AdventureSimulator.Run(
            tough, Warrior, Place, [pebble], ItemOf, [], From, From.AddSeconds(11), new ScriptedRandom(0.0), supply: supply);

        Assert.Equal(2, run.Summary.Fights);
        var secondFight = run.Events.OfType<EncounterEvent>().Last().AtSeconds;
        Assert.Equal(10, secondFight, 9);
        var potions = run.Events.OfType<PotionEvent>().ToList();
        Assert.Equal(9, potions.Count(potion => potion.AtSeconds < secondFight));
        Assert.Equal(9, potions.Count(potion => potion.AtSeconds >= secondFight));
        Assert.Equal(18, run.Summary.PotionsUsed);
        Assert.Equal(2, run.SupplyRemaining);
    }

    // 沒有補給：剩餘數量為 null
    [Fact]
    public void Adventure_ReportsNoSupplyRemaining_WithoutSupply()
    {
        var run = Run(Hero(), seconds: 1);

        Assert.Null(run.SupplyRemaining);
        Assert.Equal(0, run.Summary.PotionsUsed);
    }

    // 勝場擲掉落：ore topr 10 → 分母 9，擲 0.0 → 1 命中；herb topr 1 → 不擲、一定掉；名稱來自道具定義
    [Fact]
    public void Adventure_EmitsDropsOnWins()
    {
        var monster = Dummy with { Drops = [new("ore", 10), new("herb", 1)] };

        var run = Run(Hero(), seconds: 1, monsters: monster);

        var reward = run.Events.OfType<RewardEvent>().Single();
        Assert.Equal([new DropResult("ore", "名稱-ore", 1, false), new DropResult("herb", "名稱-herb", 1, false)], reward.Drops);
        Assert.Equal(["ore", "herb"], run.DroppedItemIds);
        Assert.Equal(0, run.Summary.Drops);
    }

    // 敗場沒有獎勵事件也不掉落；超限只有部分經驗，同樣不掉落
    [Fact]
    public void Adventure_DropsNothingOnLossesAndTimeouts()
    {
        var drops = new DropEntry[] { new("ore", 1) };
        var brute = Monster("brute", hp: 60, at: 40, df: 1000) with { Drops = drops };
        var rock = Monster("rock", hp: 60, at: 40, df: 1000) with { Drops = drops };

        var lost = Run(Hero(), seconds: 1, monsters: brute);
        Assert.Equal(1, lost.Summary.Losses);
        Assert.Empty(lost.DroppedItemIds);

        var timedOut = Run(Hero() with { Base = Hero().Base with { Vitality = 1000 } }, seconds: 1, monsters: rock);
        Assert.Equal(1, timedOut.Summary.Timeouts);
        Assert.Empty(timedOut.DroppedItemIds);
        Assert.All(timedOut.Events.OfType<RewardEvent>(), reward => Assert.Empty(reward.Drops));
    }

    // 窗口外的勝場照樣掉落（進背包），只是事件被略去：31 秒 → 窗口從 1 秒起，0 秒那場在窗口外
    [Fact]
    public void Adventure_CountsDropsOfFightsOutsideTheWindow()
    {
        var monster = Dummy with { Drops = [new("ore", 1)] };

        var run = Run(Hero(), seconds: 31, monsters: monster);

        Assert.Equal(run.Summary.Wins, run.DroppedItemIds.Count);
        Assert.Equal(run.Summary.Wins - 1, run.Events.OfType<RewardEvent>().Sum(reward => reward.Drops.Count));
    }

    // 擲骰順序：經驗 → 金錢 → 掉落。先量出沒有掉落時一場消耗的亂數數 N，
    // 再把第 N + 1 次（緊接在經驗、金錢之後）設為 0.99：ore topr 10 分母 9 → 擲 9 落空。順序若不同，這個值會被別的擲骰吃掉，ore 就會掉
    [Fact]
    public void Adventure_DropsConsumeRngAfterRewards()
    {
        var baseline = new ScriptedRandom(0.0);
        Run(Hero(), seconds: 1, baseline);

        var script = Enumerable.Repeat(0.0, baseline.Consumed).Append(0.99).ToArray();
        var run = Run(Hero(), seconds: 1, new ScriptedRandom(0.0, script), Dummy with { Drops = [new("ore", 10)] });

        Assert.Empty(run.DroppedItemIds);
    }

    private static readonly MonsterDefinition Sprite = Monster("test-sprite");

    private static ActiveMission Hunt(int progress = 0) => TrackedMissions.Single("test-hunt", "測試討伐", "test-sprite", Required: 3, progress, AcceptedAt: From, ExpiresAt: null);

    private static AdventureRun RunWithMissions(
        Character character, double seconds, IReadOnlyList<ActiveMission> missions, DateTimeOffset? from = null, IRandomSource? random = null,
        params MonsterDefinition[] monsters)
    {
        var start = from ?? From;
        return AdventureSimulator.Run(
            character, Warrior, Place, monsters.Length == 0 ? [Sprite] : monsters, ItemOf, [], start, start.AddSeconds(seconds), random ?? new ScriptedRandom(0.0),
            missions: missions);
    }

    // 20 秒 8 場全勝（同 dummy，每場 0 秒擊倒、間隔 2.8 秒）：前 3 場各 +1，之後已達成不再計；
    // 進度事件緊接在同一時間的 RewardEvent 之後（第 7 場的升級在後面，不受影響）
    [Fact]
    public void Adventure_CountsMissionKillsAndEmitsProgress()
    {
        var run = RunWithMissions(Hero(), seconds: 20, [Hunt()]);

        Assert.Equal(8, run.Summary.Wins);
        TrackedMissions.AssertSame([Hunt(progress: 3)], run.Missions);
        var progress = run.Events.OfType<MissionProgressEvent>().ToList();
        Assert.Equal([1, 2, 3], progress.Select(mission => mission.Progress));
        Assert.All(progress, mission => Assert.Equal(
            ("test-hunt", "測試討伐", "test-sprite", 3), (mission.MissionId, mission.Title, mission.MonsterId, mission.Required)));
        foreach (var mission in progress)
        {
            var index = run.Events.ToList().IndexOf(mission);
            var reward = Assert.IsType<RewardEvent>(run.Events[index - 1]);
            Assert.Equal(reward.AtSeconds, mission.AtSeconds);
        }

        Assert.Equal(3, run.Summary.MissionKills);
    }

    private static MonsterDefinition Npc(string id = "npc", string? message = "我是信使") => new(
        id, "信使", Level: 0, HealthPoints: 0, ManaPoints: 0, Attack: 0, MagicAttack: 0, Defense: 0, MagicDefense: 0, Agility: 0, Charisma: 0, Element: 0, JobExperience: 0, Meet: 0, PlaceId: 1, SubPlaces: [])
    {
        MissionId = "visit",
        Npc = true,
        NpcMessage = message,
    };

    private static ActiveMission Visit(int required = 1) =>
        TrackedMissions.Single("visit", "拜訪", "npc", Required: required, Progress: 0, AcceptedAt: From, ExpiresAt: null);

    [Fact]
    public void Adventure_NpcAdvancesMissionWithoutFight()
    {
        var hero = Hero();

        var run = RunWithMissions(hero, seconds: 1, [Visit()], monsters: Npc());

        Assert.Collection(
            run.Events,
            first => Assert.Equal(new NpcEncounterEvent(0, "npc", "信使", "我是信使"), first),
            second => Assert.Equal(new MissionProgressEvent(0, "visit", "拜訪", "npc", "信使", 1, 1, false, true, false), second));
        Assert.Equal((0, 0, 0, 0L, 0L, 1), (run.Summary.Fights, run.Summary.Wins, run.Summary.Losses, run.Summary.Experience, run.Summary.Money, run.Summary.MissionKills));
        Assert.Equal(hero, run.Character);
        TrackedMissions.AssertSame([Visit().WithProgress(1)], run.Missions);
    }

    [Fact]
    public void Adventure_NpcTakesOneFightInterval()
    {
        var run = RunWithMissions(Hero(), seconds: 5, [Visit(required: 9)], monsters: Npc());

        Assert.Equal([0.0, 2.0, 4.0], run.Events.OfType<NpcEncounterEvent>().Select(npc => npc.AtSeconds));
        Assert.Equal(6.0, run.ConsumedSeconds);
    }

    [Fact]
    public void Adventure_NpcWithFullTargetAddsNothing()
    {
        ActiveMission mission = new(
            "visit", "拜訪", [new MissionTarget("npc", 5, 5), new MissionTarget("other-npc", 5, 0)], From, ExpiresAt: null);

        var run = RunWithMissions(Hero(), seconds: 1, [mission], monsters: Npc());

        Assert.IsType<NpcEncounterEvent>(Assert.Single(run.Events));
        Assert.Equal(0, run.Summary.MissionKills);
    }

    // 只有遇敵的兩擲（rand(0,100)、候選索引），NPC 本身不擲
    [Fact]
    public void Adventure_NpcUsesOnlyEncounterRolls()
    {
        var random = new ScriptedRandom(0.0);

        RunWithMissions(Hero(), seconds: 1, [Visit()], random: random, monsters: Npc());

        Assert.Equal(2, random.Consumed);
    }

    // 地區有 NPC、但任務沒接：NPC 不在候選裡，擲骰序列與沒有 NPC 時完全相同
    [Fact]
    public void Adventure_NoNpcKeepsRollSequence()
    {
        var with = new ScriptedRandom(0.0);
        var without = new ScriptedRandom(0.0);

        var withNpc = RunWithMissions(Hero(), seconds: 10, [], random: with, monsters: [Sprite, Npc()]);
        var withoutNpc = RunWithMissions(Hero(), seconds: 10, [], random: without, monsters: [Sprite]);

        Assert.Equal(without.Consumed, with.Consumed);
        Assert.Equal(withoutNpc.Summary, withNpc.Summary);
    }

    // 8 場全打 test-sprite：另一個目標還沒滿，所以填滿這個目標時任務仍未完成
    [Fact]
    public void Adventure_MissionEventCompletedOnlyWhenAllTargetsFull()
    {
        ActiveMission Double(int wolves) => new(
            "test-double", "雙目標",
            [new MissionTarget("test-sprite", Required: 1, Progress: 0), new MissionTarget("test-wolf", Required: 1, wolves)],
            From, ExpiresAt: null);

        var unfinished = Assert.Single(RunWithMissions(Hero(), seconds: 20, [Double(wolves: 0)]).Events.OfType<MissionProgressEvent>());
        var finished = Assert.Single(RunWithMissions(Hero(), seconds: 20, [Double(wolves: 1)]).Events.OfType<MissionProgressEvent>());

        Assert.Equal(("test-sprite", 1, 1, false), (unfinished.MonsterId, unfinished.Progress, unfinished.Required, unfinished.Completed));
        Assert.Equal(("test-sprite", 1, 1, true), (finished.MonsterId, finished.Progress, finished.Required, finished.Completed));
    }

    [Fact]
    public void Adventure_MissionEventCarriesSkillTooLow()
    {
        var low = RunWithMissions(Hero(), seconds: 20, [Hunt() with { SkillTooLow = true }]).Events.OfType<MissionProgressEvent>().First();
        var met = RunWithMissions(Hero(), seconds: 20, [Hunt()]).Events.OfType<MissionProgressEvent>().First();

        Assert.True(low.SkillTooLow);
        Assert.False(met.SkillTooLow);
    }

    // 同上 8 場（開打即打完於 0、2.8、…、19.6 秒），任務在第 19 秒才接取：只有 19.6 秒那場計入
    [Fact]
    public void Adventure_CountsOnlyFightsAfterAcceptance()
    {
        var hunt = Hunt() with { AcceptedAt = From.AddSeconds(19) };

        var run = RunWithMissions(Hero(), seconds: 20, [hunt]);

        Assert.Equal(8, run.Summary.Wins);
        TrackedMissions.AssertSame([hunt.WithProgress(1)], run.Missions);
        Assert.Equal(1, run.Summary.MissionKills);
        var progress = Assert.Single(run.Events.OfType<MissionProgressEvent>());
        Assert.Equal((1, 19.6), (progress.Progress, Math.Round(progress.AtSeconds, 6)));
    }

    // 進度事件帶著任務是否還要交付：混合任務為 true，純擊殺任務為 false
    [Fact]
    public void Adventure_FlagsProgressOfMissionsThatRequireDelivery()
    {
        var mixed = RunWithMissions(Hero(), seconds: 20, [Hunt() with { RequiresDelivery = true }]);
        var killOnly = RunWithMissions(Hero(), seconds: 20, [Hunt()]);

        Assert.Equal([true, true, true], mixed.Events.OfType<MissionProgressEvent>().Select(mission => mission.RequiresDelivery));
        Assert.Equal([false, false, false], killOnly.Events.OfType<MissionProgressEvent>().Select(mission => mission.RequiresDelivery));
    }

    // 同 Adventure_DropsNothingOnLossesAndTimeouts：敗場、超限（有部分經驗）都不計任務
    [Fact]
    public void Adventure_CountsMissionKillsOnlyOnWins()
    {
        var brute = Monster("brute", hp: 60, at: 40, df: 1000);
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);
        var bruteHunt = TrackedMissions.Single("brute-hunt", "討伐", "brute", Required: 3, Progress: 0, AcceptedAt: From, ExpiresAt: null);
        var rockHunt = TrackedMissions.Single("rock-hunt", "討伐", "rock", Required: 3, Progress: 0, AcceptedAt: From, ExpiresAt: null);

        var lost = RunWithMissions(Hero(), seconds: 1, [bruteHunt], monsters: brute);
        Assert.Equal(1, lost.Summary.Losses);
        TrackedMissions.AssertSame([bruteHunt], lost.Missions);
        Assert.Empty(lost.Events.OfType<MissionProgressEvent>());
        Assert.Equal(0, lost.Summary.MissionKills);

        var timedOut = RunWithMissions(Hero() with { Base = Hero().Base with { Vitality = 1000 } }, seconds: 1, [rockHunt], monsters: rock);
        Assert.Equal(1, timedOut.Summary.Timeouts);
        TrackedMissions.AssertSame([rockHunt], timedOut.Missions);
        Assert.Empty(timedOut.Events.OfType<MissionProgressEvent>());
        Assert.Equal(0, timedOut.Summary.MissionKills);
    }

    // 離線結算跨過 09:00（01:00 UTC）。耐打的角色（Vit 1000，NextExp 很大避免升級後 HP 門檻上升而住宿）對 hp 2、at 40、df 1000 的雪精：雙方每下 1 點，
    // 玩家先攻、第 2 回合（0.8 秒）擊倒，每場 1.6 秒 + 場間 2 秒 = 3.6 秒。從 00:59:49 起 20 秒 6 場，開打於 0、3.6、…、18 秒、
    // 打完於開打 + 0.8 秒：前 3 場打完於 00:59:49.8、53.4、57.0 計入；第 4 場在 00:59:59.8（重置前）開打、
    // 01:00:00.6（重置後）打完，以打完的時刻判斷 → 不計，之後的也都不算
    [Fact]
    public void Adventure_StopsCountingDailyAfterReset()
    {
        var from = new DateTimeOffset(2026, 10, 22, 0, 59, 49, TimeSpan.Zero);
        var reset = new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero);
        var daily = TrackedMissions.Single("test-daily", "測試每日", "test-sprite", Required: 10, Progress: 0, AcceptedAt: from, ExpiresAt: reset);
        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 }, NextLevelExperience = 1_000_000 };
        var pebble = Monster("test-sprite", hp: 2, at: 40, df: 1000);

        var run = RunWithMissions(tough, seconds: 20, [daily], from, monsters: pebble);

        Assert.Equal(6, run.Summary.Wins);
        var straddling = (Start: run.Events.OfType<EncounterEvent>().ElementAt(3).AtSeconds, End: run.Events.OfType<RewardEvent>().ElementAt(3).AtSeconds);
        Assert.Equal((10.8, 11.6), (Math.Round(straddling.Start, 6), Math.Round(straddling.End, 6)));
        Assert.True(from.AddSeconds(straddling.Start) < reset && from.AddSeconds(straddling.End) > reset);
        TrackedMissions.AssertSame([daily.WithProgress(3)], run.Missions);
        Assert.Equal(3, run.Summary.MissionKills);
        Assert.Equal([1, 2, 3], run.Events.OfType<MissionProgressEvent>().Select(mission => mission.Progress));
    }

    // 60 秒 22 場全勝（開打於 0、2.8、…、58.8；NextExp 很大避免升級改變節奏）：窗口從 30 秒起，只有 30.8 秒起的 11 場輸出事件，
    // 但窗口外 11 場的擊殺照樣計入進度與 MissionKills
    [Fact]
    public void Adventure_CountsKillsOutsideTheEventWindow()
    {
        var longHunt = TrackedMissions.Single("long-hunt", "長期討伐", "test-sprite", Required: 100, Progress: 0, AcceptedAt: From, ExpiresAt: null);

        var run = RunWithMissions(Hero() with { NextLevelExperience = 1_000_000 }, seconds: 60, [longHunt]);

        Assert.Equal(22, run.Summary.Wins);
        Assert.Equal(11, run.Events.OfType<EncounterEvent>().Count());
        TrackedMissions.AssertSame([longHunt.WithProgress(22)], run.Missions);
        Assert.Equal(22, run.Summary.MissionKills);
        Assert.Equal(Enumerable.Range(12, 11), run.Events.OfType<MissionProgressEvent>().Select(mission => mission.Progress));
    }

    // 第一場就升級（Exp 999 + 146 > 1000）：同一場的順序是 RewardEvent → MissionProgressEvent → LevelUpEvent
    [Fact]
    public void Adventure_EmitsMissionProgressBeforeLevelUp()
    {
        var run = RunWithMissions(Hero() with { Experience = 999 }, seconds: 1, [Hunt()]);

        Assert.Equal(
            [typeof(RewardEvent), typeof(MissionProgressEvent), typeof(LevelUpEvent)],
            run.Events.Where(battleEvent => battleEvent is RewardEvent or MissionProgressEvent or LevelUpEvent).Select(battleEvent => battleEvent.GetType()));
    }

    // 地區 1 子地區 1：雪精（meet 0）、寒狼（meet 30）、首領（meet 60，任務 test-chief-hunt）；數值同 dummy，每場都贏
    private static readonly MonsterDefinition[] ChiefPlace =
    [
        Monster("test-sprite", placeId: 1, subPlaces: 1),
        Monster("test-wolf", meet: 30, placeId: 1, subPlaces: 1),
        Monster("test-sprite-chief", meet: 60, placeId: 1, subPlaces: 1) with { MissionId = "test-chief-hunt" },
    ];

    // SeededRandom(7)、60 秒（18 場全勝）：帶著 test-chief-hunt 0/1 時，首領在 36.2 秒那場出現並被擊倒（進度事件 36.6 秒），
    // 之後任務已達成，遭遇都不是牠。用 60 秒而非更長，讓擊倒首領的那場落在 30 秒事件窗口內（此後的窗口內碰巧沒有擲到首領的機會，
    // 所以另以「已達成／已過期的任務 → 序列與沒帶任務相同」確認過濾）
    [Fact]
    public void Adventure_MissionMonsterOnlyWhileActive()
    {
        var chiefHunt = TrackedMissions.Single("test-chief-hunt", "測試首領", "test-sprite-chief", Required: 1, Progress: 0, AcceptedAt: From, ExpiresAt: null);

        var without = RunWithMissions(Hero(), seconds: 60, [], random: new SeededRandom(7), monsters: ChiefPlace);
        var with = RunWithMissions(Hero(), seconds: 60, [chiefHunt], random: new SeededRandom(7), monsters: ChiefPlace);

        Assert.DoesNotContain(without.Events.OfType<EncounterEvent>(), encounter => encounter.MonsterId == "test-sprite-chief");
        var events = with.Events.ToList();
        var completed = events.FindIndex(battleEvent => battleEvent is MissionProgressEvent { MissionId: "test-chief-hunt" });
        Assert.True(completed >= 0);
        var lastEncounterBefore = events.Take(completed).OfType<EncounterEvent>().Last();
        Assert.Equal("test-sprite-chief", lastEncounterBefore.MonsterId);
        Assert.NotEmpty(events.Skip(completed).OfType<EncounterEvent>());
        Assert.DoesNotContain(events.Skip(completed).OfType<EncounterEvent>(), encounter => encounter.MonsterId == "test-sprite-chief");
        TrackedMissions.AssertSame([chiefHunt.WithProgress(1)], with.Missions);
        Assert.Equal(1, with.Summary.MissionKills);

        // 已達成、已過期或尚未接取的任務不讓首領出現：遭遇序列與沒帶任務時完全相同
        var reached = RunWithMissions(Hero(), seconds: 60, [chiefHunt.WithProgress(1)], random: new SeededRandom(7), monsters: ChiefPlace);
        var expired = RunWithMissions(Hero(), seconds: 60, [chiefHunt with { ExpiresAt = From }], random: new SeededRandom(7), monsters: ChiefPlace);
        var notYetAccepted = RunWithMissions(
            Hero(), seconds: 60, [chiefHunt with { AcceptedAt = From.AddSeconds(60) }], random: new SeededRandom(7), monsters: ChiefPlace);
        Assert.Equal(Monsters(without), Monsters(reached));
        Assert.Equal(Monsters(without), Monsters(expired));
        Assert.Equal(Monsters(without), Monsters(notYetAccepted));

        static string[] Monsters(AdventureRun run) => run.Events.OfType<EncounterEvent>().Select(encounter => encounter.MonsterId).ToArray();
    }

    // 遇敵兩擲都是 0.99：rand(0,100) = 99（雪精 meet 0、首領 meet 60 都入選），NextInt(2) = 1 → 可出現時一定是首領；
    // 首領不在候選時只剩雪精。其餘擲骰 0.0（同 dummy，第 1 回合擊倒）。先量出一場用掉的亂數數 N，
    // 讓第二場的遇敵兩擲也是 0.99：第一場擊倒首領、任務達成後，第二場只能是雪精
    [Fact]
    public void Adventure_MissionMonsterStopsOnceMissionIsMet()
    {
        MonsterDefinition[] place =
        [
            Monster("test-sprite", placeId: 1, subPlaces: 1),
            Monster("test-sprite-chief", meet: 60, placeId: 1, subPlaces: 1) with { MissionId = "test-chief-hunt" },
        ];
        var chiefHunt = TrackedMissions.Single("test-chief-hunt", "測試首領", "test-sprite-chief", Required: 1, Progress: 0, AcceptedAt: From, ExpiresAt: null);
        var baseline = new ScriptedRandom(0.0);
        RunWithMissions(Hero(), seconds: 1, [], random: baseline, monsters: place);
        double[] Script() => [0.99, 0.99, .. Enumerable.Repeat(0.0, baseline.Consumed - 2), 0.99, 0.99];

        var run = RunWithMissions(Hero(), seconds: 4, [chiefHunt], random: new ScriptedRandom(0.0, Script()), monsters: place);
        var without = RunWithMissions(Hero(), seconds: 4, [], random: new ScriptedRandom(0.0, Script()), monsters: place);

        Assert.Equal(["test-sprite-chief", "test-sprite"], run.Events.OfType<EncounterEvent>().Select(encounter => encounter.MonsterId));
        Assert.Equal(["test-sprite", "test-sprite"], without.Events.OfType<EncounterEvent>().Select(encounter => encounter.MonsterId));
        TrackedMissions.AssertSame([chiefHunt.WithProgress(1)], run.Missions);
    }

    // 會還手的怪：hp 200、at 15、df 5，一場打好幾回合，讓寵物有機會代擋與出手
    private static readonly MonsterDefinition Brute = Monster("test-brute", hp: 200, at: 15, df: 5);

    private static AdventureRun RunWithPet(Character character, double seconds, Pet? pet, IRandomSource random, params MonsterDefinition[] monsters) =>
        AdventureSimulator.Run(
            character, Warrior, Place, monsters.Length == 0 ? [Brute] : monsters, ItemOf, [], From, From.AddSeconds(seconds), random,
            companions: new AdventureCompanions(pet));

    // 親密度 255、出擊值 255（積極、玩家 au 10 對 10）→ 出擊率 21；代擋量 5000 整段都用不完
    [Fact]
    public void Adventure_CarriedPetJoinsEveryFight()
    {
        var pet = TestPets.Create("小雪", "test-sprite", defense: 5000, spirit: 255, intimacy: 255);

        var run = RunWithPet(Hero(), seconds: 60, pet, new SeededRandom(7));

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        Assert.NotEmpty(encounters);
        Assert.All(encounters, encounter => Assert.Equal(("小雪", "test-sprite"), (encounter.PetName, encounter.PetMonsterId)));
        Assert.NotEmpty(run.Events.OfType<PetGuardEvent>());
        Assert.NotEmpty(run.Events.OfType<PetAttackEvent>());
    }

    // 代擋量 1：每場第一次代擋就退場（之後不再代擋），但下一場重新建立、又能代擋一次 → 不只一次退場，且各在不同場次
    [Fact]
    public void Adventure_PetGuardResetsEachFight()
    {
        var pet = TestPets.Create("小雪", "test-sprite", defense: 1, spirit: 255, intimacy: 255);

        var run = RunWithPet(Hero(), seconds: 60, pet, new SeededRandom(7));

        var guardsPerFight = new List<int>();
        foreach (var battleEvent in run.Events)
        {
            if (battleEvent is EncounterEvent)
            {
                guardsPerFight.Add(0);
            }
            else if (battleEvent is PetGuardEvent guard)
            {
                Assert.True(guard.Retired);
                Assert.Equal(0, guard.GuardAfter);
                guardsPerFight[^1]++;
            }
        }

        Assert.All(guardsPerFight, count => Assert.InRange(count, 0, 1));
        Assert.True(guardsPerFight.Count(count => count == 1) >= 2);
    }

    // 沒有攜帶寵物（companions 為 null 或 CarriedPet 為 null）：事件與擲骰都和計畫 8 相同
    [Fact]
    public void Adventure_WithoutCarriedPetKeepsSequence()
    {
        var baseline = Run(Hero(), seconds: 60, new SeededRandom(7), Brute);
        var withoutPet = RunWithPet(Hero(), seconds: 60, pet: null, new SeededRandom(7));

        Assert.Equivalent(baseline.Events, withoutPet.Events, strict: true);
        Assert.All(withoutPet.Events.OfType<EncounterEvent>(), encounter => Assert.Null(encounter.PetName));
        Assert.Equal(baseline.Summary, withoutPet.Summary);
        Assert.Equal(baseline.Character, withoutPet.Character);
    }

    private static CaptureState Net(int quantity) => new("capture-net", "捕獸網", Multiplier: 1, quantity);

    private static AdventureRun RunWithCapture(
        Character character, double seconds, CaptureState? capture, IRandomSource random, int petCount = 0, Pet? carried = null,
        IReadOnlyList<ActiveMission>? missions = null, params MonsterDefinition[] monsters) =>
        AdventureSimulator.Run(
            character, Warrior, Place, monsters.Length == 0 ? [Dummy] : monsters, ItemOf, [], From, From.AddSeconds(seconds), random,
            missions: missions, companions: new AdventureCompanions(carried, petCount, capture));

    // 同 Adventure_DropsNothingOnLossesAndTimeouts 的敗場與超限：擲骰 0.0 若有擲捕捉一定損壞，但兩者都不擲
    [Fact]
    public void Adventure_CapturesOnWinsOnly()
    {
        var brute = Monster("brute", hp: 60, at: 40, df: 1000);
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);

        var lost = RunWithCapture(Hero(), seconds: 1, Net(5), new ScriptedRandom(0.0), monsters: brute);
        var timedOut = RunWithCapture(Hero() with { Base = Hero().Base with { Vitality = 1000 } }, seconds: 1, Net(5), new ScriptedRandom(0.0), monsters: rock);

        Assert.Equal((1, 1), (lost.Summary.Losses, timedOut.Summary.Timeouts));
        foreach (var run in new[] { lost, timedOut })
        {
            Assert.Empty(run.Events.OfType<CaptureEvent>());
            Assert.Equal(5, run.SupplyRemaining);
            Assert.Empty(run.Captured);
            Assert.Equal(0, run.Summary.PetsCaptured);
        }
    }

    // 沒有捕捉道具（或數量 0）：擲骰與事件都和計畫 8 相同
    [Fact]
    public void Adventure_NoCaptureDiceWithoutItem()
    {
        var baseline = Run(Hero(), seconds: 60, new SeededRandom(7));
        var withoutItem = RunWithCapture(Hero(), seconds: 60, capture: null, new SeededRandom(7));
        var emptyItem = RunWithCapture(Hero(), seconds: 60, Net(0), new SeededRandom(7));

        Assert.True(baseline.Summary.Wins > 0);
        foreach (var run in new[] { withoutItem, emptyItem })
        {
            Assert.Equivalent(baseline.Events, run.Events, strict: true);
            Assert.Equal(baseline.Summary, run.Summary);
            Assert.Equal(baseline.Character, run.Character);
            Assert.Empty(run.Captured);
        }
    }

    // 攜帶寵物時不捕捉：事件與只帶寵物、沒裝捕捉道具時相同，道具不減
    [Fact]
    public void Adventure_NoCaptureWhileCarryingPet()
    {
        var pet = TestPets.Create("小雪", "test-sprite");

        var petOnly = RunWithCapture(Hero(), seconds: 60, capture: null, new SeededRandom(7), carried: pet);
        var withItem = RunWithCapture(Hero(), seconds: 60, Net(5), new SeededRandom(7), carried: pet);

        Assert.True(petOnly.Summary.Wins > 0);
        Assert.Equivalent(petOnly.Events, withItem.Events, strict: true);
        Assert.Equal(petOnly.Summary, withItem.Summary);
        Assert.Empty(withItem.Captured);
        Assert.Equal(5, withItem.SupplyRemaining);
    }

    // 擲骰 0.0：每個勝場都損壞（−1）且捕捉擲 1 必中；30 秒 11 場全勝。寵物數加上這次捕到的不超過 3：
    // 0 隻 → 前 3 場各捕一隻；2 隻 → 只捕第一場；3 隻 → 照擲但不捕。道具 20 − 每場損壞 − 捕到的數量
    [Theory]
    [InlineData(0, 3)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public void Adventure_CaptureSkipsWhenPetsFull(int petCount, int expectedCaptures)
    {
        var run = RunWithCapture(Hero() with { NextLevelExperience = 1_000_000 }, seconds: 30, Net(20), new ScriptedRandom(0.0), petCount);

        Assert.Equal(11, run.Summary.Wins);
        Assert.Equal(expectedCaptures, run.Captured.Count);
        Assert.Equal(expectedCaptures, run.Summary.PetsCaptured);
        Assert.Equal(20 - run.Summary.Wins - expectedCaptures, run.SupplyRemaining);
        var captures = run.Events.OfType<CaptureEvent>().ToList();
        Assert.Equal(run.Summary.Wins, captures.Count);
        Assert.All(captures, capture => Assert.True(capture.ItemBroken));
        Assert.Equal(expectedCaptures, captures.Count(capture => capture.CapturedMonsterId is not null));
        Assert.All(captures.Take(expectedCaptures), capture => Assert.Equal("dummy", capture.CapturedMonsterId));
    }

    // 先量出沒有捕捉道具時一場勝場消耗的亂數數 N（dummy 沒有掉落、不升級），再讓緊接的兩擲是 0.5：
    // 損壞擲 21、捕捉擲 51 → 兩擲都消耗了，但沒損壞也沒捕到，所以沒有 CaptureEvent、數量不變
    [Fact]
    public void Adventure_NoCaptureEventWhenNothingHappens()
    {
        var baseline = new ScriptedRandom(0.0);
        Run(Hero(), seconds: 1, baseline);
        var random = new ScriptedRandom(0.0, [.. Enumerable.Repeat(0.0, baseline.Consumed), 0.5, 0.5]);

        var run = RunWithCapture(Hero(), seconds: 1, Net(5), random);

        Assert.Equal(1, run.Summary.Wins);
        Assert.Equal(baseline.Consumed + 2, random.Consumed);
        Assert.Empty(run.Events.OfType<CaptureEvent>());
        Assert.Empty(run.Captured);
        Assert.Equal(5, run.SupplyRemaining);
    }

    // 第一場就升級（Exp 999 + 146 > 1000）且有任務：同一場是 RewardEvent → CaptureEvent → MissionProgressEvent → LevelUpEvent。
    // 擲骰 0.0：損壞 −1、捕到 −1 → 剩 3；能力 rand(1,10) = 1 × 倍率 1、個性 1 積極，捕捉時刻 = 擊殺時刻
    [Fact]
    public void Adventure_CaptureEventFollowsReward()
    {
        var run = RunWithCapture(Hero() with { Experience = 999 }, seconds: 1, Net(5), new ScriptedRandom(0.0), missions: [Hunt()], monsters: Sprite);

        Assert.Equal(
            [typeof(RewardEvent), typeof(CaptureEvent), typeof(MissionProgressEvent), typeof(LevelUpEvent)],
            run.Events.Where(battleEvent => battleEvent is RewardEvent or CaptureEvent or MissionProgressEvent or LevelUpEvent)
                .Select(battleEvent => battleEvent.GetType()));
        Assert.Equal(new CaptureEvent(0, "捕獸網", ItemBroken: true, CapturedMonsterId: "test-sprite", CapturedMonsterName: "test-sprite", Remaining: 3), run.Events.OfType<CaptureEvent>().Single());
        Assert.Equal(run.Events.OfType<RewardEvent>().Single().AtSeconds, run.Events.OfType<CaptureEvent>().Single().AtSeconds);
        Assert.Equal([new CapturedPet("test-sprite", "test-sprite", PetPersonality.Aggressive, 1, 1, 1, From)], run.Captured);
        Assert.Equal(1, run.Summary.PetsCaptured);
        Assert.Equal(3, run.SupplyRemaining);
    }

    private static AdventureRun RunWithBuffs(
        Character character, double seconds, IReadOnlyList<CharacterBuff>? buffs, IRandomSource random, params MonsterDefinition[] monsters) =>
        RunWithLodging(character, seconds, buffs, lodging: null, random, monsters);

    private static AdventureRun RunWithLodging(
        Character character, double seconds, IReadOnlyList<CharacterBuff>? buffs, CharacterBuff? lodging, IRandomSource random, params MonsterDefinition[] monsters) =>
        AdventureSimulator.Run(
            character,
            Warrior,
            Place,
            monsters.Length == 0 ? [Dummy] : monsters,
            ItemOf,
            [],
            From,
            From.AddSeconds(seconds),
            random,
            buffs: buffs,
            lodging: lodging);

    // 每個勝場的（開打秒數, 經驗, 金錢, 熟練度）；事件裡 RewardEvent 接在它那一場的 EncounterEvent 之後
    private static List<(double Start, long Exp, long Money, long JobExp)> RewardsByFightStart(AdventureRun run)
    {
        var rewards = new List<(double, long, long, long)>();
        var start = 0.0;
        foreach (var battleEvent in run.Events)
        {
            if (battleEvent is EncounterEvent encounter)
            {
                start = encounter.AtSeconds;
            }
            else if (battleEvent is RewardEvent reward)
            {
                rewards.Add((start, reward.Experience, reward.Money, reward.JobExperience));
            }
        }

        return rewards;
    }

    // 經驗密藥在 From + 30 秒到期；50 秒的事件窗口是 [20, 50)，兩邊都有勝場。經驗密藥不影響戰鬥，同種子下逐場對齊：
    // 30 秒前開打的勝場經驗 = round(無密藥 × 1.5)，之後的相同。NextExp 設得很大，避免升級讓兩條時間軸分岔
    [Fact]
    public void Adventure_ElixirAppliesOnlyWithinItsWindow()
    {
        var hero = Hero() with { NextLevelExperience = 1_000_000_000 };
        IReadOnlyList<CharacterBuff> buffs = [new(BuffKind.Exp, 1.5, From.AddSeconds(-3570), From.AddSeconds(30))];

        var plain = RewardsByFightStart(RunWithBuffs(hero, 50, null, new SeededRandom(7)));
        var buffed = RewardsByFightStart(RunWithBuffs(hero, 50, buffs, new SeededRandom(7)));

        Assert.Equal(plain.Count, buffed.Count);
        Assert.Contains(plain, reward => reward.Start < 30);
        Assert.Contains(plain, reward => reward.Start >= 30);
        Assert.All(plain.Zip(buffed), pair =>
        {
            Assert.Equal(pair.First.Start, pair.Second.Start);
            Assert.Equal(pair.First.Start < 30 ? DerivedStats.RoundLikePhp(pair.First.Exp * 1.5) : pair.First.Exp, pair.Second.Exp);
            Assert.Equal((pair.First.Money, pair.First.JobExp), (pair.Second.Money, pair.Second.JobExp));
        });
    }

    // 經驗與熟練密藥全程生效：勝場經驗 146 → 219、熟練度 12 → 18，金錢 2 不變；
    // 超限的部分經驗 43 不受經驗密藥影響（手算見 Adventure_TimeoutGivesPartialExpOnly）
    [Fact]
    public void Adventure_ElixirDoesNotChangeMoneyOrTimeoutExp()
    {
        IReadOnlyList<CharacterBuff> buffs =
        [
            new(BuffKind.Exp, 1.5, From, From.AddHours(1)),
            new(BuffKind.JobExp, 1.5, From, From.AddHours(1)),
        ];

        var win = RunWithBuffs(Hero(), seconds: 1, buffs, new ScriptedRandom(0.0));
        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 } };
        var timeout = RunWithBuffs(tough, seconds: 1, buffs, new ScriptedRandom(0.0), Monster("rock", hp: 60, at: 40, df: 1000));

        Assert.Equal(new RewardEvent(0, Experience: 219, Money: 2, JobExperience: 18), win.Events.OfType<RewardEvent>().Single());
        Assert.Equal([new JobGain("warrior", 18, 18)], win.JobGains);
        Assert.Equal(new RewardEvent(32, Experience: 43, Money: 0, JobExperience: 0), timeout.Events.OfType<RewardEvent>().Single());
    }

    // 沒有密藥時（空清單或 null）擲骰序列與計畫 9 相同：事件、摘要、角色逐一相等
    [Fact]
    public void Adventure_NoBuffsKeepsRollSequence()
    {
        var withNull = RunWithBuffs(Hero(), 600, null, new SeededRandom(11));
        var withEmpty = RunWithBuffs(Hero(), 600, [], new SeededRandom(11));

        Assert.Equal(withNull.Events, withEmpty.Events);
        Assert.Equal(withNull.Summary, withEmpty.Summary);
        Assert.Equal(withNull.Character, withEmpty.Character);
        Assert.Equal(withNull.ConsumedSeconds, withEmpty.ConsumedSeconds);
    }

    // 房型加成在 From + 30 秒到期（半開區間）；同種子下逐場對齊：到期前開打的勝場經驗 = Scale(無加成, 1.1)，之後相同
    [Fact]
    public void Adventure_LodgingBoostsWinExpInsideWindow()
    {
        var hero = Hero() with { NextLevelExperience = 1_000_000_000 };
        var lodging = new CharacterBuff(BuffKind.Exp, 1.1, From, From.AddSeconds(30));

        var plain = RewardsByFightStart(RunWithLodging(hero, 50, null, null, new SeededRandom(7)));
        var boosted = RewardsByFightStart(RunWithLodging(hero, 50, null, lodging, new SeededRandom(7)));

        Assert.Equal(plain.Count, boosted.Count);
        Assert.Contains(plain, reward => reward.Start < 30);
        Assert.Contains(plain, reward => reward.Start >= 30);
        Assert.All(plain.Zip(boosted), pair =>
        {
            Assert.Equal(pair.First.Start, pair.Second.Start);
            Assert.Equal(pair.First.Start < 30 ? Buffs.Scale(pair.First.Exp, 1.1) : pair.First.Exp, pair.Second.Exp);
            Assert.Equal((pair.First.Money, pair.First.JobExp), (pair.Second.Money, pair.Second.JobExp));
        });
    }

    // 經驗密藥 1.5 與學徒閣樓 1.1 相乘：146 → Scale(146, 1.65) = 241
    [Fact]
    public void Adventure_LodgingAndElixirMultiply()
    {
        IReadOnlyList<CharacterBuff> buffs = [new(BuffKind.Exp, 1.5, From, From.AddHours(1))];
        var lodging = new CharacterBuff(BuffKind.Exp, 1.1, From, From.AddHours(8));

        var run = RunWithLodging(Hero(), seconds: 1, buffs, lodging, new ScriptedRandom(0.0));

        Assert.Equal(Buffs.Scale(146, 1.65), run.Events.OfType<RewardEvent>().Single().Experience);
    }

    // 沒有房型加成時（不傳或 null）擲骰序列不變：事件、摘要、角色逐一相等
    [Fact]
    public void Adventure_NoLodgingKeepsRollSequence()
    {
        var omitted = AdventureSimulator.Run(
            Hero(), Warrior, Place, [Dummy], ItemOf, [], From, From.AddSeconds(600), new SeededRandom(11));
        var withNull = RunWithLodging(Hero(), 600, null, null, new SeededRandom(11));

        Assert.Equal(omitted.Events, withNull.Events);
        Assert.Equal(omitted.Summary, withNull.Summary);
        Assert.Equal(omitted.Character, withNull.Character);
        Assert.Equal(omitted.ConsumedSeconds, withNull.ConsumedSeconds);
    }

    // 物攻密藥只在開打時刻落在窗口內的場次生效：打不倒的怪物在超限前累計的傷害變大；已到期的密藥事件與沒喝相同
    [Fact]
    public void Adventure_AttackElixirStrengthensThePlayer()
    {
        var sturdy = Monster("sturdy", hp: 100_000);
        IReadOnlyList<CharacterBuff> buffs = [new(BuffKind.Attack, 1.2, From, From.AddHours(1))];
        IReadOnlyList<CharacterBuff> expired = [new(BuffKind.Attack, 1.2, From.AddHours(-1), From)];

        var tough = Hero() with { Base = Hero().Base with { Vitality = 1000 } };

        var plain = RunWithBuffs(tough, 1, null, new ScriptedRandom(0.5), sturdy);
        var buffed = RunWithBuffs(tough, 1, buffs, new ScriptedRandom(0.5), sturdy);
        var after = RunWithBuffs(tough, 1, expired, new ScriptedRandom(0.5), sturdy);

        Assert.True(buffed.Summary.Timeouts == 1 && plain.Summary.Timeouts == 1);
        Assert.True(
            buffed.Events.OfType<TimeoutEvent>().Single().DamageDealt > plain.Events.OfType<TimeoutEvent>().Single().DamageDealt);
        Assert.Equal(plain.Events, after.Events);
    }

    [Fact]
    public void Run_MonsterSkillsApply_InAdventure()
    {
        var skill = new SkillDefinition("爆氣", "爆氣", SkillType.Active, SkillTiming.BeforeAttack, "",
            [new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 100)], [],
                [new StatChangeEffect(SkillTarget.Caster, SkillStat.Attack, 5, null, null, Rounds: 0)])]);
        var monster = Dummy with { Skills = [new EquippedSkill(skill, 1, 1)] };

        var run = Run(Hero(), seconds: 30, random: null, monster);

        Assert.Contains(run.Events.OfType<SkillEvent>(), cast => cast.Caster == CombatSide.Monster && cast.SkillName == "爆氣");
        Assert.Equal(0, run.Summary.SkillsCast);
    }
}
