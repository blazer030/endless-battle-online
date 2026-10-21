using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain;

// 計畫 4 Task 5 重寫：目前每場只跑 WogBattle，沒有獎勵、行動力與住宿
public class AdventureSimulatorTests
{
    private static CombatantState Player(int hp = 100, int at = 10) =>
        new(hp, HpMax: 100, Sp: 20, SpMax: 30, at, Mat: 0, Df: 50, Mdf: 50, Agi: 10, Au: 10, Element: 0, SpecialAttackName: "烈焰斬");

    private static MonsterDefinition Monster(string id, int hp, int at = 10) =>
        new(id, id, Level: 3, Hp: hp, Sp: 5, At: at, Mat: 0, Df: 50, Mdf: 50, Agi: 10, Au: 10, Element: 0, JobExp: 0, Meet: 0);

    private static void AssertSeconds(double[] expected, IEnumerable<double> actual)
    {
        var actualList = actual.ToList();
        Assert.Equal(expected.Length, actualList.Count);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index], actualList[index], 9);
        }
    }

    // 全部擲骰 0.0：玩家每回合先攻、必中、暴擊，但 at 10 對 df 50 每下只有 1 點；怪物同樣每下 1 點。
    // 怪物 2 血 → 第 2 回合玩家先攻擊倒，一場 2 回合 = 1.6 秒，加場間 2 秒 → 每 3.6 秒一場。
    // 可用 8 秒：0、3.6、7.2 開打；7.2 那場跨過 8 秒照樣打完，結算到 10.8。玩家每場被打 1 下，HP 延續到下一場
    [Fact]
    public void Run_ChainsFights_CarryingPlayerHp()
    {
        var run = AdventureSimulator.Run(Player(), [Monster("sprite", hp: 2)], availableSeconds: 8, new ScriptedRandom(0.0));

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        AssertSeconds([0, 3.6, 7.2], encounters.Select(encounter => encounter.AtSeconds));
        Assert.Equal(new[] { 100, 99, 98 }, encounters.Select(encounter => encounter.PlayerHp));
        AssertSeconds([0.8, 4.4, 8.0], run.Events.OfType<DefeatEvent>().Select(defeat => defeat.AtSeconds));
        Assert.Equal(10.8, run.ConsumedSeconds, 9);
    }

    // 計畫 4 Task 5 加入 WOG 獎勵公式；橋接期間沒有 RewardEvent，獲得一律 0
    [Fact]
    public void Run_GivesNoRewardYet()
    {
        var run = AdventureSimulator.Run(Player(), [Monster("sprite", hp: 2)], availableSeconds: 8, new ScriptedRandom(0.0));

        Assert.Empty(run.Events.OfType<RewardEvent>());
        Assert.Equal(0, run.ExperienceGained);
        Assert.Equal(0, run.SpiritStonesGained);
    }

    // 暫時規則（Task 4／5 改為住宿）：玩家 HP 歸零後下一場以滿血開打。
    // 玩家 1 血先攻打 1 點，怪物在 0.4 秒回擊 1 點 → 第 1 回合倒下；0.8 + 2 = 2.8 秒後的下一場 PlayerHp 為 HpMax 100
    [Fact]
    public void Run_RestoresHpAfterDefeat()
    {
        var run = AdventureSimulator.Run(Player(hp: 1), [Monster("boss", hp: 1000)], availableSeconds: 3, new ScriptedRandom(0.0));

        var encounters = run.Events.OfType<EncounterEvent>().ToList();
        AssertSeconds([0, 2.8], encounters.Select(encounter => encounter.AtSeconds));
        Assert.Equal(new[] { 1, 100 }, encounters.Select(encounter => encounter.PlayerHp));
        Assert.Equal(new DefeatEvent(0.4, CombatSide.Player), run.Events.OfType<DefeatEvent>().First());
    }

    // 雙方 1000 血、每回合各 1 點：打滿 40 回合逾時 = 32 秒，加場間 2 秒 → 34 秒一場
    [Fact]
    public void Run_CountsTimeoutAsFortyRounds()
    {
        var run = AdventureSimulator.Run(Player(hp: 1000), [Monster("rock", hp: 1000)], availableSeconds: 40, new ScriptedRandom(0.0));

        AssertSeconds([0, 34], run.Events.OfType<EncounterEvent>().Select(encounter => encounter.AtSeconds));
        AssertSeconds([32, 66], run.Events.OfType<TimeoutEvent>().Select(timeout => timeout.AtSeconds));
        Assert.Equal(68, run.ConsumedSeconds, 9);
    }

    [Fact]
    public void Run_ConsumesNothing_WhenNoTimeAvailable()
    {
        var run = AdventureSimulator.Run(Player(), [Monster("sprite", hp: 2)], availableSeconds: 0, new ScriptedRandom(0.0));

        Assert.Empty(run.Events);
        Assert.Equal(0, run.ConsumedSeconds);
    }

    // 第一次擲骰 0.7 → NextInt(2) = 1 → 第二隻
    [Fact]
    public void Run_PicksMonsterFromStageList()
    {
        var run = AdventureSimulator.Run(Player(), [Monster("first", hp: 2), Monster("second", hp: 2)], availableSeconds: 1, new ScriptedRandom(0.0, 0.7));

        Assert.Equal("second", run.Events.OfType<EncounterEvent>().First().MonsterId);
    }

    [Fact]
    public void Run_EncounterCarriesHpAndSpOfBothSides()
    {
        var run = AdventureSimulator.Run(Player(hp: 80), [Monster("sprite", hp: 20)], availableSeconds: 1, new ScriptedRandom(0.0));

        Assert.Equal(
            new EncounterEvent(0, "sprite", MonsterHp: 20, MonsterHpMax: 20, PlayerHp: 80, PlayerHpMax: 100, PlayerSp: 20, PlayerSpMax: 30, StaminaDepleted: false),
            run.Events[0]);
    }
}
