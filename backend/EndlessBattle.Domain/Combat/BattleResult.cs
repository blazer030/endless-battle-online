using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Combat;

// Winner 為 null 表示時間到仍未分出勝負
public sealed record BattleResult(IReadOnlyList<BattleEvent> Events, CombatSide? Winner, double DurationSeconds);
