using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Skills;

// 每個條件各自報告有沒有達成（技能總覽用）；滿級時不看熟練度與金錢（兩者為 true）。
// 不能學時的錯誤代碼依序 WRONG_JOB、PREREQUISITE、MAX_LEVEL、NOT_ENOUGH_PROFICIENCY、NOT_ENOUGH_MONEY
public sealed record LearnCheck(bool Teaches, bool PrerequisiteMet, bool AtMaxLevel, bool EnoughProficiency, bool EnoughMoney, int TargetLevel)
{
    public bool Allowed => Teaches && PrerequisiteMet && !AtMaxLevel && EnoughProficiency && EnoughMoney;

    public IReadOnlyList<string> Reasons =>
    [
        .. Teaches ? [] : new[] { ErrorCodes.WrongJob },
        .. PrerequisiteMet ? [] : new[] { ErrorCodes.Prerequisite },
        .. AtMaxLevel ? [ErrorCodes.MaxLevel] : Array.Empty<string>(),
        .. EnoughProficiency ? [] : new[] { ErrorCodes.NotEnoughProficiency },
        .. EnoughMoney ? [] : new[] { ErrorCodes.NotEnoughMoney },
    ];
}

public sealed class SkillLearningException(IReadOnlyList<string> reasons) : Exception("無法學習技能")
{
    public IReadOnlyList<string> Reasons { get; } = reasons;
}

// Teaches：職業與技能的關聯表有「用來學的職業＋這個技能」；PrerequisiteLevel：前置技能已學的等級（沒有前置技能時不看）
public sealed record SkillTeaching(bool Teaches, int PrerequisiteLevel);

// 一次只學下一級（前置＝同技能上一級）；成功時同時扣金錢與技能點 sk_，熟練度 ch_ 不動
public static class SkillLearning
{
    public static LearnCheck Evaluate(Character character, SkillDefinition skill, int currentLevel, long skillPoints, SkillTeaching teaching)
    {
        var prerequisiteMet = skill.Prerequisite is not { } prerequisite || teaching.PrerequisiteLevel >= prerequisite.Level;
        if (currentLevel >= skill.MaxLevel)
        {
            return new LearnCheck(teaching.Teaches, prerequisiteMet, AtMaxLevel: true, EnoughProficiency: true, EnoughMoney: true, skill.MaxLevel);
        }

        var targetLevel = currentLevel + 1;
        var next = skill.LevelAt(targetLevel);
        return new LearnCheck(
            teaching.Teaches, prerequisiteMet, AtMaxLevel: false, skillPoints >= next.Proficiency, character.Money >= next.Money, targetLevel);
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
