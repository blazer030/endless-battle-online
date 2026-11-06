using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Items;

public sealed record StatBonus(
    int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax = 0)
{
    public static StatBonus Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // 只加七項能力
    public BaseStats AddTo(BaseStats stats) => new(
        Str: stats.Str + Str,
        Smart: stats.Smart + Smart,
        Life: stats.Life + Life,
        Vit: stats.Vit + Vit,
        Agi: stats.Agi + Agi,
        Au: stats.Au + Au,
        Be: stats.Be + Be);

    public static StatBonus operator +(StatBonus left, StatBonus right) => new(
        left.Str + right.Str,
        left.Smart + right.Smart,
        left.Life + right.Life,
        left.Vit + right.Vit,
        left.Agi + right.Agi,
        left.Au + right.Au,
        left.Be + right.Be,
        left.At + right.At,
        left.Mat + right.Mat,
        left.Df + right.Df,
        left.Mdf + right.Mdf,
        left.HpMax + right.HpMax);
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

// 固定魔石取 Fixed；隨機魔石鑲嵌當下各能力在 [Min, Max] 擲骰。BoxClass 1～3 是魔石寶箱的等級，0 不會從寶箱出現
public sealed record StoneSpec(bool Random, StatBonus Fixed, StatBonus? Min, StatBonus? Max, int BoxClass = 0);

// 禁區鑰匙：進入 PlaceId 後可停留 Seconds 秒
public sealed record AreaKeySpec(int PlaceId, int Seconds);

// 武器附帶的技能：Rate 是發動率，取代技能的 Chance 條件
public sealed record WeaponSkillSpec(string SkillId, int Level, int Rate);

public sealed record ItemDefinition(
    string Id,
    string Name,
    ItemCategory Category,
    ConsumableKind Consumable,
    StatBonus Bonus,
    int SupplyHp,
    int SupplySp,
    int Amount, // 消耗品的數值，意義依 ConsumableKind
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
    StoneSpec? Stone,
    AreaKeySpec? Key = null,
    ElixirSpec? Elixir = null)
{
    public WeaponSkillSpec? WeaponSkill { get; init; }

    public bool IsEquipment => Category is ItemCategory.Weapon or ItemCategory.Head or ItemCategory.Body
        or ItemCategory.Hand or ItemCategory.Foot or ItemCategory.Accessory;

    public bool Stackable => !IsEquipment;
}
