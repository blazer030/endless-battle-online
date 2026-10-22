using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

public static class EquippedSkills
{
    // 內容檔移除的技能以 null 交給 Domain 略過；技能只有十幾個，線性查找即可
    public static async Task<IReadOnlyList<EquippedSkill>> LoadAsync(
        ISkillLoadoutRepository loadout, ISkillBookRepository skillBook, IGameContent content, Character character,
        CancellationToken cancellationToken) =>
        SkillLoadout.ForBattle(
            await loadout.ListAsync(character.Id, cancellationToken),
            await skillBook.ListAsync(character.Id, cancellationToken),
            character.JobId,
            skillId => content.AllSkills.FirstOrDefault(skill => skill.Id == skillId));
}
