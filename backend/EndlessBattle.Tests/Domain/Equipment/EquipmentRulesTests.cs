using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Equipment;

// 戰士 Base (12,12,12,10,12,10,10)
public class EquipmentRulesTests
{
    private static readonly EndlessBattle.Domain.Characters.Character Warrior =
        TestCharacters.Default(1, "測試者", 1, 1, DateTimeOffset.UnixEpoch);

    // 需求只看裸點：力量 12 < 60、敏捷 12 < 13；魅力 10 = 10 合格。CanEquip 不看裝備，穿了 +50 力量也一樣不合格
    [Fact]
    public void Equip_RequirementsUseBaseStats()
    {
        var heavy = TestItems.Equipment("heavy", requires: new EquipmentRequirement(Strength: 60, Intelligence: 0, Agility: 13, Charisma: 10));

        Assert.Equal(["statTooLow:str", "statTooLow:agi"], EquipmentRules.CanEquip(Warrior, heavy));
    }

    [Fact]
    public void Equip_JobMismatch()
    {
        Assert.Equal(["jobMismatch"], EquipmentRules.CanEquip(Warrior, TestItems.Equipment("staff", jobId: "mage")));
    }

    [Fact]
    public void Equip_AllowsAnyJobWhenNull()
    {
        Assert.Empty(EquipmentRules.CanEquip(Warrior, TestItems.Equipment("sword", requires: new EquipmentRequirement(12, 12, 12, 10))));
        Assert.Empty(EquipmentRules.CanEquip(Warrior, TestItems.Equipment("axe", jobId: "warrior")));
    }

    [Fact]
    public void Equip_RejectsNonEquipment()
    {
        Assert.Equal(["notEquipment"], EquipmentRules.CanEquip(Warrior, TestItems.Consumable("supply")));
    }

    [Theory]
    [InlineData(ItemCategory.Weapon, EquipmentSlot.Weapon)]
    [InlineData(ItemCategory.Head, EquipmentSlot.Head)]
    [InlineData(ItemCategory.Body, EquipmentSlot.Body)]
    [InlineData(ItemCategory.Hand, EquipmentSlot.Hand)]
    [InlineData(ItemCategory.Foot, EquipmentSlot.Foot)]
    [InlineData(ItemCategory.Accessory, EquipmentSlot.Accessory)]
    public void SlotOf_MapsEquipmentCategories(ItemCategory category, EquipmentSlot slot)
    {
        Assert.Equal(slot, EquipmentRules.SlotOf(category));
    }
}
