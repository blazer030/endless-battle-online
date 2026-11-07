import { SkillVo, type SkillLevelVo, type SkillSlotType, type SkillTiming } from "./SkillVo"

export type SkillFormValues = {
  id: string
  name: string
  slotType: SkillSlotType
  timing: SkillTiming
  description: string
  maxLevel: number
  // 空字串代表沒有前置技能
  prerequisiteSkillId: string
  prerequisiteLevel: number | null
  jobIds: string[]
  levels: SkillLevelVo[]
}

const blankLevel: SkillLevelVo = {
  level: 1,
  learningCost: 0,
  proficiencyRequired: 0,
  cooldown: 0,
  conditions: [],
  costs: [],
  effects: [],
}

export const blankSkillValues: SkillFormValues = {
  id: "",
  name: "",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "",
  maxLevel: 1,
  prerequisiteSkillId: "",
  prerequisiteLevel: null,
  jobIds: [],
  levels: [blankLevel],
}

export const skillToValues = (skill: SkillVo): SkillFormValues => ({
  id: skill.id,
  name: skill.name,
  slotType: skill.slotType,
  timing: skill.timing,
  description: skill.description,
  maxLevel: skill.maxLevel,
  prerequisiteSkillId: skill.prerequisite?.skillId ?? "",
  prerequisiteLevel: skill.prerequisite?.level ?? null,
  jobIds: [...skill.jobIds],
  levels: structuredClone([...skill.levels]),
})

// 等級上限改變時在最後增減等級；新增的一級從前一級複製
export function resizeLevels(levels: readonly SkillLevelVo[], maxLevel: number): SkillLevelVo[] {
  const resized = levels.slice(0, maxLevel)
  while (resized.length < maxLevel) {
    const previous = resized.at(-1) ?? blankLevel
    resized.push({ ...structuredClone(previous), level: resized.length + 1 })
  }
  return resized
}

export const skillFromValues = (values: SkillFormValues): SkillVo =>
  SkillVo.create({
    id: values.id.trim(),
    name: values.name,
    slotType: values.slotType,
    timing: values.timing,
    description: values.description,
    maxLevel: values.maxLevel,
    prerequisite:
      values.prerequisiteSkillId === ""
        ? null
        : { skillId: values.prerequisiteSkillId, level: values.prerequisiteLevel ?? 1 },
    jobIds: values.jobIds,
    levels: resizeLevels(values.levels, values.maxLevel).map((level, index) => ({
      ...level,
      level: index + 1,
    })),
  })
