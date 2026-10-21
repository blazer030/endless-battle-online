using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Domain.Equipment;

// 六格裝備的本體、魔石與精鍊加成總和；補給欄不計
public sealed record EquipmentBonus(StatBonus Total)
{
    public static EquipmentBonus Zero { get; } = new(StatBonus.Zero);

    // 七項有效值 = 裸點 + 加成
    public BaseStats Effective(BaseStats baseStats) => new(
        Str: baseStats.Str + Total.Str,
        Smart: baseStats.Smart + Total.Smart,
        Life: baseStats.Life + Total.Life,
        Vit: baseStats.Vit + Total.Vit,
        Agi: baseStats.Agi + Total.Agi,
        Au: baseStats.Au + Total.Au,
        Be: baseStats.Be + Total.Be);
}
