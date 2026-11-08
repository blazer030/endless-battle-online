import type { SkillDto } from "./SkillConverter"

export const skillLevel = (level: number, overrides: Partial<SkillDto["levels"][number]> = {}) =>
  ({
    level,
    learningCost: level * 100,
    proficiencyRequired: level * 10,
    cooldown: 0,
    conditions: [{ kind: "Chance", percent: 40 }],
    costs: [{ kind: "SkillPoints", amount: 2 }],
    effects: [{ kind: "StatChange", target: "Caster", stat: "Attack", percent: level, rounds: 3 }],
    ...overrides,
  }) as SkillDto["levels"][number]

export const skillDto = (overrides: Partial<SkillDto> = {}): SkillDto => ({
  id: "skill-1",
  name: "爆氣",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "提升攻擊",
  maxLevel: 2,
  prerequisite: null,
  jobIds: ["job-6"],
  levels: [skillLevel(1), skillLevel(2)],
  ...overrides,
})

export const skills = [
  skillDto(),
  skillDto({ id: "skill-2", name: "鐵壁", slotType: "Passive", jobIds: ["job-7"] }),
  skillDto({ id: "skill-3", name: "連擊", jobIds: ["job-6", "job-7"] }),
]
