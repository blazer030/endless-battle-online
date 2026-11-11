using EndlessBattle.Domain.Items;

namespace EndlessBattle.Tests.Support;

// Domain 測試用的道具定義；未指定的欄位都是 0／null
public static class TestItems
{
    public static ItemDefinition Equipment(
        string id,
        ItemCategory category = ItemCategory.Weapon,
        StatBonus? bonus = null,
        EquipmentRequirement? requires = null,
        string? jobId = null,
        int holes = 0) =>
        Item(id, category) with { Bonus = bonus ?? StatBonus.Zero, Requires = requires ?? EquipmentRequirement.None, JobId = jobId, Holes = holes, RefineMax = 4 };

    public static ItemDefinition Consumable(string id, ConsumableKind kind = ConsumableKind.Supply) =>
        Item(id, ItemCategory.Consumable) with { Consumable = kind, SupplyHp = kind == ConsumableKind.Supply ? 50 : 0 };

    public static ItemDefinition Item(string id, ItemCategory category) => new(
        Id: id,
        Name: id,
        Category: category,
        Consumable: ConsumableKind.None,
        Bonus: StatBonus.Zero,
        SupplyHp: 0,
        SupplySp: 0,
        Amount: 0,
        Requires: EquipmentRequirement.None,
        JobId: null,
        Element: 0,
        Holes: 0,
        RefineMax: 0,
        Price: 0,
        ShopLevel: 1,
        ShopSellable: false,
        Bound: false,
        Refine: null,
        Stone: null);

    public static InventoryEntry Entry(long id, string itemId, int quantity = 1) =>
        new(id, itemId, quantity, 0, StatBonus.Zero, [], 0);
}
