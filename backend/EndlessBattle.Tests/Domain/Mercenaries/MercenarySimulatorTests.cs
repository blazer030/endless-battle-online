using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Mercenaries;

// 除非另外指定，擲骰一律 0.0。地區 1 的 dummy（Lv5、hp 100、at 10、df 1、Meet 0）：
// 滿血的預設角色第 1 回合以 210 點擊倒，怪物不出手；獎勵（Lv1）經驗 146、金錢 2、熟練度 12（手算見 AdventureSimulatorTests）
public class MercenarySimulatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    private static readonly JobDefinition Warrior = new(
        "warrior", "戰士", Tier: 1, ParentId: null, UnlockLevel: 1, ProficiencyRequired: 0, StatRequirements: [],
        new JobGrowth(new(2, 3), new(0, 1), new(1, 2), new(1, 2), new(1, 2), new(0, 1), new(0, 1)));

    private static readonly MercenaryDefinition Full = new("test", "測試傭兵", Price: 100, Fights: 10, MoneyPercent: 100, DropPercent: 100, ExpPercent: 100, DeathCost: 2);

    private static MonsterDefinition Monster(string id, int hp = 100, int at = 10, int df = 1, int placeId = 1) =>
        new(id, id, Level: 5, hp, Sp: 5, at, Mat: 0, df, Mdf: 1, Agi: 12, Au: 10, Element: 0, JobExp: 12, Meet: 0, placeId);

    private static readonly MonsterDefinition Dummy = Monster("dummy");

    private static Character Hero() => TestCharacters.Default(accountId: 1, "勇者", 1, 1, Now) with { NextExp = 1_000_000_000 };

    private static MercenaryContract Contract(int remaining, DateTimeOffset nextFightAt) =>
        new("test", "阿傭", remaining, PlaceId: 1, nextFightAt, HiredAt: Now.AddDays(-3));

    private static MercenaryRun Run(
        Character character,
        MercenaryContract contract,
        MercenaryDefinition? definition = null,
        IReadOnlyList<MonsterDefinition>? monsters = null,
        Pet? pet = null,
        IReadOnlyList<ActiveMission>? missions = null,
        IRandomSource? random = null,
        IReadOnlyList<EquippedSkill>? skills = null) =>
        MercenarySimulator.Run(
            character, Warrior, contract, definition ?? Full, monsters ?? [Dummy], skills ?? [], EquipmentBonus.Zero, pet, missions ?? [], Now,
            random ?? new ScriptedRandom(0.0));

    [Fact]
    public void Mercenary_FightsEveryHundredSeconds()
    {
        var start = Now.AddSeconds(-250);

        var run = Run(Hero(), Contract(10, start));

        Assert.Equal([start, start.AddSeconds(100), start.AddSeconds(200)], run.Fights.Select(fight => fight.At));
        Assert.Equal(Contract(7, start.AddSeconds(300)), run.Contract);
        Assert.All(run.Fights, fight => Assert.Equal(("dummy", MercenaryFightResult.Won, 146L, 2L), (fight.MonsterId, fight.Result, fight.Exp, fight.Money)));
        Assert.Equal(3 * 146, run.Character.Exp);
        Assert.Equal(2000 + 3 * 2, run.Character.Money);
        Assert.Equal(3 * 12, run.JobExp);
    }

    [Fact]
    public void Mercenary_DiscountsExpAndMoney()
    {
        var half = Full with { ExpPercent = 50, MoneyPercent = 50 };

        var full = Run(Hero(), Contract(10, Now.AddSeconds(-500)), Full, random: new SeededRandom(7));
        var discounted = Run(Hero(), Contract(10, Now.AddSeconds(-500)), half, random: new SeededRandom(7));

        Assert.Equal(6, full.Fights.Count);
        Assert.Contains(full.Fights, fight => fight.Exp > 0);
        Assert.Equal(
            full.Fights.Select(fight => (fight.MonsterId, fight.Result, MercenaryRules.Discount(fight.Exp, 50), MercenaryRules.Discount(fight.Money, 50))),
            discounted.Fights.Select(fight => (fight.MonsterId, fight.Result, fight.Exp, fight.Money)));
        Assert.Equal(full.JobExp, discounted.JobExp);
        Assert.Equal(full.Fights.Sum(fight => MercenaryRules.Discount(fight.Exp, 50)), discounted.Character.Exp);
    }

    // hornet（hp 300）：玩家第 1 回合 210 點打不死，怪物還手；Hp 1 的角色會倒下，傭兵以滿血開打所以第 2 回合擊倒。
    // 傭兵沒有補給參數也不住宿；角色的 Hp／Sp 原樣留著
    [Fact]
    public void Mercenary_FightsAtFullHealthWithoutTouchingCharacterHp()
    {
        var hornet = Monster("hornet", hp: 300);
        var wounded = Hero() with { Hp = 1, Sp = 3 };
        Assert.Equal(
            CombatSide.Monster,
            WogBattle.Run(CombatBridge.ToCombatant(wounded), CombatantState.From(hornet), playerPreferred: null, new ScriptedRandom(0.0)).Winner);

        var run = Run(wounded, Contract(10, Now), monsters: [hornet]);

        Assert.Equal(MercenaryFightResult.Won, Assert.Single(run.Fights).Result);
        Assert.Equal((1, 3), (run.Character.Hp, run.Character.Sp));
    }

    // 能力全 1 的角色打 brute（at 40、df 1000）：每場都輸（或超限），次數每場 −2
    [Fact]
    public void Mercenary_LossesCostDeathCost()
    {
        var weak = Hero() with { Base = new BaseStats(1, 1, 1, 1, 1, 1, 1) };
        var brute = Monster("brute", hp: 60, at: 40, df: 1000);

        var run = Run(weak, Contract(10, Now.AddSeconds(-200)), monsters: [brute]);

        Assert.Equal(3, run.Fights.Count);
        Assert.All(run.Fights, fight => Assert.NotEqual(MercenaryFightResult.Won, fight.Result));
        Assert.Equal(4, run.Contract!.Remaining);
        Assert.Equal(0, run.JobExp);
        Assert.Empty(run.DroppedItemIds);
    }

    // 超限：部分經驗照 RewardFormulas.TimeoutExp 再打折，沒有金錢，次數 −DeathCost
    [Fact]
    public void Mercenary_TimeoutGivesDiscountedPartialExp()
    {
        var tough = Hero() with { Base = Hero().Base with { Vit = 1000 } };
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);
        var half = Full with { ExpPercent = 50 };

        var run = Run(tough, Contract(10, Now), half, monsters: [rock]);

        var fight = Assert.Single(run.Fights);
        Assert.Equal((MercenaryFightResult.Timeout, 22L, 0L), (fight.Result, fight.Exp, fight.Money));
        Assert.Equal(MercenaryRules.Discount(RewardFormulas.TimeoutExp(rock, 40, 1, new ScriptedRandom(0.0)), 50), fight.Exp);
        Assert.Equal(8, run.Contract!.Remaining);
    }

    [Fact]
    public void Mercenary_StopsWhenFightsRunOut()
    {
        var run = Run(Hero(), Contract(2, Now.AddSeconds(-900)));

        Assert.Equal([Now.AddSeconds(-900), Now.AddSeconds(-800)], run.Fights.Select(fight => fight.At));
        Assert.Null(run.Contract);
    }

    // 兩天前就該開打：從 now − 86400 的格點起算，到 now（含）共 865 場
    [Fact]
    public void Mercenary_SkipsFightsOlderThanOneDay()
    {
        var run = Run(Hero(), Contract(10_000, Now.AddDays(-2)));

        Assert.Equal(865, run.Fights.Count);
        Assert.Equal(Now.AddSeconds(-86_400), run.Fights[0].At);
        Assert.Equal(Now, run.Fights[^1].At);
        Assert.Equal(Contract(10_000 - 865, Now.AddSeconds(100)), run.Contract);
    }

    // 不在 now 的格線上：NextFightAt = now − 2 天 − 30 秒，跳過的場次照原本的格點走，
    // 第一場是不早於 now − 86400 的格點 now − 86330，最後一場 now − 30，共 864 場
    [Fact]
    public void Mercenary_SkipsToTheFirstGridPointWithinOneDay()
    {
        var nextFightAt = Now.AddDays(-2).AddSeconds(-30);

        var run = Run(Hero(), Contract(10_000, nextFightAt));

        Assert.Equal(864, run.Fights.Count);
        Assert.Equal(Now.AddSeconds(-86_400 + 70), run.Fights[0].At);
        Assert.All(run.Fights, fight => Assert.Equal(0, (fight.At - nextFightAt).Ticks % TimeSpan.FromSeconds(100).Ticks));
        Assert.Equal(Now.AddSeconds(-30), run.Fights[^1].At);
        Assert.Equal(Contract(10_000 - 864, Now.AddSeconds(70)), run.Contract);
    }

    [Fact]
    public void Mercenary_NeverMeetsGlobalMonsters()
    {
        var ghost = Monster("ghost", placeId: 0);

        var run = Run(Hero(), Contract(100, Now.AddSeconds(-3000)), monsters: [ghost, Dummy], random: new SeededRandom(7));

        Assert.Equal(31, run.Fights.Count);
        Assert.All(run.Fights, fight => Assert.Equal("dummy", fight.MonsterId));
    }

    // 擊殺時刻＝場次時刻（now − 300、−200、−100、now 四場）：兩個任務都在第 2 場之前接取，第 1 場不算；
    // 每日任務在第 3、4 場之間（now − 50）過期，第 4 場也不算。以 now 計擊殺的話一般任務會是 4、每日任務是 0
    [Fact]
    public void Mercenary_CountsMissionKills()
    {
        var start = Now.AddSeconds(-300);
        var hunt = TrackedMissions.Single("hunt", "討伐", "dummy", Required: 10, Progress: 0, AcceptedAt: start.AddSeconds(50), ExpiresAt: null);
        var daily = TrackedMissions.Single("daily", "每日", "dummy", Required: 10, Progress: 0, AcceptedAt: start.AddSeconds(50), ExpiresAt: Now.AddSeconds(-50));

        var run = Run(Hero(), Contract(10, start), missions: [hunt, daily]);

        Assert.Equal(4, run.Fights.Count);
        TrackedMissions.AssertSame([hunt.WithProgress(3), daily.WithProgress(2)], run.Missions);
    }

    // 寶物 30% → 分母 ⌊10 × 0.9 × 1.7⌋ = 15。掉落是該場最後一骰（NextExp 很大、不升級），先數出全 0.0 時一場用掉幾骰，
    // 再把最後一骰換成 0.067：分母 15 → ⌊1.005⌋ + 1 = 2 落空；寶物 100%（分母 9）→ ⌊0.603⌋ + 1 = 1 掉落
    [Fact]
    public void Mercenary_AppliesDropDenominatorFactor()
    {
        var looter = Dummy with { Drops = [new DropEntry("ore", 10)] };
        var stingy = Full with { DropPercent = 30 };
        var counter = new ScriptedRandom(0.0);
        Run(Hero(), Contract(10, Now), stingy, monsters: [looter], random: counter);
        double[] script = [.. Enumerable.Repeat(0.0, counter.Consumed - 1), 0.067];

        var discounted = Run(Hero(), Contract(10, Now), stingy, monsters: [looter], random: new ScriptedRandom(0.0, script));
        var plain = Run(Hero(), Contract(10, Now), Full, monsters: [looter], random: new ScriptedRandom(0.0, script));

        Assert.Empty(Assert.Single(discounted.Fights).Drops);
        Assert.Empty(discounted.DroppedItemIds);
        Assert.Equal(["ore"], Assert.Single(plain.Fights).Drops);
        Assert.Equal(["ore"], plain.DroppedItemIds);
    }

    // 999 + 146 > 1000 → Lv2，成長取下限 (2,0,1,1,1,0,0)
    [Fact]
    public void Mercenary_LevelsUpTheCharacter()
    {
        var run = Run(Hero() with { Exp = 999, NextExp = 1000 }, Contract(10, Now));

        Assert.Equal(1, run.LevelsGained);
        Assert.Equal((2, 145L), (run.Character.Level, run.Character.Exp));
        Assert.Equal(new BaseStats(14, 12, 13, 11, 13, 10, 10), run.Character.Base);
    }

    // rock 對 Vit 1000 的角色：雙方每下 1 點，沒有寵物時 40 回合超限。
    // 帶寵物（出擊率 10 + 10 + 1 = 21，擲 1 < 21）：怪物命中都由寵物代擋，回合末寵物物理一擊 6000 對 df 1000 擊倒 rock
    [Fact]
    public void Mercenary_CarriedPetJoins()
    {
        var tough = Hero() with { Base = Hero().Base with { Vit = 1000 } };
        var rock = Monster("rock", hp: 60, at: 40, df: 1000);
        var pet = TestPets.Create(attack: 5000, defense: 5000, spirit: 255, intimacy: 255);

        var with = Run(tough, Contract(10, Now), monsters: [rock], pet: pet);
        var without = Run(tough, Contract(10, Now), monsters: [rock]);

        Assert.Equal(MercenaryFightResult.Won, Assert.Single(with.Fights).Result);
        Assert.NotEqual(MercenaryFightResult.Won, Assert.Single(without.Fights).Result);
    }

    [Fact]
    public void Run_MonsterSkillsApply_InMercenaryFight()
    {
        // 怪物回合開頭削掉傭兵全部 HP：傭兵還沒出手就輸了
        var finisher = new SkillDefinition("星芒", "星芒", SkillType.Active, SkillTiming.BeforeAttack, "",
            [new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 100)], [], [new PercentDamageEffect(100, 0)])]);
        var monster = Dummy with { Skills = [new EquippedSkill(finisher, 1, 1)] };

        var run = Run(Hero(), Contract(10, Now), monsters: [monster]);

        Assert.Equal(MercenaryFightResult.Lost, run.Fights.Single().Result);
    }

    [Fact]
    public void Run_WeaponSkillApplies_InMercenaryFight()
    {
        // 武器附帶的技能回合開頭削掉怪物全部 HP：傭兵本來打不贏血量這麼高的怪
        var finisher = new SkillDefinition("星芒", "星芒", SkillType.Active, SkillTiming.BeforeAttack, "",
            [new SkillLevel(1, 0, 0, 0, [new SkillCondition(SkillConditionKind.Chance, 40)], [new SkillCost(SkillCostKind.SkillPoints, 9)], [new PercentDamageEffect(100, 0)])]);
        var skills = SkillLoadout.WithWeaponSkill([], new WeaponSkillSpec("星芒", 1, 100), _ => finisher);
        var tough = Monster("tough", hp: 10_000_000);

        var without = Run(Hero(), Contract(10, Now), monsters: [tough]);
        var with = Run(Hero(), Contract(10, Now), monsters: [tough], skills: skills);

        Assert.NotEqual(MercenaryFightResult.Won, without.Fights.Single().Result);
        Assert.Equal(MercenaryFightResult.Won, with.Fights.Single().Result);
    }
}
