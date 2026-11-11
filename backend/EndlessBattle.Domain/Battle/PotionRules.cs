namespace EndlessBattle.Domain.Battle;

// 補給欄的補給品；Quantity 是剩餘數量。HealthPointsPercent／ManaPointsPercent 是玩家設定的門檻（-1 略過該條件），預設同計畫 9
public sealed record SupplyState(string ItemId, string Name, int HealthPoints, int ManaPoints, int Quantity)
{
    public int HealthPointsPercent { get; init; } = 15;
    public int ManaPointsPercent { get; init; } = 100;
}

// 研究 §5.1；門檻用整數比較：healthPoints × 100 < 門檻 × maxHealthPoints
public static class PotionRules
{
    public const int MaxUsesPerFight = 9;

    public static bool ShouldUse(CombatantState player, SupplyState? supply, int usedThisFight) =>
        supply is { Quantity: > 0 }
        && usedThisFight < MaxUsesPerFight
        && Below(player.HealthPoints, player.MaxHealthPoints, supply.HealthPointsPercent)
        && Below(player.ManaPoints, player.MaxManaPoints, supply.ManaPointsPercent)
        && Recovers(player, supply);

    // 門檻可以都填 -1 略過；補不到任何東西時不喝，免得滿血滿魔也把補給喝光
    private static bool Recovers(CombatantState player, SupplyState supply) =>
        (supply.HealthPoints > 0 && player.HealthPoints < player.MaxHealthPoints) || (supply.ManaPoints > 0 && player.ManaPoints < player.MaxManaPoints);

    private static bool Below(int value, int maximum, int percent) =>
        percent < 0 || (long)value * 100 < (long)percent * maximum;
}
