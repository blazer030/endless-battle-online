using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Items;

public sealed record StatBonus(
    int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax = 0)
{
    public static StatBonus Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

// 裝備需求只比對這四項裸點
public sealed record StatRequirement7(int Str, int Smart, int Agi, int Au)
{
    public static StatRequirement7 None { get; } = new(0, 0, 0, 0);
}

// 精鍊成功時各能力在 [Min, Max] 擲骰累加；null 表示不加該能力
public sealed record RefineStoneSpec(
    int Level,
    int Kind,
    StatRange? At,
    StatRange? Mat,
    StatRange? Df,
    StatRange? Mdf,
    StatRange? Str,
    StatRange? Smart,
    StatRange? Life,
    StatRange? Vit,
    StatRange? Agi,
    StatRange? Au,
    StatRange? Be);

// 固定魔石取 Fixed；隨機魔石鑲嵌當下各能力在 [Min, Max] 擲骰
public sealed record StoneSpec(bool Random, StatBonus Fixed, StatBonus? Min, StatBonus? Max);

public sealed record ItemDefinition(
    string Id,
    string Name,
    ItemCategory Category,
    ConsumableKind Consumable,
    StatBonus Bonus,
    int SupplyHp,
    int SupplySp,
    int Amount, // 行動力藥水的加值、擴充的格數
    StatRequirement7 Requires,
    string? JobId,
    int Element,
    int Holes,
    int RefineMax,
    long Price,
    int ShopLevel,
    bool ShopSellable,
    bool Bound,
    RefineStoneSpec? Refine,
    StoneSpec? Stone)
{
    public bool IsEquipment => Category is ItemCategory.Weapon or ItemCategory.Head or ItemCategory.Body
        or ItemCategory.Hand or ItemCategory.Foot or ItemCategory.Accessory;

    public bool Stackable => !IsEquipment;
}
