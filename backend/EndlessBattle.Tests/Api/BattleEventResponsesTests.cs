using System.Text.Json;
using EndlessBattle.Api.Endpoints;
using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Tests.Api;

public sealed class BattleEventResponsesTests
{
    private static JsonElement Serialize(BattleEvent battleEvent) =>
        JsonSerializer.SerializeToElement(BattleEventResponse.From(battleEvent), JsonSerializerOptions.Web);

    [Fact]
    public void AttackEvent_SerializesWithoutSpecialAttackName()
    {
        var json = Serialize(new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: true,
            Damage: 500, ElementMultiplier: 1.0, TargetHealthPointsAfter: 0));

        Assert.Equal("attack", json.GetProperty("type").GetString());
        Assert.True(json.GetProperty("critical").GetBoolean());
        Assert.False(json.TryGetProperty("specialAttackName", out _));
    }

    [Fact]
    public void SkillEvent_SerializesWithSkillDiscriminator()
    {
        var json = Serialize(new SkillEvent(0, CombatSide.Player, "burst", "爆氣", Level: 1, ManaPointsAfter: 10, Heal: 0));

        Assert.Equal("skill", json.GetProperty("type").GetString());
        Assert.Equal("爆氣", json.GetProperty("skillName").GetString());
    }

    [Theory]
    [InlineData("mission")]
    [InlineData("npc")]
    public void EventDiscriminators_MatchTheDocumentedNames(string expected)
    {
        BattleEvent battleEvent = expected == "mission"
            ? new MissionProgressEvent(0, "mission-1", "任務", "monster-1", 1, 3, false, false, false)
            : new NpcEncounterEvent(0, "monster-1", "旅人", null);

        Assert.Equal(expected, Serialize(battleEvent).GetProperty("type").GetString());
    }
}
