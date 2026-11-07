import type { SkillSlotType, SkillTiming } from "./SkillVo"

export const skillSlotLabels: Record<SkillSlotType, string> = { Active: "主動", Passive: "被動" }

export const skillTimingLabels: Record<SkillTiming, string> = {
  BeforeAttack: "出手前",
  AfterHit: "命中後",
  RoundEnd: "回合結束",
}
