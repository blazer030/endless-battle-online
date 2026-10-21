using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Domain.Items;

public static class ConsumableUse
{
    // 原因代碼：notUsable（補給品、裝備、材料等不能從背包直接使用）、noEffect（擴充格數不比目前多）
    public static IReadOnlyList<string> CanUse(Character character, ItemDefinition item) => item.Consumable switch
    {
        ConsumableKind.StatPotion or ConsumableKind.StaminaPotion => [],
        ConsumableKind.BagExpansion => item.Amount > character.BagBonus ? [] : ["noEffect"],
        ConsumableKind.DepotExpansion => item.Amount > character.DepotBonus ? [] : ["noEffect"],
        _ => ["notUsable"],
    };

    // 呼叫前先以 CanUse 確認可用
    public static Character Apply(Character character, ItemDefinition item) => item.Consumable switch
    {
        ConsumableKind.StatPotion => character with { Base = item.Bonus.AddTo(character.Base) },
        // 行動力以存檔值計算；尚未入帳的自然回復之後照常加上，總和一樣不超過上限
        ConsumableKind.StaminaPotion => character with { ActNum = Math.Min(StaminaRules.Max, character.ActNum + item.Amount) },
        // 擴充是設定成道具的格數，不累加
        ConsumableKind.BagExpansion => character with { BagBonus = item.Amount },
        ConsumableKind.DepotExpansion => character with { DepotBonus = item.Amount },
        _ => throw new InvalidOperationException($"道具 {item.Id} 不能使用"),
    };
}
