using System.Text.Json.Serialization;

namespace Inkbound.Domain.Combat;

// 前端用同一串事件同時驅動對戰舞台與戰報；AtSeconds 是相對於這批事件起點的秒數
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EncounterEvent), "encounter")]
[JsonDerivedType(typeof(AttackEvent), "attack")]
[JsonDerivedType(typeof(DefeatEvent), "defeat")]
[JsonDerivedType(typeof(RewardEvent), "reward")]
public abstract record BattleEvent(double AtSeconds);

public sealed record EncounterEvent(double AtSeconds, string MonsterId) : BattleEvent(AtSeconds);

public sealed record AttackEvent(double AtSeconds, CombatSide Attacker, int Damage, bool IsCritical, int TargetHealthAfter)
    : BattleEvent(AtSeconds);

public sealed record DefeatEvent(double AtSeconds, CombatSide Loser) : BattleEvent(AtSeconds);

public sealed record RewardEvent(double AtSeconds, long Experience, long SpiritStones) : BattleEvent(AtSeconds);
