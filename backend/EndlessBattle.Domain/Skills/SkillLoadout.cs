using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Skills;

// 欄位 1～3 放主動技能（發動率由技能等級固定），4～5 放被動技能（常駐）；兩種欄位都不限職業，學過就能裝
public static class SkillLoadout
{
    public const int ActiveSlots = 3, PassiveSlots = 2;

    public static bool IsActiveSlot(int slot) => slot is >= 1 and <= ActiveSlots;

    public static bool IsPassiveSlot(int slot) => slot > ActiveSlots && slot <= ActiveSlots + PassiveSlots;

    // 回傳所有不合法原因（不重複、依出現順序），空清單表示合法。
    // 代碼：slotOutOfRange、duplicateSlot、notLearned、duplicateSkill、activeInPassiveSlot、passiveInActiveSlot
    public static IReadOnlyList<string> Validate(IReadOnlyList<SkillSlot> slots, IReadOnlyList<LearnedSkill> learned, Func<string, SkillDefinition> skills)
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
            if (isActiveSlot && skill.Type == SkillType.Passive)
            {
                reasons.Add("passiveInActiveSlot");
            }
            else if (IsPassiveSlot(slot.Slot) && skill.Type == SkillType.Active)
            {
                reasons.Add("activeInPassiveSlot");
            }
        }

        return reasons.Distinct().ToList();
    }

    // 戰鬥實際生效的欄位，依欄位 1→5：略過技能書沒有、內容已移除（skills 回傳 null）的技能。
    // 內容縮減等級上限時，已學等級以上限計
    public static IReadOnlyList<EquippedSkill> ForBattle(IReadOnlyList<SkillSlot> slots, IReadOnlyList<LearnedSkill> learned, Func<string, SkillDefinition?> skills)
    {
        var equipped = new List<EquippedSkill>();
        foreach (var slot in slots.OrderBy(slot => slot.Slot))
        {
            var learnedSkill = learned.FirstOrDefault(skill => skill.SkillId == slot.SkillId);
            if (learnedSkill is null || skills(slot.SkillId) is not { } skill)
            {
                continue;
            }

            equipped.Add(new EquippedSkill(skill, Math.Min(learnedSkill.Level, skill.MaxLevel), slot.Slot));
        }

        return equipped;
    }
}
