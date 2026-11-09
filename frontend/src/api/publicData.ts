import { CreationOptionsVo, type CreationOption } from "../game/creationOptions"
import {
  JobVo,
  growthStatKeys,
  type GrowthRange,
  type GrowthStatKey,
  type JobSkill,
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

const toSkill = (value: unknown, path: string): JobSkill => {
  const dto = parseObject(value, path)
  const prerequisitePath = `${path}.prerequisite`
  const prerequisite =
    dto.prerequisite === null ? null : parseObject(dto.prerequisite, prerequisitePath)
  return {
    id: parseString(dto.id, `${path}.id`),
    name: parseString(dto.name, `${path}.name`),
    description: parseString(dto.description, `${path}.description`),
    maxLevel: parseNumber(dto.maxLevel, `${path}.maxLevel`),
    prerequisite:
      prerequisite === null
        ? null
        : {
            skillId: parseString(prerequisite.skillId, `${prerequisitePath}.skillId`),
            name: parseString(prerequisite.name, `${prerequisitePath}.name`),
            level: parseNumber(prerequisite.level, `${prerequisitePath}.level`),
          },
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
    const growth = parseObject(dto.growth, `${path}.growth`)
    return JobVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      tier: parseNumber(dto.tier, `${path}.tier`),
      parentId: parseNullableString(dto.parentId, `${path}.parentId`),
      unlockLevel: parseNumber(dto.unlockLevel, `${path}.unlockLevel`),
      proficiencyRequired: parseNumber(dto.proficiencyRequired, `${path}.proficiencyRequired`),
      statRequirements: parseArray(
        dto.statRequirements,
        `${path}.statRequirements`,
        (item, itemPath) => {
          const requirement = parseObject(item, itemPath)
          return {
            stat: parseString(requirement.stat, `${itemPath}.stat`),
            minimum: parseNumber(requirement.minimum, `${itemPath}.minimum`),
          }
        },
      ),
      growth: Object.fromEntries(
        growthStatKeys.map((key) => [key, toGrowthRange(growth[key], `${path}.growth.${key}`)]),
      ) as Record<GrowthStatKey, GrowthRange>,
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
