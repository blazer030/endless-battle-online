using Inkbound.Domain.Combat;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Domain;

public class BattleSimulatorTests
{
    [Fact]
    public void Simulate_EndsWithDefeat_AndPlayerActsFirstOnTies()
    {
        var player = TestStats.Make(maxHealth: 100, attack: 10);
        var monster = TestStats.Make(maxHealth: 20, attack: 1);
        var result = BattleSimulator.Simulate(player, monster, new ScriptedRandom(0.99), timeLimitSeconds: 60);
        Assert.Equal(new BattleEvent[]
        {
            new AttackEvent(1, CombatSide.Player, 10, false, 10),
            new AttackEvent(1, CombatSide.Monster, 1, false, 99),
            new AttackEvent(2, CombatSide.Player, 10, false, 0),
            new DefeatEvent(2, CombatSide.Monster),
        }, result.Events);
        Assert.Equal(CombatSide.Player, result.Winner);
        Assert.Equal(2, result.DurationSeconds);
    }

    [Fact]
    public void Simulate_FasterSideAttacksMoreOften()
    {
        var player = TestStats.Make(maxHealth: 1000, attack: 1, attacksPerSecond: 2);
        var monster = TestStats.Make(maxHealth: 1000, attack: 1, attacksPerSecond: 1);
        var result = BattleSimulator.Simulate(player, monster, new ScriptedRandom(0.99), timeLimitSeconds: 4);
        var attacks = result.Events.OfType<AttackEvent>().ToList();
        Assert.Equal(8, attacks.Count(attack => attack.Attacker == CombatSide.Player));
        Assert.Equal(4, attacks.Count(attack => attack.Attacker == CombatSide.Monster));
    }

    [Fact]
    public void Simulate_PlayerCanLose()
    {
        var result = BattleSimulator.Simulate(TestStats.Make(maxHealth: 5, attack: 1), TestStats.Make(maxHealth: 1000, attack: 50), new ScriptedRandom(0.99), 60);
        Assert.Equal(CombatSide.Monster, result.Winner);
        Assert.Equal(new DefeatEvent(1, CombatSide.Player), result.Events[^1]);
    }

    [Fact]
    public void Simulate_ReturnsStalemate_WhenTimeLimitReached()
    {
        var tank = TestStats.Make(maxHealth: 1_000_000, attack: 1);
        var result = BattleSimulator.Simulate(tank, tank, new ScriptedRandom(0.99), timeLimitSeconds: 5);
        Assert.Null(result.Winner);
        Assert.Equal(5, result.DurationSeconds);
        Assert.DoesNotContain(result.Events, battleEvent => battleEvent is DefeatEvent);
        Assert.All(result.Events, battleEvent => Assert.True(battleEvent.AtSeconds <= 5));
    }
}
