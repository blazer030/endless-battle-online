using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Skills;

// 欄位 1～3 放主動技能（發動率由玩家設定），4～5 放當前職業的被動技能（發動率用技能自己的值）
public static class SkillLoadout
{
    public const int ActiveSlots = 3, PassiveSlots = 2, DefaultChance = 50, MinChance = 10, MaxChance = 100, ChanceStep = 10;

    public static bool IsActiveSlot(int slot) => slot is >= 1 and <= ActiveSlots;

    public static bool IsPassiveSlot(int slot) => slot > ActiveSlots && slot <= ActiveSlots + PassiveSlots;

    // 回傳所有不合法原因（不重複、依出現順序），空清單表示合法。
    // 代碼：slotOutOfRange、duplicateSlot、notLearned、duplicateSkill、activeInPassiveSlot、passiveInActiveSlot、passiveNotCurrentJob、chanceOutOfRange。
    // 被動欄的 Chance 不檢查：由呼叫端以技能在已學等級的 chance 覆寫
    public static IReadOnlyList<string> Validate(
        IReadOnlyList<SkillSlot> slots, string currentJobId, IReadOnlyList<LearnedSkill> learned, Func<string, SkillDefinition> skills)
    {
        var reasons = new List<string>();
        var seenSlots = new HashSet<int>();
        var seenSkills = new HashSet<string>(StringComparer.Ordinal);
        var learnedIds = learned.Select(skill => skill.SkillId).ToHashSet(StringComparer.Ordinal);

        foreach (var slot in slots)
        {
            var isActiveSlot = IsActiveSlot(slot.Slot);
            if (!isActiveSlot && !IsPassiveSlot(slot.Slot))
            {
                reasons.Add("slotOutOfRange");
            }

            if (!seenSlots.Add(slot.Slot))
            {
                reasons.Add("duplicateSlot");
            }

            if (!learnedIds.Contains(slot.SkillId))
            {
                reasons.Add("notLearned");
                continue;
            }

            if (!seenSkills.Add(slot.SkillId))
            {
                reasons.Add("duplicateSkill");
            }

            var skill = skills(slot.SkillId);
            if (isActiveSlot)
            {
                if (skill.Type == SkillType.Passive)
                {
                    reasons.Add("passiveInActiveSlot");
                }

                if (!IsValidActiveChance(slot.Chance))
                {
                    reasons.Add("chanceOutOfRange");
                }
            }
            else if (IsPassiveSlot(slot.Slot))
            {
                if (skill.Type == SkillType.Active)
                {
                    reasons.Add("activeInPassiveSlot");
                }
                else if (skill.JobId != currentJobId)
                {
                    reasons.Add("passiveNotCurrentJob");
                }
            }
        }

        return reasons.Distinct().ToList();
    }

    // 戰鬥實際生效的欄位，依欄位 1→5：略過技能書沒有、內容已移除（skills 回傳 null）的技能，
    // 以及別職業的被動（轉職與推進競態時可能殘留）；主動欄照 WOG 不限職業
    public static IReadOnlyList<EquippedSkill> ForBattle(
        IReadOnlyList<SkillSlot> slots, IReadOnlyList<LearnedSkill> learned, string currentJobId, Func<string, SkillDefinition?> skills)
    {
        var equipped = new List<EquippedSkill>();
        foreach (var slot in slots.OrderBy(slot => slot.Slot))
        {
            var learnedSkill = learned.FirstOrDefault(skill => skill.SkillId == slot.SkillId);
            if (learnedSkill is null || skills(slot.SkillId) is not { } skill)
            {
                continue;
            }

            if (IsPassiveSlot(slot.Slot) && skill.JobId != currentJobId)
            {
                continue;
            }

            equipped.Add(new EquippedSkill(skill, learnedSkill.Level, ChanceOf(slot, skill, learnedSkill.Level), slot.Slot));
        }

        return equipped;
    }

    // 主動欄用玩家設定的發動率；被動欄用技能在已學等級的 chance，存下的值只是顯示快取
    public static int ChanceOf(SkillSlot slot, SkillDefinition skill, int learnedLevel) =>
        IsPassiveSlot(slot.Slot) ? skill.LevelAt(learnedLevel).Chance : slot.Chance;

    private static bool IsValidActiveChance(int chance) =>
        chance is >= MinChance and <= MaxChance && chance % ChanceStep == 0;
}
