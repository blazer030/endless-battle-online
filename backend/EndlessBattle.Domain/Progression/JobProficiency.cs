namespace EndlessBattle.Domain.Progression;

// 對應 WOG 的 ch_<job>（熟練度）與 sk_<job>（技能點）
public sealed record JobProficiency(string JobId, long Proficiency, long SkillPoints);
