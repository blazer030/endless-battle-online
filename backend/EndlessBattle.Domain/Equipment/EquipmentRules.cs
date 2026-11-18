using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

public static class EquipmentRules
{
    // 原因代碼：NOT_EQUIPMENT、JOB_MISMATCH、STAT_TOO_LOW:<stat>；能力需求比對裸點，不含裝備加成。欄位由分類決定，不會放錯欄
    public static IReadOnlyList<string> CanEquip(Character character, ItemDefinition item)
    {
        if (!item.IsEquipment)
        {
            return [ErrorCodes.NotEquipment];
        }

        var reasons = new List<string>();
        if (item.JobId is not null && item.JobId != character.JobId)
        {
            reasons.Add(ErrorCodes.JobMismatch);
        }

        var bare = character.Base;
        var required = item.Requires;
        foreach (var (stat, have, need) in new[]
                 {
                     (BaseStat.Strength, bare.Strength, required.Strength),
                     (BaseStat.Intelligence, bare.Intelligence, required.Intelligence),
                     (BaseStat.Agility, bare.Agility, required.Agility),
                     (BaseStat.Charisma, bare.Charisma, required.Charisma),
                 })
        {
            if (have < need)
            {
                reasons.Add(BaseStatCode.StatTooLow(stat));
            }
        }

        return reasons;
    }

    public static EquipmentSlot SlotOf(ItemCategory category) => category switch
    {
        ItemCategory.Weapon => EquipmentSlot.Weapon,
        ItemCategory.Head => EquipmentSlot.Head,
        ItemCategory.Body => EquipmentSlot.Body,
        ItemCategory.Hand => EquipmentSlot.Hand,
        ItemCategory.Foot => EquipmentSlot.Foot,
        ItemCategory.Accessory => EquipmentSlot.Accessory,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "不是裝備分類"),
    };
}
