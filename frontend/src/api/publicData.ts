import { CreationOptionsVo, type CreationOption } from "../game/creationOptions"
import {
  JobVo,
  growthStatKeys,
  type GrowthRange,
  type GrowthStatKey,
  type JobSkill,
  type SkillPrerequisite,
  type StatRequirement,
} from "../game/jobBook"
import { parseArray, parseNullableString, parseNumber, parseObject, parseString } from "./parse"
import type { components } from "./schema"

export type JobDto = components["schemas"]["PublicJobResponse"]
export type PublicSettingsDto = components["schemas"]["PublicSettingsResponse"]

const toOption = (value: unknown, path: string): CreationOption => {
  const dto = parseObject(value, path)
  return { id: parseNumber(dto.id, `${path}.id`), name: parseString(dto.name, `${path}.name`) }
}

const toGrowthRange = (value: unknown, path: string): GrowthRange => {
  const dto = parseObject(value, path)
  return {
    minimum: parseNumber(dto.minimum, `${path}.minimum`),
    maximum: parseNumber(dto.maximum, `${path}.maximum`),
  }
}

// 技能頁（api/skillData.ts）以前置技能、能力需求為基礎再加上 met，成長格式相同
export const toPrerequisite = (value: unknown, path: string): SkillPrerequisite | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    skillId: parseString(dto.skillId, `${path}.skillId`),
    name: parseString(dto.name, `${path}.name`),
    level: parseNumber(dto.level, `${path}.level`),
  }
}

export const toStatRequirement = (value: unknown, path: string): StatRequirement => {
  const dto = parseObject(value, path)
  return {
    stat: parseString(dto.stat, `${path}.stat`),
    minimum: parseNumber(dto.minimum, `${path}.minimum`),
  }
}

export const toStatRequirements = (value: unknown, path: string): StatRequirement[] =>
  parseArray(value, path, toStatRequirement)

export const toGrowth = (value: unknown, path: string): Record<GrowthStatKey, GrowthRange> => {
  const dto = parseObject(value, path)
  return Object.fromEntries(
    growthStatKeys.map((key) => [key, toGrowthRange(dto[key], `${path}.${key}`)]),
  ) as Record<GrowthStatKey, GrowthRange>
}

const toSkill = (value: unknown, path: string): JobSkill => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    description: parseString(dto.description, `${path}.description`),
    maxLevel: parseNumber(dto.maxLevel, `${path}.maxLevel`),
    prerequisite: toPrerequisite(dto.prerequisite, `${path}.prerequisite`),
    levels: parseArray(dto.levels, `${path}.levels`, (item, itemPath) => {
      const level = parseObject(item, itemPath)
      return {
        level: parseNumber(level.level, `${itemPath}.level`),
        summary: parseString(level.summary, `${itemPath}.summary`),
      }
    }),
  }
}

export const JobConverter = {
  toVo(value: unknown, path = "job"): JobVo {
    const dto = parseObject(value, path)
    return JobVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      tier: parseNumber(dto.tier, `${path}.tier`),
      parentId: parseNullableString(dto.parentId, `${path}.parentId`),
      unlockLevel: parseNumber(dto.unlockLevel, `${path}.unlockLevel`),
      proficiencyRequired: parseNumber(dto.proficiencyRequired, `${path}.proficiencyRequired`),
      statRequirements: toStatRequirements(dto.statRequirements, `${path}.statRequirements`),
      growth: toGrowth(dto.growth, `${path}.growth`),
      skills: parseArray(dto.skills, `${path}.skills`, toSkill),
    })
  },
}

export const CreationOptionsConverter = {
  toVo(value: unknown, path = "settings"): CreationOptionsVo {
    const dto = parseObject(value, path)
    return CreationOptionsVo.create({
      sexes: parseArray(dto.sexes, `${path}.sexes`, toOption),
      elements: parseArray(dto.elements, `${path}.elements`, (item, itemPath) => {
        const element = parseObject(item, itemPath)
        return {
          ...toOption(element, itemPath),
          strongAgainst: parseNumber(element.strongAgainst, `${itemPath}.strongAgainst`),
          weakAgainst: parseNumber(element.weakAgainst, `${itemPath}.weakAgainst`),
        }
      }),
      births: parseArray(dto.births, `${path}.births`, toOption),
    })
  },
}
