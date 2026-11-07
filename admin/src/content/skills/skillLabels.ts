import { skillBases, type SkillBasis, type SkillSlotType, type SkillTiming } from "./SkillVo"

export const skillSlotLabels: Record<SkillSlotType, string> = { Active: "主動", Passive: "被動" }

export const skillTimingLabels: Record<SkillTiming, string> = {
  BeforeAttack: "出手前",
  AfterHit: "命中後",
  RoundEnd: "回合結束",
}

export const skillBasisLabels: Record<SkillBasis, string> = {
  Strength: "力量",
  Intelligence: "智力",
  Vitality: "體質",
  Agility: "敏捷",
  Charisma: "魅力",
  Faith: "信仰",
  Level: "等級",
  Attack: "攻擊",
  MagicAttack: "魔攻",
  Defense: "防禦",
  SkillPoints: "SP",
}

export const skillBasisChoices = skillBases.map((value) => ({
  value,
  label: skillBasisLabels[value],
}))
