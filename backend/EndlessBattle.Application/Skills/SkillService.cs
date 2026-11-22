using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain;
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
        var learnedOthers = content.AllSkills.Where(skill => !content.Teaches(character.JobId, skill.Id) && levels.ContainsKey(skill.Id));
        var skillPoints = SkillPointsOf(points, character.JobId);
        var views = currentJobSkills.Concat(learnedOthers)
            .Select(skill => ToView(character, skill, levels.GetValueOrDefault(skill.Id), skillPoints, TeachingOf(character.JobId, skill, levels)))
            .ToList();

        var slots = await loadout.ListAsync(character.Id, cancellationToken);
        return new SkillOverview(character.JobId, SkillPointsOf(points, character.JobId), views, slots);
    }

    public Task<SkillOverview> LearnAsync(long accountId, string skillId, CancellationToken cancellationToken) =>
        LearnAsync(accountId, skillId, null, cancellationToken);

    // jobId 是用哪個職業的技能點學：不帶或等於目前職業時用目前職業；以前當過的職業（有熟練度紀錄）只能升級已學的技能，
    // 沒當過的職業只回 WRONG_JOB、不判斷其他條件。
    // 角色更新（扣錢、版本鎖）成功後才扣技能點（條件式，不足則 409）與寫技能書；版本衝突時其他表都不動
    public async Task<SkillOverview> LearnAsync(long accountId, string skillId, string? jobId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var skill = FindSkill(skillId)
            ?? throw new InputValidationException("不存在的技能", [ErrorCodes.SkillUnknown]);
        var learningJobId = jobId ?? character.JobId;
        if (content.AllJobs.All(job => job.Id != learningJobId))
        {
            throw new InputValidationException("不存在的職業", [ErrorCodes.JobUnknown]);
        }

        var rows = await proficiencies.ListAsync(character.Id, cancellationToken);
        var throughPreviousJob = learningJobId != character.JobId;
        if (throughPreviousJob && rows.All(row => row.JobId != learningJobId))
        {
            throw new InputValidationException("沒有當過這個職業", [ErrorCodes.WrongJob]);
        }

        var skillPoints = SkillPointsOf(rows, learningJobId);
        var levels = (await skillBook.ListAsync(character.Id, cancellationToken))
            .ToDictionary(learned => learned.SkillId, learned => learned.Level, StringComparer.Ordinal);
        var currentLevel = levels.GetValueOrDefault(skill.Id);
        var teaching = TeachingOf(learningJobId, skill, levels) with { ThroughPreviousJob = throughPreviousJob };

        var check = SkillLearning.Evaluate(character, skill, currentLevel, skillPoints, teaching);
        if (!check.Allowed)
        {
            throw new InputValidationException("無法學習技能", check.Reasons);
        }

        var (updated, proficiencyCost, learnedSkill) = SkillLearning.Apply(character, skill, currentLevel, skillPoints, teaching);
        await characters.UpdateAsync(updated, cancellationToken);
        await proficiencies.SpendSkillPointsAsync(character.Id, learningJobId, proficiencyCost, cancellationToken);
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
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色", ErrorCodes.CharacterNotFound);

    private static long SkillPointsOf(IReadOnlyList<JobProficiency> points, string jobId) =>
        points.FirstOrDefault(row => row.JobId == jobId)?.SkillPoints ?? 0;

    private SkillTeaching TeachingOf(string jobId, SkillDefinition skill, IReadOnlyDictionary<string, int> levels) =>
        new(content.Teaches(jobId, skill.Id), skill.Prerequisite is { } prerequisite ? levels.GetValueOrDefault(prerequisite.SkillId) : 0);

    private SkillView ToView(Character character, SkillDefinition skill, int level, long skillPoints, SkillTeaching teaching)
    {
        var check = SkillLearning.Evaluate(character, skill, level, skillPoints, teaching);
        NextLevelView? next = null;
        if (!check.AtMaxLevel)
        {
            var target = skill.LevelAt(check.TargetLevel);
            next = new NextLevelView(target.Level, target.Money, target.Proficiency, SkillSummary.Of(target), check.Allowed, check.EnoughMoney, check.EnoughProficiency);
        }

        // 內容縮減等級上限時，已學等級以上限計
        var current = level == 0 ? null : skill.LevelAt(Math.Min(level, skill.MaxLevel));
        return new SkillView(
            skill.Id, skill.Name, skill.Type, skill.Timing, skill.Description, skill.Icon, level, skill.MaxLevel,
            current is null ? null : SkillSummary.Of(current),
            check.Teaches,
            skill.Prerequisite is { } prerequisite
                ? new SkillPrerequisiteCheckView(prerequisite.SkillId, content.GetSkill(prerequisite.SkillId).Name, prerequisite.Level, check.PrerequisiteMet)
                : null,
            next);
    }
}
