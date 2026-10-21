using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Skills;

// Reasons 代碼：wrongJob、maxLevel、notEnoughProficiency、notEnoughMoney
public sealed record LearnCheck(bool Allowed, IReadOnlyList<string> Reasons, int TargetLevel);

public sealed class SkillLearningException(IReadOnlyList<string> reasons) : Exception("無法學習技能")
{
    public IReadOnlyList<string> Reasons { get; } = reasons;
}

// 一次只學下一級（前置＝同技能上一級）；成功時同時扣金錢與技能點 sk_，熟練度 ch_ 不動
public static class SkillLearning
{
    public static LearnCheck Evaluate(Character character, SkillDefinition skill, int currentLevel, long skillPoints)
    {
        var reasons = new List<string>();
        if (skill.JobId != character.JobId)
        {
            reasons.Add("wrongJob");
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
        Character character, SkillDefinition skill, int currentLevel, long skillPoints)
    {
        var check = Evaluate(character, skill, currentLevel, skillPoints);
        if (!check.Allowed)
        {
            throw new SkillLearningException(check.Reasons);
        }

        var level = skill.LevelAt(check.TargetLevel);
        return (character with { Money = character.Money - level.Money }, level.Proficiency, new LearnedSkill(skill.Id, check.TargetLevel));
    }
}
