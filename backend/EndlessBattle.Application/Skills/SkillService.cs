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
        var learnedOthers = content.AllSkills.Where(skill => !content.Teaches(character.JobId, skill.Id) && levels.ContainsKey(skill.Id));
        var skillPoints = SkillPointsOf(points, character.JobId);
        var views = currentJobSkills.Concat(learnedOthers)
            .Select(skill => ToView(character, skill, levels.GetValueOrDefault(skill.Id), skillPoints, TeachingOf(character, skill, levels)))
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
        // 學習扣的是目前職業的技能點：關聯表有目前職業＋這個技能才能學
        var skillPoints = SkillPointsOf(await proficiencies.ListAsync(character.Id, cancellationToken), character.JobId);
        var levels = (await skillBook.ListAsync(character.Id, cancellationToken))
            .ToDictionary(learned => learned.SkillId, learned => learned.Level, StringComparer.Ordinal);
        var currentLevel = levels.GetValueOrDefault(skill.Id);
        var teaching = TeachingOf(character, skill, levels);

        var check = SkillLearning.Evaluate(character, skill, currentLevel, skillPoints, teaching);
        if (!check.Allowed)
        {
            throw new InputValidationException("無法學習技能", check.Reasons);
        }

        var (updated, proficiencyCost, learnedSkill) = SkillLearning.Apply(character, skill, currentLevel, skillPoints, teaching);
        await characters.UpdateAsync(updated, cancellationToken);
        await proficiencies.SpendSkillPointsAsync(character.Id, character.JobId, proficiencyCost, cancellationToken);
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

    private SkillTeaching TeachingOf(Character character, SkillDefinition skill, IReadOnlyDictionary<string, int> levels) =>
        new(content.Teaches(character.JobId, skill.Id), skill.Prerequisite is { } prerequisite ? levels.GetValueOrDefault(prerequisite.SkillId) : 0);

    private static SkillView ToView(Character character, SkillDefinition skill, int level, long skillPoints, SkillTeaching teaching)
    {
        NextLevelView? next = null;
        if (level < skill.MaxLevel)
        {
            var check = SkillLearning.Evaluate(character, skill, level, skillPoints, teaching);
            var target = skill.LevelAt(level + 1);
            next = new NextLevelView(target.Level, SpOf(target), RoundsOf(target), ChanceOf(target), target.Cooldown, target.Money, target.Proficiency, check.Allowed, check.Reasons);
        }

        // 內容縮減等級上限時，已學等級以上限計
        var current = level == 0 ? null : skill.LevelAt(Math.Min(level, skill.MaxLevel));
        return new SkillView(
            skill.Id, skill.Name, skill.Type, skill.Timing, skill.Effect, skill.Description, skill.JobId, level, skill.MaxLevel,
            current is null ? 0 : RoundsOf(current), current is null ? 0 : ChanceOf(current), current?.Cooldown ?? 0, TriggerOf(skill), next);
    }

    // 新格式的技能在畫面改用效果文字之前，先換算成舊欄位，回應維持不變
    private static int SpOf(SkillLevel level) => level.Costs is { } costs
        ? costs.Where(cost => cost.Kind == SkillCostKind.SkillPoints).Sum(cost => cost.Amount)
        : level.Sp;

    private static int RoundsOf(SkillLevel level) => level.Effects is { Count: > 0 } effects ? effects.Max(effect => effect.Rounds) : level.Rounds;

    private static int ChanceOf(SkillLevel level) => level.Conditions is { } conditions
        ? conditions.FirstOrDefault(condition => condition.Kind == SkillConditionKind.Chance)?.Percent ?? 0
        : level.Chance;

    private static SkillTrigger? TriggerOf(SkillDefinition skill) => !skill.IsComposed
        ? skill.EffectiveTrigger
        : skill.Levels[0].Conditions!.FirstOrDefault() switch
        {
            { Kind: SkillConditionKind.HitPointsBelow, Percent: var percent } => SkillTrigger.HpBelow(percent),
            { Kind: SkillConditionKind.Chance } => SkillTrigger.Chance,
            _ => null,
        };
}
