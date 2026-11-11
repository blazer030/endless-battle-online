import {
  JobChoiceVo,
  SkillOverviewVo,
  skillTimings,
  skillTypes,
  type Skill,
  type SkillNextLevel,
} from "../game/skillOverview"
import {
  parseArray,
  parseBoolean,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
} from "./parse"
import { toGrowth, toPrerequisite, toStatRequirements } from "./publicData"

const toStrings = (value: unknown, path: string): string[] => parseArray(value, path, parseString)

const toNextLevel = (value: unknown, path: string): SkillNextLevel | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    level: parseNumber(dto.level, `${path}.level`),
    money: parseNumber(dto.money, `${path}.money`),
    proficiency: parseNumber(dto.proficiency, `${path}.proficiency`),
    summary: parseString(dto.summary, `${path}.summary`),
    canLearn: parseBoolean(dto.canLearn, `${path}.canLearn`),
    reasons: toStrings(dto.reasons, `${path}.reasons`),
  }
}

const toSkill = (value: unknown, path: string): Skill => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    slotType: parseOneOf(dto.slotType, skillTypes, `${path}.slotType`),
    timing: parseOneOf(dto.timing, skillTimings, `${path}.timing`),
    description: parseString(dto.description, `${path}.description`),
    icon: parseNullableString(dto.icon, `${path}.icon`),
    level: parseNumber(dto.level, `${path}.level`),
    maxLevel: parseNumber(dto.maxLevel, `${path}.maxLevel`),
    summary: parseNullableString(dto.summary, `${path}.summary`),
    prerequisite: toPrerequisite(dto.prerequisite, `${path}.prerequisite`),
    next: toNextLevel(dto.next, `${path}.next`),
  }
}

export const SkillOverviewConverter = {
  toVo(value: unknown, path = "skills"): SkillOverviewVo {
    const dto = parseObject(value, path)
    return SkillOverviewVo.create({
      jobId: parseString(dto.jobId, `${path}.jobId`),
      skillPoints: parseNumber(dto.skillPoints, `${path}.skillPoints`),
      skills: parseArray(dto.skills, `${path}.skills`, toSkill),
      slots: parseArray(dto.slots, `${path}.slots`, (item, itemPath) => {
        const slot = parseObject(item, itemPath)
        return {
          slot: parseNumber(slot.slot, `${itemPath}.slot`),
          skillId: parseString(slot.skillId, `${itemPath}.skillId`),
        }
      }),
    })
  },
}

export const JobChoiceConverter = {
  toVo(value: unknown, path = "job"): JobChoiceVo {
    const dto = parseObject(value, path)
    return JobChoiceVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      tier: parseNumber(dto.tier, `${path}.tier`),
      parentId: parseNullableString(dto.parentId, `${path}.parentId`),
      parentName: parseNullableString(dto.parentName, `${path}.parentName`),
      unlockLevel: parseNumber(dto.unlockLevel, `${path}.unlockLevel`),
      proficiencyRequired: parseNumber(dto.proficiencyRequired, `${path}.proficiencyRequired`),
      statRequirements: toStatRequirements(dto.statRequirements, `${path}.statRequirements`),
      growth: toGrowth(dto.growth, `${path}.growth`),
      current: parseBoolean(dto.current, `${path}.current`),
      proficiencyPercent: parseNumber(dto.proficiencyPercent, `${path}.proficiencyPercent`),
      eligible: parseBoolean(dto.eligible, `${path}.eligible`),
      reasons: toStrings(dto.reasons, `${path}.reasons`),
    })
  },
}
