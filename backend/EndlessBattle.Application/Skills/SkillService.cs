using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Application.Skills;

public sealed class SkillService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    ISkillBookRepository skillBook,
    ISkillLoadoutRepository loadout,
    IGameContent content)
{
    public async Task<SkillOverview> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var points = await proficiencies.ListAsync(character.Id, cancellationToken);
        var learned = await skillBook.ListAsync(character.Id, cancellationToken);
        var levels = learned.ToDictionary(skill => skill.SkillId, skill => skill.Level, StringComparer.Ordinal);

        var currentJobSkills = content.SkillsOf(character.JobId);
        var learnedOthers = content.AllSkills.Where(skill => skill.JobId != character.JobId && levels.ContainsKey(skill.Id));
        var views = currentJobSkills.Concat(learnedOthers)
            .Select(skill => ToView(character, skill, levels.GetValueOrDefault(skill.Id), SkillPointsOf(points, skill.JobId)))
            .ToList();

        var slots = await loadout.ListAsync(character.Id, cancellationToken);
        return new SkillOverview(character.JobId, SkillPointsOf(points, character.JobId), views, slots);
    }

    // 角色更新（扣錢、版本鎖）成功後才扣技能點（條件式，不足則 409）與寫技能書；版本衝突時其他表都不動
    public async Task<SkillOverview> LearnAsync(long accountId, string skillId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var skill = FindSkill(skillId)
            ?? throw new InputValidationException("不存在的技能");
        var skillPoints = SkillPointsOf(await proficiencies.ListAsync(character.Id, cancellationToken), skill.JobId);
        var currentLevel = (await skillBook.ListAsync(character.Id, cancellationToken))
            .FirstOrDefault(learned => learned.SkillId == skill.Id)?.Level ?? 0;

        var check = SkillLearning.Evaluate(character, skill, currentLevel, skillPoints);
        if (!check.Allowed)
        {
            throw new InputValidationException("無法學習技能", check.Reasons);
        }

        var (updated, proficiencyCost, learnedSkill) = SkillLearning.Apply(character, skill, currentLevel, skillPoints);
        await characters.UpdateAsync(updated, cancellationToken);
        await proficiencies.SpendSkillPointsAsync(character.Id, skill.JobId, proficiencyCost, cancellationToken);
        await skillBook.UpsertAsync(character.Id, learnedSkill, cancellationToken);
        return await GetAsync(accountId, cancellationToken);
    }

    // 未列出的欄位視為清空
    public async Task<SkillOverview> SetSlotsAsync(long accountId, IReadOnlyList<SkillSlot> requested, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var learned = await skillBook.ListAsync(character.Id, cancellationToken);

        var reasons = SkillLoadout.Validate(requested, learned, content.GetSkill);
        if (reasons.Count > 0)
        {
            throw new InputValidationException("技能欄位不正確", reasons);
        }

        var slots = requested.OrderBy(slot => slot.Slot).ToList();
        // 角色不變，只為了走版本鎖：與轉職同時進行時其中一方會 409
        await characters.UpdateAsync(character, cancellationToken);
        await loadout.ReplaceAsync(character.Id, slots, cancellationToken);
        return await GetAsync(accountId, cancellationToken);
    }

    private SkillDefinition? FindSkill(string skillId) =>
        content.AllSkills.FirstOrDefault(skill => skill.Id == skillId);

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");

    private static long SkillPointsOf(IReadOnlyList<JobProficiency> points, string jobId) =>
        points.FirstOrDefault(row => row.JobId == jobId)?.SkillPoints ?? 0;

    private static SkillView ToView(Character character, SkillDefinition skill, int level, long skillPoints)
    {
        NextLevelView? next = null;
        if (level < skill.MaxLevel)
        {
            var check = SkillLearning.Evaluate(character, skill, level, skillPoints);
            var target = skill.LevelAt(level + 1);
            next = new NextLevelView(target.Level, target.Sp, target.Rounds, target.Chance, target.Money, target.Proficiency, check.Allowed, check.Reasons);
        }

        // 內容縮減等級上限時，已學等級以上限計
        var chance = level == 0 ? 0 : skill.LevelAt(Math.Min(level, skill.MaxLevel)).Chance;
        return new SkillView(skill.Id, skill.Name, skill.Type, skill.Effect, skill.Description, skill.JobId, level, skill.MaxLevel, chance, next);
    }
}
