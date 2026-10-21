using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

// 六格裝備的本體、魔石與精鍊加成總和；補給欄不計
public sealed record EquipmentBonus(StatBonus Total)
{
    public static EquipmentBonus Zero { get; } = new(StatBonus.Zero);

    // 七項有效值 = 裸點 + 加成
    public BaseStats Effective(BaseStats baseStats) => Total.AddTo(baseStats);
}

public static class EquipmentBonusCalculator
{
    // 每件裝備：本體加成 + 精鍊累加 + 各孔魔石的擲骰值；補給欄的道具不計
    public static EquipmentBonus Sum(Loadout loadout, Func<string, ItemDefinition> items)
    {
        var total = StatBonus.Zero;
        foreach (var (slot, entry) in loadout.Equipped)
        {
            if (slot == EquipmentSlot.Supply)
            {
                continue;
            }

            total += items(entry.ItemId).Bonus + entry.RefineBonus;
            foreach (var socket in entry.Sockets)
            {
                total += socket.Rolled;
            }
        }

        return new EquipmentBonus(total);
    }
}
