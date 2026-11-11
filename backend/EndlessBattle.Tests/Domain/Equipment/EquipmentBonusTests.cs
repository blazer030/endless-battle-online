using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Equipment;

public class EquipmentBonusTests
{
    private static readonly Dictionary<string, ItemDefinition> Items = new[]
    {
        TestItems.Equipment("sword", bonus: StatBonus.Zero with { Attack = 20 }),
        TestItems.Equipment("helm", ItemCategory.Head, bonus: StatBonus.Zero with { Defense = 2 }),
        TestItems.Equipment("ring", ItemCategory.Accessory, bonus: StatBonus.Zero with { Strength = 2 }),
        // 補給欄的道具不計加成：故意給它能力，證明被排除
        TestItems.Consumable("supply") with { Bonus = StatBonus.Zero with { Strength = 99, Attack = 99 } },
    }.ToDictionary(item => item.Id);

    // 武器本體 at 20 + 精鍊 at 8 = 28；戒指 str +2 → 有效 str 14 → at 28 + 14 = 42；
    // 頭盔鑲嵌的魔石 hpMax +50 → hpMax 12×25+50+50 = 400；df 2 + vit 10 = 12
    [Fact]
    public void Stats_IncludeEquipmentAndStones()
    {
        var loadout = new Loadout(new Dictionary<EquipmentSlot, InventoryEntry>
        {
            [EquipmentSlot.Weapon] = TestItems.Entry(1, "sword") with { RefineLevel = 2, RefineBonus = StatBonus.Zero with { Attack = 8 } },
            [EquipmentSlot.Head] = TestItems.Entry(2, "helm") with
            {
                Holes = 1,
                Sockets = [new SocketedStone(1, "stone", StatBonus.Zero with { MaxHealthPoints = 50 })],
            },
            [EquipmentSlot.Accessory] = TestItems.Entry(3, "ring"),
            [EquipmentSlot.Supply] = TestItems.Entry(4, "supply", 10),
        }, SupplyQuantity: 10);

        var bonus = EquipmentBonusCalculator.Sum(loadout, id => Items[id]);

        Assert.Equal(StatBonus.Zero with { Attack = 28, Strength = 2, Defense = 2, MaxHealthPoints = 50 }, bonus.Total);
        var derived = DerivedStats.From(new BaseStats(Strength: 12, Intelligence: 12, Life: 12, Vitality: 10, Agility: 12, Charisma: 10, Faith: 10), bonus);
        Assert.Equal((42, 12, 400), (derived.Attack, derived.Defense, derived.MaxHealthPoints));
    }

    [Fact]
    public void Sum_EmptyLoadoutIsZero()
    {
        Assert.Equal(EquipmentBonus.Zero, EquipmentBonusCalculator.Sum(new Loadout(new Dictionary<EquipmentSlot, InventoryEntry>(), 0), id => Items[id]));
    }
}
