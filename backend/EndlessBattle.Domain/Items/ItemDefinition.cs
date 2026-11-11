using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Items;

public sealed record StatBonus(
    int Strength,
    int Intelligence,
    int Life,
    int Vitality,
    int Agility,
    int Charisma,
    int Faith,
    int Attack,
    int MagicAttack,
    int Defense,
    int MagicDefense,
    int MaxHealthPoints = 0)
{
    public static StatBonus Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 只加七項能力
    public BaseStats AddTo(BaseStats stats) => new(
        Strength: stats.Strength + Strength,
        Intelligence: stats.Intelligence + Intelligence,
        Life: stats.Life + Life,
        Vitality: stats.Vitality + Vitality,
        Agility: stats.Agility + Agility,
        Charisma: stats.Charisma + Charisma,
        Faith: stats.Faith + Faith);

    public static StatBonus operator +(StatBonus left, StatBonus right) => new(
        left.Strength + right.Strength,
        left.Intelligence + right.Intelligence,
        left.Life + right.Life,
        left.Vitality + right.Vitality,
        left.Agility + right.Agility,
        left.Charisma + right.Charisma,
        left.Faith + right.Faith,
        left.Attack + right.Attack,
        left.MagicAttack + right.MagicAttack,
        left.Defense + right.Defense,
        left.MagicDefense + right.MagicDefense,
        left.MaxHealthPoints + right.MaxHealthPoints);
}

// 裝備需求只比對這四項裸點
public sealed record EquipmentRequirement(int Strength, int Intelligence, int Agility, int Charisma)
{
    public static EquipmentRequirement None { get; } = new(0, 0, 0, 0);
}

// 精鍊成功時各能力在 [Min, Max] 擲骰累加；null 表示不加該能力
public sealed record RefineStoneSpec(
    int Level,
    int Kind,
    StatRange? Attack,
    StatRange? MagicAttack,
    StatRange? Defense,
    StatRange? MagicDefense,
    StatRange? Strength,
    StatRange? Intelligence,
    StatRange? Life,
    StatRange? Vitality,
    StatRange? Agility,
    StatRange? Charisma,
    StatRange? Faith);

// 固定魔石取 Fixed；隨機魔石鑲嵌當下各能力在 [Min, Max] 擲骰。BoxClass 1～3 是魔石寶箱的等級，0 不會從寶箱出現
public sealed record StoneSpec(bool Random, StatBonus Fixed, StatBonus? Min, StatBonus? Max, int BoxClass = 0);

// 禁區鑰匙：進入 PlaceId 後可停留 Seconds 秒
public sealed record AreaKeySpec(int PlaceId, int Seconds);

// 武器附帶的技能：Rate 是發動率，取代技能的 Chance 條件；SkillName 在內容載入時填入
public sealed record WeaponSkillSpec(string SkillId, int Level, int Rate)
{
    public string SkillName { get; init; } = "";
}

public sealed record ItemDefinition(
    string Id,
    string Name,
    ItemCategory Category,
    ConsumableKind Consumable,
    StatBonus Bonus,
    int SupplyHealthPoints,
    int SupplyManaPoints,
    int Amount, // 消耗品的數值，意義依 ConsumableKind
    EquipmentRequirement Requires,
    string? JobId,
    int Element,
    int Holes,
    int RefineMax,
    long Price,
    int ShopLevel,
    bool ShopSellable,
    bool Bound,
    RefineStoneSpec? Refine,
    StoneSpec? Stone,
    AreaKeySpec? Key = null,
    ElixirSpec? Elixir = null)
{
    public WeaponSkillSpec? WeaponSkill { get; init; }

    public bool IsEquipment => Category is ItemCategory.Weapon or ItemCategory.Head or ItemCategory.Body
        or ItemCategory.Hand or ItemCategory.Foot or ItemCategory.Accessory;

    public bool Stackable => !IsEquipment;
}
