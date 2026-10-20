using Inkbound.Domain.Adventure;
using Inkbound.Domain.Combat;
using Inkbound.Domain.Content;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Domain;

public class AdventureSimulatorTests
{
    private static MonsterDefinition Monster(string id, int maxHealth, int attack) =>
        new(id, id, TestStats.Make(maxHealth: maxHealth, attack: attack), ExperienceReward: 5, SpiritStoneReward: 2);

    [Fact]
    public void Run_ChainsFightsWithRestAndRewards()
    {
        var run = AdventureSimulator.Run(TestStats.Make(attack: 10), [Monster("sprite", 20, 1)], availableSeconds: 10, new ScriptedRandom(0.99));
        Assert.Equal(new double[] { 0, 3, 6 }, run.Events.OfType<EncounterEvent>().Select(encounter => encounter.AtSeconds));
        Assert.Equal(new double[] { 2, 5, 8 }, run.Events.OfType<RewardEvent>().Select(reward => reward.AtSeconds));
        Assert.Equal(9, run.ConsumedSeconds);
        Assert.Equal(15, run.ExperienceGained);
        Assert.Equal(6, run.SpiritStonesGained);
    }

    [Fact]
    public void Run_GivesNoReward_WhenPlayerLoses_AndKeepsFighting()
    {
        var run = AdventureSimulator.Run(TestStats.Make(maxHealth: 5, attack: 1), [Monster("boss", 1000, 50)], availableSeconds: 30, new ScriptedRandom(0.99));
        Assert.Empty(run.Events.OfType<RewardEvent>());
        Assert.True(run.Events.OfType<EncounterEvent>().Count() >= 2);
        Assert.Equal(0, run.ExperienceGained);
    }

    [Fact]
    public void Run_CountsStalemateAsConsumedTime()
    {
        var tank = TestStats.Make(maxHealth: 1_000_000, attack: 1);
        var run = AdventureSimulator.Run(tank, [new MonsterDefinition("rock", "rock", tank, 5, 2)], availableSeconds: 130, new ScriptedRandom(0.99));
        Assert.Equal(new double[] { 0, 61 }, run.Events.OfType<EncounterEvent>().Select(encounter => encounter.AtSeconds));
        Assert.Equal(122, run.ConsumedSeconds);
    }

    [Fact]
    public void Run_ConsumesNothing_WhenNoTimeAvailable()
    {
        var run = AdventureSimulator.Run(TestStats.Make(), [Monster("sprite", 20, 1)], availableSeconds: 0, new ScriptedRandom(0.99));
        Assert.Empty(run.Events);
        Assert.Equal(0, run.ConsumedSeconds);
    }

    [Fact]
    public void Run_PicksMonsterFromStageList()
    {
        var run = AdventureSimulator.Run(TestStats.Make(attack: 10), [Monster("first", 20, 1), Monster("second", 20, 1)], availableSeconds: 3, new ScriptedRandom(0.99, 0.7));
        Assert.Equal("second", run.Events.OfType<EncounterEvent>().First().MonsterId);
    }
}
