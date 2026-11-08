using System.Text.Json;
using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Tests.Domain.Battle;

public sealed class BattleEventsTests
{
    [Fact]
    public void AttackEvent_SerializesWithoutSpecialAttackName()
    {
        BattleEvent attack = new AttackEvent(0, CombatSide.Player, AttackKind.Physical, Hit: true, Critical: true,
            Damage: 500, ElementMultiplier: 1.0, TargetHpAfter: 0);

        var json = JsonSerializer.SerializeToElement(attack, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.True(json.GetProperty("critical").GetBoolean());
        Assert.False(json.TryGetProperty("specialAttackName", out _));
    }
}
