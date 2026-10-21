using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Equipment;

public class EquipmentBonusTests
{
    private static readonly Dictionary<string, ItemDefinition> Items = new[]
    {
        TestItems.Equipment("sword", bonus: StatBonus.Zero with { At = 20 }),
        TestItems.Equipment("helm", ItemCategory.Head, bonus: StatBonus.Zero with { Df = 2 }),
        TestItems.Equipment("ring", ItemCategory.Accessory, bonus: StatBonus.Zero with { Str = 2 }),
        // 補給欄的道具不計加成：故意給它能力，證明被排除
        TestItems.Consumable("supply") with { Bonus = StatBonus.Zero with { Str = 99, At = 99 } },
    }.ToDictionary(item => item.Id);

    // 武器本體 at 20 + 精鍊 at 8 = 28；戒指 str +2 → 有效 str 14 → at 28 + 14 = 42；
    // 頭盔鑲嵌的魔石 hpMax +50 → hpMax 12×25+50+50 = 400；df 2 + vit 10 = 12
    [Fact]
    public void Stats_IncludeEquipmentAndStones()
    {
        var loadout = new Loadout(new Dictionary<EquipmentSlot, InventoryEntry>
        {
            [EquipmentSlot.Weapon] = TestItems.Entry(1, "sword") with { RefineLevel = 2, RefineBonus = StatBonus.Zero with { At = 8 } },
            [EquipmentSlot.Head] = TestItems.Entry(2, "helm") with
            {
                Holes = 1,
                Sockets = [new SocketedStone(1, "stone", StatBonus.Zero with { HpMax = 50 })],
            },
            [EquipmentSlot.Accessory] = TestItems.Entry(3, "ring"),
            [EquipmentSlot.Supply] = TestItems.Entry(4, "supply", 10),
        }, SupplyQuantity: 10);

        var bonus = EquipmentBonusCalculator.Sum(loadout, id => Items[id]);

        Assert.Equal(StatBonus.Zero with { At = 28, Str = 2, Df = 2, HpMax = 50 }, bonus.Total);
        var derived = DerivedStats.From(new BaseStats(Str: 12, Smart: 12, Life: 12, Vit: 10, Agi: 12, Au: 10, Be: 10), bonus);
        Assert.Equal((42, 12, 400), (derived.At, derived.Df, derived.HpMax));
    }

    [Fact]
    public void Sum_EmptyLoadoutIsZero()
    {
        Assert.Equal(EquipmentBonus.Zero, EquipmentBonusCalculator.Sum(new Loadout(new Dictionary<EquipmentSlot, InventoryEntry>(), 0), id => Items[id]));
    }
}
