namespace EndlessBattle.Domain.Battle;

// 補給欄的補給品；Quantity 是剩餘數量。HpPercent／SpPercent 是玩家設定的門檻（-1 略過該條件），預設同計畫 9
public sealed record SupplyState(string ItemId, string Name, int Hp, int Sp, int Quantity)
{
    public int HpPercent { get; init; } = 15;
    public int SpPercent { get; init; } = 100;
}

// 研究 §5.1；門檻用整數比較：hp × 100 < 門檻 × hpMax
public static class PotionRules
{
    public const int MaxUsesPerFight = 9;

    public static bool ShouldUse(CombatantState player, SupplyState? supply, int usedThisFight) =>
        supply is { Quantity: > 0 }
        && usedThisFight < MaxUsesPerFight
        && Below(player.Hp, player.HpMax, supply.HpPercent)
        && Below(player.Sp, player.SpMax, supply.SpPercent)
        && Recovers(player, supply);

    // 門檻可以都填 -1 略過；補不到任何東西時不喝，免得滿血滿魔也把補給喝光
    private static bool Recovers(CombatantState player, SupplyState supply) =>
        (supply.Hp > 0 && player.Hp < player.HpMax) || (supply.Sp > 0 && player.Sp < player.SpMax);

    private static bool Below(int value, int maximum, int percent) =>
        percent < 0 || (long)value * 100 < (long)percent * maximum;
}
