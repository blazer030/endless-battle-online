using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Domain.Items;

public static class ConsumableUse
{
    // 回傳使用後的角色；不能從背包直接使用的道具（補給品、裝備、材料…）回傳 null
    public static Character? Apply(Character character, ItemDefinition item) => item.Consumable switch
    {
        ConsumableKind.StatPotion => character with { Base = item.Bonus.AddTo(character.Base) },
        // 行動力以存檔值計算；尚未入帳的自然回復之後照常加上，總和一樣不超過上限
        ConsumableKind.StaminaPotion => character with { ActNum = Math.Min(StaminaRules.Max, character.ActNum + item.Amount) },
        // 擴充是設定成道具的格數，不累加
        ConsumableKind.BagExpansion => character with { BagBonus = item.Amount },
        ConsumableKind.DepotExpansion => character with { DepotBonus = item.Amount },
        _ => null,
    };
}
