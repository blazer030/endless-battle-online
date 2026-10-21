namespace EndlessBattle.Domain.Skills;

// Slot 為 1..5；被動欄的 Chance 存技能自己的值
public sealed record SkillSlot(int Slot, string SkillId, int Chance);
