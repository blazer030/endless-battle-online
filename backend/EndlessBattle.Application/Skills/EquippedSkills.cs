using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

public static class EquippedSkills
{
    // 內容檔移除的技能以 null 交給 Domain 略過；技能只有十幾個，線性查找即可；裝備中的武器附帶技能排在最後
    public static async Task<IReadOnlyList<EquippedSkill>> LoadAsync(
        ISkillLoadoutRepository loadout, ISkillBookRepository skillBook, IGameContent content, Character character, Loadout equipment,
        CancellationToken cancellationToken)
    {
        SkillDefinition? FindSkill(string skillId) => content.AllSkills.FirstOrDefault(skill => skill.Id == skillId);

        var slots = SkillLoadout.ForBattle(
            await loadout.ListAsync(character.Id, cancellationToken),
            await skillBook.ListAsync(character.Id, cancellationToken),
            FindSkill);
        var weaponSkill = equipment.Equipped.TryGetValue(EquipmentSlot.Weapon, out var weapon) ? content.GetItem(weapon.ItemId).WeaponSkill : null;
        return SkillLoadout.WithWeaponSkill(slots, weaponSkill, FindSkill);
    }
}
