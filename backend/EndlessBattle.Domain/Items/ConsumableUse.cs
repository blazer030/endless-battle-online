using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Domain.Items;

// 使用道具時需要的狀態：目前職業（升級成長）、寵物數、全部道具（寶箱的候選）、寵物蛋的物種候選
public sealed record UseContext(
    Character Character, JobDefinition Job, int PetCount, IReadOnlyList<ItemDefinition> AllItems, IReadOnlyList<MonsterDefinition> EggMonsters);

// GrantedItemIds 依抽出順序、各 1 個；Pet 放獸欄；SkillPoints 加在目前職業
public sealed record UseOutcome(Character Character, IReadOnlyList<string> GrantedItemIds, CapturedPet? Pet, long SkillPoints, int LevelsGained);

public static class ConsumableUse
{
    // 原因代碼：notUsable（補給品、裝備、材料等不能從背包直接使用）、noEffect（擴充格數不比目前多）
    public static IReadOnlyList<string> CanUse(UseContext context, ItemDefinition item) => item.Consumable switch
    {
        ConsumableKind.StatPotion or ConsumableKind.StaminaPotion => [],
        ConsumableKind.BagExpansion => item.Amount > context.Character.BagBonus ? [] : ["noEffect"],
        ConsumableKind.DepotExpansion => item.Amount > context.Character.DepotBonus ? [] : ["noEffect"],
        _ => ["notUsable"],
    };

    // 呼叫前先以 CanUse 確認可用
    public static UseOutcome Apply(UseContext context, ItemDefinition item, DateTimeOffset now, IRandomSource random)
    {
        var character = context.Character;
        return item.Consumable switch
        {
            ConsumableKind.StatPotion => Changed(character with { Base = item.Bonus.AddTo(character.Base) }),
            // 行動力以存檔值計算；尚未入帳的自然回復之後照常加上，總和一樣不超過上限
            ConsumableKind.StaminaPotion => Changed(character with { ActNum = Math.Min(StaminaRules.Max, character.ActNum + item.Amount) }),
            // 擴充是設定成道具的格數，不累加
            ConsumableKind.BagExpansion => Changed(character with { BagBonus = item.Amount }),
            ConsumableKind.DepotExpansion => Changed(character with { DepotBonus = item.Amount }),
            _ => throw new InvalidOperationException($"道具 {item.Id} 不能使用"),
        };
    }

    private static UseOutcome Changed(Character character) => new(character, [], null, 0, 0);
}
