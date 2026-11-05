using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Skills;

// Reasons 代碼：wrongJob、prerequisite、maxLevel、notEnoughProficiency、notEnoughMoney
public sealed record LearnCheck(bool Allowed, IReadOnlyList<string> Reasons, int TargetLevel);

public sealed class SkillLearningException(IReadOnlyList<string> reasons) : Exception("無法學習技能")
{
    public IReadOnlyList<string> Reasons { get; } = reasons;
}

// TaughtByCurrentJob：職業與技能的關聯表有「目前職業＋這個技能」；PrerequisiteLevel：前置技能已學的等級（沒有前置技能時不看）
public sealed record SkillTeaching(bool TaughtByCurrentJob, int PrerequisiteLevel);

// 一次只學下一級（前置＝同技能上一級）；成功時同時扣金錢與技能點 sk_，熟練度 ch_ 不動
public static class SkillLearning
{
    public static LearnCheck Evaluate(Character character, SkillDefinition skill, int currentLevel, long skillPoints, SkillTeaching teaching)
    {
        var reasons = new List<string>();
        if (!teaching.TaughtByCurrentJob)
        {
            reasons.Add("wrongJob");
        }

        if (skill.Prerequisite is { } prerequisite && teaching.PrerequisiteLevel < prerequisite.Level)
        {
            reasons.Add("prerequisite");
        }

        if (currentLevel >= skill.MaxLevel)
        {
            reasons.Add("maxLevel");
            return new LearnCheck(false, reasons, skill.MaxLevel);
        }

        var targetLevel = currentLevel + 1;
        var next = skill.LevelAt(targetLevel);
        if (skillPoints < next.Proficiency)
        {
            reasons.Add("notEnoughProficiency");
        }

        if (character.Money < next.Money)
        {
            reasons.Add("notEnoughMoney");
        }

        return new LearnCheck(reasons.Count == 0, reasons, targetLevel);
    }

    public static (Character Character, long ProficiencyCost, LearnedSkill Learned) Apply(
        Character character, SkillDefinition skill, int currentLevel, long skillPoints, SkillTeaching teaching)
    {
        var check = Evaluate(character, skill, currentLevel, skillPoints, teaching);
        if (!check.Allowed)
        {
            throw new SkillLearningException(check.Reasons);
        }

        var level = skill.LevelAt(check.TargetLevel);
        return (character with { Money = character.Money - level.Money }, level.Proficiency, new LearnedSkill(skill.Id, check.TargetLevel));
    }
}
