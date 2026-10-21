using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Adventure;

// 除非另外指定，擲骰一律 0.0：rand 取下限、NextInt 取 0、遇敵擲骰 rand(0,100) = 0 → 只有 Meet 0 的怪物入選。
// 預設角色（TestCharacters.Default）：Lv1、Base (12,12,12,10,12,10,10)、地屬性、Hp 60／350、Sp 25／91、行動力 50。
// 對上 dummy（agi 12、df 1、無屬性）：agi 相同 → 30% 先攻，擲 1 ≤ 30 玩家先攻；必中；暴擊 1 < 10、倍率 1.2 → A 14.4；
// (14.4·14.4 − 1) = 206.36，加 rand(⌊14.33/3⌋, ⌊14.33/2⌋) = 4 → 210 ≥ 100，第 1 回合在 0 秒擊倒，怪物不出手。
public class AdventureSimulatorTests
{
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly JobDefinition Warrior = new(
        "warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [],
        new JobGrowth(new(2, 3), new(0, 1), new(1, 2), new(1, 2), new(1, 2), new(0, 1), new(0, 1)));

    private static MonsterDefinition Monster(
        string id, int level = 5, int hp = 100, int at = 10, int df = 1, int jobExp = 12, int meet = 0) =>
        new(id, id, level, hp, Sp: 5, at, Mat: 0, df, Mdf: 1, Agi: 12, Au: 10, Element: 0, jobExp, meet);

    private static readonly MonsterDefinition Dummy = Monster("dummy");

    private static Character Hero() => TestCharacters.Default(accountId: 1, "勇者", "stage", From);

    private static readonly EquippedSkill Burst = new(TestSkills.Create("burst", "warrior", SkillType.Active), Level: 1, Chance: 100, Slot: 1);

    private static AdventureRun Run(Character character, double seconds, IRandomSource? random = null, params MonsterDefinition[] monsters) =>
        Run(character, seconds, skills: [], random, monsters);

    private static AdventureRun Run(
        Character character, double seconds, IReadOnlyList<EquippedSkill> skills, IRandomSource? random = null, params MonsterDefinition[] monsters)
    {
        var stageMonsters = monsters.Length == 0 ? [Dummy] : monsters;
        var stage = new StageDefinition("stage", "關卡", stageMonsters.Select(monster => monster.Id).ToList());
        return AdventureSimulator.Run(
            character,
            Warrior,
            stage,
            skills,
            id => stageMonsters.Single(monster => monster.Id == id),
            new StaminaState(character.ActNum, character.ActNumTime),
            From,
            From.AddSeconds(seconds),
            random ?? new ScriptedRandom(0.0));
    }

    // 擊殺 dummy（Lv5、at 10、mat 0、hp 100、jobExp 12）、玩家 Lv1：
    // 經驗 base 5 → rand(5, 10) = 5；×1.3 = 6.5；5 × 6.5 × 5 × 1 × 0.18 = 29.25；Lv ≤ 15 ×2.5 = 73.125；×2 = 146.25 → 146（行動力不足 ×0.5 → 73.125 → 73）
    // 金錢 rand(5/2.5, 5) = 2 × rand(10/5, 100/10×0.4) = 2 → 2 × 2 × 0.4 = 1.6 → 2（×0.5 → 0.8 → 1）；熟練度 ⌊12 × f⌋ = 12（×0.5 → 6）
    [Fact]
    public void Adventure_AppliesLevelUpsAndJobGains()
    {
        var run = Run(Hero() with { Exp = 999 }, seconds: 1);

        // 999 + 146 = 1145 > 1000 → Lv2，剩 145，下一級 6·4 + 1080·2 = 2184；成長各取下限 (2,0,1,1,1,0,0)
        var growth = new BaseStats(2, 0, 1, 1, 1, 0, 0);
        Assert.Equal(new RewardEvent(0, Exp: 146, Money: 2, JobExp: 12), run.Events.OfType<RewardEvent>().Single());
        Assert.Equal(new LevelUpEvent(0, NewLevel: 2, growth), run.Events.OfType<LevelUpEvent>().Single());
        Assert.Equal(2, run.Character.Level);
        Assert.Equal(145, run.Character.Exp);
        Assert.Equal(2184, run.Character.NextExp);
        Assert.Equal(2002, run.Character.Money);
        Assert.Equal(new BaseStats(14, 12, 13, 11, 13, 10, 10), run.Character.Base);
        Assert.Equal(new AdvanceSummary(Fights: 1, Wins: 1, Losses: 0, Timeouts: 0, Exp: 146, Money: 2, JobExp: 12, LevelsGained: 1, SkillsCast: 0), run.Summary);
        Assert.Equal([new JobGain("warrior", 12, 12)], run.JobGains);
        Assert.True(run.EventsCoverAll);
    }

    [Fact]
    public void Adventure_EncounterCarriesHpAndSpOfBothSides()
    {
        var run = Run(Hero(), seconds: 1);

        Assert.Equal(
            new EncounterEvent(0, "dummy", MonsterHp: 100, MonsterHpMax: 100, PlayerHp: 60, PlayerHpMax: 350, PlayerSp: 25, PlayerSpMax: 91, StaminaDepleted: false),
            run.Events[0]);
        Assert.Contains(run.Events, battleEvent => battleEvent is RoundEvent);
        Assert.Contains(run.Events, battleEvent => battleEvent is AttackEvent);
    }

    // 住宿：Lv ≤ 100 免費、回滿 HP 350／SP 91、死亡等 15 秒；第一場在 15 秒以滿血開打
    [Fact]
    public void Adventure_RestsBeforeNextFight_WhenDead()
    {
        var run = Run(Hero() with { Hp = 0 }, seconds: 16);

        Assert.Equal(new RestEvent(0, Cost: 0, ExpPenalty: false, WaitSeconds: 15), run.Events[0]);
        Assert.Equal(
            new EncounterEvent(15, "dummy", MonsterHp: 100, MonsterHpMax: 100, PlayerHp: 350, PlayerHpMax: 350, PlayerSp: 91, PlayerSpMax: 91, StaminaDepleted: false),
            run.Events[1]);
        Assert.Equal(1, run.Summary.Fights);
    }

    // 生命 2 → HpMax 100、門檻 15：14 先住宿（未死亡等 5 秒）再以滿血開打；剛好 15 不住宿
    [Theory]
    [InlineData(14, true)]
    [InlineData(15, false)]
    public void Adventure_RestsBeforeFight_WhenHpBelowThreshold(int hp, bool rests)
    {
        var hero = Hero() with { Base = Hero().Base with { Life = 2 }, Hp = hp };

        var run = Run(hero, seconds: 6);

        if (rests)
        {
            Assert.Equal(new RestEvent(0, Cost: 0, ExpPenalty: false, WaitSeconds: 5), run.Events[0]);
            var encounter = Assert.IsType<EncounterEvent>(run.Events[1]);
            Assert.Equal(5, encounter.AtSeconds);
            Assert.Equal(100, encounter.PlayerHp);
            Assert.Equal(100, encounter.PlayerHpMax);
        }
        else
        {
            var encounter = Assert.IsType<EncounterEvent>(run.Events[0]);
            Assert.Equal(0, encounter.AtSeconds);
            Assert.Equal(15, encounter.PlayerHp);
            Assert.Equal(100, encounter.PlayerHpMax);
        }
    }

    // 最小重現：31 秒、窗口從 1 秒起；0 秒的住宿在窗口外被略去，但 15 秒起的每一場戰鬥都在窗口內 → 戰鬥沒有缺漏
    [Fact]
    public void Adventure_EventsCoverAll_WhenOnlyARestIsSkipped()
    {
        var run = Run(Hero() with { Hp = 0 }, seconds: 31);

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
        Assert.Equal(0, run.Character.Hp);
        Assert.Equal(new AdvanceSummary(1, Wins: 0, Losses: 1, Timeouts: 0, Exp: 0, Money: 0, JobExp: 0, LevelsGained: 0, SkillsCast: 0), run.Summary);
        Assert.Empty(run.JobGains);
        Assert.Equal(2.8, run.ConsumedSeconds, 9);
    }

    // 每場 1 回合 0.8 秒 + 場間 2 秒 = 2.8 秒（行動力 50 足夠 43 場）；120 秒內在 0、2.8、…、117.6 開打共 43 場。
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

    // 行動力 0 → 消耗後 −1：f_down 0.5、場間 12 秒；存回時不低於 0（WOG 只處理 0 與 −1 兩種情況）
    [Fact]
    public void Adventure_DepletedStaminaFlagsEncounterAndHalvesRewards()
    {
        var run = Run(Hero() with { ActNum = 0, ActNumTime = From }, seconds: 1);

        Assert.True(run.Events.OfType<EncounterEvent>().Single().StaminaDepleted);
        Assert.Equal(new RewardEvent(0, Exp: 73, Money: 1, JobExp: 6), run.Events.OfType<RewardEvent>().Single());
        Assert.Equal(73, RewardFormulas.Exp(Dummy, Dummy.Hp, playerLevel: 1, killed: true, fDown: 0.5, new ScriptedRandom(0.0)));
        Assert.Equal(new StaminaState(0, From), run.Stamina);
        Assert.Equal(12.8, run.ConsumedSeconds, 9);
    }

    // 行動力 1：第一場消耗後為 0，f_down 仍為 1（WOG act == 0 那一場全額）但場間已是 12 秒；
    // 第二場消耗後為 −1 → f_down 0.5。兩場都在第 1 回合擊倒 dummy（Lv1 未升級），開打時間 0 與 0.8 + 12 = 12.8。
    // 全額：exp 146、money 2、jobExp 12；半額：73.125 → 73、0.8 → 1、⌊6⌋ = 6（手算見 Adventure_AppliesLevelUpsAndJobGains 上方）
    [Fact]
    public void Adventure_LastStaminaPointStillPaysFull()
    {
        var run = Run(Hero() with { ActNum = 1, ActNumTime = From }, seconds: 14);

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        Assert.Equal(2, encounters.Count);
        Assert.Equal(12.8, encounters[1].AtSeconds - encounters[0].AtSeconds, 9);
        Assert.Equal([false, true], encounters.Select(encounter => encounter.StaminaDepleted));
        Assert.Equal(
            [new RewardEvent(0, Exp: 146, Money: 2, JobExp: 12), new RewardEvent(12.8, Exp: 73, Money: 1, JobExp: 6)],
            run.Events.OfType<RewardEvent>().Select(reward => reward with { AtSeconds = Math.Round(reward.AtSeconds, 9) }));
    }

    // 行動力 0 起跑：每場消耗後 −1 → 半額、場間 12 秒，存回 0；開打時間 0、12.8、…、179.2（第 14 場仍不滿 180 秒）。
    // 第 15 場在 15 × 12.8 = 192 秒開打前回復：192 / 180 ≥ 1 → +6、LastRegenAt 推進到 192 秒；
    // 消耗後 5 → 全額、場間回到 2 秒，之後 194.8、197.6 開打（200.4 已超過 200 秒）。最終行動力 6 − 3 = 3。
    // NextExp 設得很大，避免升級改變獎勵：全額 146／2／12、半額 73／1／6（手算見 Adventure_AppliesLevelUpsAndJobGains 上方）
    [Fact]
    public void Adventure_RegeneratesStaminaOnTheTimeline()
    {
        var run = Run(Hero() with { ActNum = 0, ActNumTime = From, NextExp = 1_000_000 }, seconds: 200);

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        var rewards = run.Events.OfType<RewardEvent>().ToList();
        Assert.Equal([179.2, 192, 194.8, 197.6], encounters.Select(encounter => Math.Round(encounter.AtSeconds, 6)));
        Assert.Equal([true, false, false, false], encounters.Select(encounter => encounter.StaminaDepleted));
        Assert.Equal((Exp: 73L, Money: 1L, JobExp: 6L), (rewards[0].Exp, rewards[0].Money, rewards[0].JobExp));
        Assert.Equal((Exp: 146L, Money: 2L, JobExp: 12L), (rewards[1].Exp, rewards[1].Money, rewards[1].JobExp));
        Assert.Equal(2.8, encounters[2].AtSeconds - encounters[1].AtSeconds, 9);
        Assert.Equal(new StaminaState(3, From.AddSeconds(encounters[1].AtSeconds)), run.Stamina);
    }

    // rock（hp 60、at 40、df 1000）對 Df 1000 的角色：雙方每下 1 點，40 回合後超限（32 秒）。
    // 累計傷害 40 → round(40 / 1.5) = 27；等級以 5 × 0.6 = 3 計：base 20 → rand(20, 23) = 20；×1.3 = 26；
    // 3 × 26 × 3 × (27/60) × 0.18 = 18.954；未擊殺再 ×0.45 = 8.5293；×2.5 = 21.32325；×2 = 42.6465 → 43。沒有金錢與熟練度、不算敗場
    [Fact]
    public void Adventure_TimeoutGivesPartialExpOnly()
    {
        var tough = Hero() with { Base = Hero().Base with { Vit = 1000 } };
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);

        var run = Run(tough, seconds: 1, monsters: rock);

        Assert.Equal(new TimeoutEvent(32, 40, 40), run.Events.OfType<TimeoutEvent>().Single());
        Assert.Equal(new RewardEvent(32, Exp: 43, Money: 0, JobExp: 0), run.Events.OfType<RewardEvent>().Single());
        Assert.Equal(43, RewardFormulas.Exp(rock, 27, playerLevel: 1, killed: false, fDown: 1, new ScriptedRandom(0.0), levelOverride: 3));
        Assert.Equal(new AdvanceSummary(1, Wins: 0, Losses: 0, Timeouts: 1, Exp: 43, Money: 0, JobExp: 0, LevelsGained: 0, SkillsCast: 0), run.Summary);
        Assert.Equal(20, run.Character.Hp);
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

    // burst lv1 耗 SP 5、發動率 100（擲 1）：0 秒那場 25 → 20，2.8 秒那場從 20 開始 → 15；住宿以外不回 SP
    [Fact]
    public void Adventure_CarriesSpBetweenFights()
    {
        var run = Run(Hero(), seconds: 3, skills: [Burst]);

        Assert.Equal(2, run.Summary.Fights);
        Assert.Equal([20, 15], run.Events.OfType<SkillEvent>().Select(skill => skill.SpAfter));
        Assert.Equal([25, 20], run.Events.OfType<EncounterEvent>().Select(encounter => encounter.PlayerSp));
        Assert.Equal(15, run.Character.Sp);
    }

    // 120 秒 43 場（見 Adventure_KeepsOnlyLastThirtySecondsOfEvents）；SP 25 只夠前 5 場各施放一次（→ 0），
    // 這 5 場都在 90 秒之前、事件被略去，摘要仍要計入
    [Fact]
    public void Adventure_CountsSkillsCastAcrossDroppedFights()
    {
        var run = Run(Hero(), seconds: 120, skills: [Burst]);

        Assert.Equal(43, run.Summary.Fights);
        Assert.Empty(run.Events.OfType<SkillEvent>());
        Assert.Equal(5, run.Summary.SkillsCast);
        Assert.Equal(0, run.Character.Sp);
    }

    // 擲骰一律 0.5 → 發動擲骰 rand(1,100) = 51：欄位發動率 10 永遠不施放，100 則施放
    [Theory]
    [InlineData(10, false)]
    [InlineData(100, true)]
    public void Adventure_UsesSlotChanceForCasting(int chance, bool casts)
    {
        var run = Run(Hero(), seconds: 1, skills: [Burst with { Chance = chance }], new ScriptedRandom(0.5));

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
}
