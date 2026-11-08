using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Characters;

public sealed record DerivedStats(int At, int Mat, int Df, int Mdf, int HpMax, int SpMax)
{
    public static DerivedStats From(BaseStats baseStats) => From(baseStats, EquipmentBonus.Zero);

    // 研究 §2.3：七項取有效值；at／mat／df／mdf 再加裝備本身的數值，hpMax 再加魔石的 hpMax
    public static DerivedStats From(BaseStats baseStats, EquipmentBonus equipment)
    {
        var total = equipment.Effective(baseStats);
        var bonus = equipment.Total;
        return new DerivedStats(
            At: bonus.At + total.Str,
            Mat: bonus.Mat + total.Smart,
            Df: bonus.Df + total.Vit,
            Mdf: bonus.Mdf + total.Be,
            HpMax: RoundLikePhp(total.Life * 25.0) + 50 + bonus.HpMax,
            SpMax: RoundLikePhp((total.Smart + total.Be) * 3.0) + 25);
    }

    // 與 PHP round 相同：.5 遠離零進位
    public static int RoundLikePhp(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
