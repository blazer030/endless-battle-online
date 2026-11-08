import type { components } from "../../api/schema"
import {
  parseArray,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
} from "../shared/parse"
import { contentStats, contentStatValues, type ContentStatKey } from "../shared/stats"
import { JobVo } from "./JobVo"

export type JobDto = components["schemas"]["JobRecord"]

export const JobConverter = {
  toVo(dto: JobDto, path = "job"): JobVo {
    const growth = parseObject(dto.growth, `${path}.growth`)
    return JobVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      tier: parseNumber(dto.tier, `${path}.tier`),
      parentJobId: parseNullableString(dto.parentJobId, `${path}.parentJobId`),
      unlockLevel: parseNumber(dto.unlockLevel, `${path}.unlockLevel`),
      proficiencyRequired: parseNumber(dto.proficiencyRequired, `${path}.proficiencyRequired`),
      statRequirements: parseArray(
        dto.statRequirements,
        `${path}.statRequirements`,
        (item, itemPath) => {
          const requirement = parseObject(item, itemPath)
          return {
            stat: parseOneOf(requirement.stat, `${itemPath}.stat`, contentStatValues),
            minimum: parseNumber(requirement.minimum, `${itemPath}.minimum`),
          }
        },
      ),
      growth: contentStats.map(({ key }) => {
        const statPath = `${path}.growth.${key}`
        const range = parseObject(growth[key], statPath)
        return {
          stat: key,
          minimum: parseNumber(range.minimum, `${statPath}.minimum`),
          maximum: parseNumber(range.maximum, `${statPath}.maximum`),
        }
      }),
    })
  },

  toDto(vo: JobVo): JobDto {
    const rangeOf = (key: ContentStatKey) => {
      const row = vo.growth.find((candidate) => candidate.stat === key)
      return { minimum: row?.minimum ?? 0, maximum: row?.maximum ?? 0 }
    }
    return {
      id: vo.id,
      name: vo.name,
      tier: vo.tier,
      parentJobId: vo.parentJobId,
      unlockLevel: vo.unlockLevel,
      proficiencyRequired: vo.proficiencyRequired,
      statRequirements: vo.statRequirements.map((requirement) => ({ ...requirement })),
      growth: {
        strength: rangeOf("strength"),
        intelligence: rangeOf("intelligence"),
        life: rangeOf("life"),
        vitality: rangeOf("vitality"),
        agility: rangeOf("agility"),
        charisma: rangeOf("charisma"),
        faith: rangeOf("faith"),
      },
    }
  },
}
