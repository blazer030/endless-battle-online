namespace EndlessBattle.Domain.Battle;

// 補給欄的補給品；Quantity 是剩餘數量
public sealed record SupplyState(string ItemId, string Name, int Hp, int Sp, int Quantity);

// 研究 §5.1：門檻先固定（玩家可調留到計畫 10）
public static class PotionRules
{
    public const double HpThreshold = 0.15;
    public const double SpThreshold = 1.0;
    public const int MaxUsesPerFight = 9;

    public static bool ShouldUse(CombatantState player, SupplyState? supply, int usedThisFight) =>
        supply is { Quantity: > 0 }
        && usedThisFight < MaxUsesPerFight
        && (double)player.Hp < HpThreshold * player.HpMax
        && (double)player.Sp < SpThreshold * player.SpMax;
}
