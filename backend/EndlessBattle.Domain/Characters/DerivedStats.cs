using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Domain.Characters;

public sealed record DerivedStats(int Attack, int MagicAttack, int Defense, int MagicDefense, int MaxHealthPoints, int MaxManaPoints)
{
    public static DerivedStats From(BaseStats baseStats) => From(baseStats, EquipmentBonus.Zero);

    // 研究 §2.3：七項取有效值；攻擊、魔攻、防禦、魔防再加裝備本身的數值，HP 上限再加魔石的 HP 上限
    public static DerivedStats From(BaseStats baseStats, EquipmentBonus equipment)
    {
        var total = equipment.Effective(baseStats);
        var bonus = equipment.Total;
        return new DerivedStats(
            Attack: bonus.Attack + total.Strength,
            MagicAttack: bonus.MagicAttack + total.Intelligence,
            Defense: bonus.Defense + total.Vitality,
            MagicDefense: bonus.MagicDefense + total.Faith,
            MaxHealthPoints: RoundLikePhp(total.Life * 25.0) + 50 + bonus.MaxHealthPoints,
            MaxManaPoints: RoundLikePhp((total.Intelligence + total.Faith) * 3.0) + 25);
    }

    // 與 PHP round 相同：.5 遠離零進位
    public static int RoundLikePhp(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
