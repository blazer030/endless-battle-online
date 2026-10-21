namespace EndlessBattle.Domain.Battle;

// Winner 為 null 表示回合用完仍未分出勝負；Player／Monster 帶著戰鬥結束時的 HP；Supply 是戰後剩下的補給品（沒裝補給時為 null）
public sealed record BattleOutcome(
    IReadOnlyList<BattleEvent> Events,
    CombatSide? Winner,
    int Rounds,
    int DamageDealtToMonster,
    CombatantState Player,
    CombatantState Monster,
    SupplyState? Supply = null);
