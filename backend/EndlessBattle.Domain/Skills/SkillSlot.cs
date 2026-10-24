namespace EndlessBattle.Domain.Skills;

// Slot 為 1..5；發動率由技能自己決定，欄位不存
public sealed record SkillSlot(int Slot, string SkillId);
