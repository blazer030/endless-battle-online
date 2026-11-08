import type { components } from "../../api/schema"
import { contentElements } from "../shared/elements"
import {
  ContentParseError,
  parseArray,
  parseNumber,
  parseObject,
  parseOneOf,
  parseOptionalNumber,
  parseString,
} from "../shared/parse"
import {
  skillAttackKinds,
  skillBases,
  skillResources,
  skillSlotTypes,
  skillStatChoices,
  skillTargets,
  skillTimings,
  SkillVo,
  type SkillConditionVo,
  type SkillCostVo,
  type SkillEffectVo,
  type SkillLevelVo,
  type SkillScalingVo,
} from "./SkillVo"

export type SkillDto = components["schemas"]["SkillRecord"]

type Part = Record<string, unknown>

const parseBasis = (value: unknown, path: string) => parseOneOf(value, path, skillBases)

const parseScalingRow = (item: unknown, path: string): SkillScalingVo => {
  const row = parseObject(item, path)
  return {
    basis: parseBasis(row.basis, `${path}.basis`),
    ratio: parseNumber(row.ratio, `${path}.ratio`),
  }
}

const parseScaling = (part: Part, path: string) =>
  parseArray(part.scaling, `${path}.scaling`, parseScalingRow)

const parseOptionalScaling = (part: Part, path: string) =>
  part.scaling === undefined || part.scaling === null ? undefined : parseScaling(part, path)

function parseCondition(item: unknown, path: string): SkillConditionVo {
  const part = parseObject(item, path)
  const kind = part.kind
  switch (kind) {
    case "Chance":
    case "HitPointsBelow":
      return { kind, percent: parseNumber(part.percent, `${path}.percent`) }
    case "HitPointsBelowOpponent":
      return { kind }
    case "StatAbove":
      return { kind, stat: parseBasis(part.stat, `${path}.stat`) }
    default:
      throw new ContentParseError(`${path}.kind`, "認識的條件種類")
  }
}

function parseCost(item: unknown, path: string): SkillCostVo {
  const part = parseObject(item, path)
  const kind = part.kind
  const amount = parseOptionalNumber(part.amount, `${path}.amount`)
  const percent = parseOptionalNumber(part.percent, `${path}.percent`)
  switch (kind) {
    case "HitPoints":
      return { kind, amount, percent }
    case "SkillPoints":
      return { kind, amount, percent, all: part.all === true ? true : undefined }
    case "Money":
      return { kind, amount: parseNumber(part.amount, `${path}.amount`) }
    default:
      throw new ContentParseError(`${path}.kind`, "認識的消耗種類")
  }
}

function parseEffect(item: unknown, path: string): SkillEffectVo {
  const part = parseObject(item, path)
  const kind = part.kind
  const rounds = (): number => parseNumber(part.rounds, `${path}.rounds`)
  const number = (name: string) => parseNumber(part[name], `${path}.${name}`)
  const optional = (name: string) => parseOptionalNumber(part[name], `${path}.${name}`)
  const attackKind = () => parseOneOf(part.attackKind, `${path}.attackKind`, skillAttackKinds)
  switch (kind) {
    case "StatChange":
      return {
        kind,
        target: parseOneOf(part.target, `${path}.target`, skillTargets),
        stat: parseOneOf(part.stat, `${path}.stat`, skillStatChoices),
        percent: optional("percent"),
        amount: optional("amount"),
        scaling: parseOptionalScaling(part, path),
        chance: optional("chance"),
        rounds: rounds(),
      }
    case "BonusDamage":
      return {
        kind,
        attackKind: attackKind(),
        scaling: parseScaling(part, path),
        chance: optional("chance"),
        rounds: rounds(),
      }
    case "MultiHit":
      return { kind, attackKind: attackKind(), hits: number("hits"), rounds: rounds() }
    case "DamageReduction":
      return { kind, attackKind: attackKind(), percent: number("percent"), rounds: rounds() }
    case "Restore":
      return {
        kind,
        resource: parseOneOf(part.resource, `${path}.resource`, skillResources),
        scaling: parseOptionalScaling(part, path),
        percentOfDamage: optional("percentOfDamage"),
        percentOfMaximum: optional("percentOfMaximum"),
        rounds: rounds(),
      }
    case "Stun": {
      const byStatPath = `${path}.chanceByStat`
      const byStat =
        part.chanceByStat === undefined || part.chanceByStat === null
          ? undefined
          : parseObject(part.chanceByStat, byStatPath)
      return {
        kind,
        chance: number("chance"),
        chanceByStat: byStat && {
          stat: parseBasis(byStat.stat, `${byStatPath}.stat`),
          percent: parseNumber(byStat.percent, `${byStatPath}.percent`),
        },
        rounds: rounds(),
      }
    }
    case "Silence":
      return { kind, chance: number("chance"), rounds: rounds() }
    case "PercentDamage":
    case "ElementAdvantage":
      return { kind, percent: number("percent"), rounds: rounds() }
    case "DamageOverTime":
      return { kind, scaling: parseScaling(part, path), rounds: rounds() }
    case "ReduceSkillPoints":
      return {
        kind,
        amount: optional("amount"),
        percentOfMaximum: optional("percentOfMaximum"),
        rounds: rounds(),
      }
    case "AttackElement":
    case "DefenseElement":
      return {
        kind,
        element: parseOneOf(part.element, `${path}.element`, contentElements),
        rounds: rounds(),
      }
    default:
      throw new ContentParseError(`${path}.kind`, "認識的效果種類")
  }
}

function parseLevel(item: unknown, path: string): SkillLevelVo {
  const level = parseObject(item, path)
  return {
    level: parseNumber(level.level, `${path}.level`),
    learningCost: parseNumber(level.learningCost, `${path}.learningCost`),
    proficiencyRequired: parseNumber(level.proficiencyRequired, `${path}.proficiencyRequired`),
    cooldown: parseNumber(level.cooldown, `${path}.cooldown`),
    conditions: parseArray(level.conditions, `${path}.conditions`, parseCondition),
    costs: parseArray(level.costs, `${path}.costs`, parseCost),
    effects: parseArray(level.effects, `${path}.effects`, parseEffect),
  }
}

export const SkillConverter = {
  toVo(dto: SkillDto, path = "skill"): SkillVo {
    const prerequisitePath = `${path}.prerequisite`
    const prerequisite =
      dto.prerequisite === null ? null : parseObject(dto.prerequisite, prerequisitePath)
    return SkillVo.create({
      id: parseString(dto.id, `${path}.id`),
      name: parseString(dto.name, `${path}.name`),
      slotType: parseOneOf(dto.slotType, `${path}.slotType`, skillSlotTypes),
      timing: parseOneOf(dto.timing, `${path}.timing`, skillTimings),
      description: parseString(dto.description, `${path}.description`),
      maxLevel: parseNumber(dto.maxLevel, `${path}.maxLevel`),
      prerequisite: prerequisite && {
        skillId: parseString(prerequisite.skillId, `${prerequisitePath}.skillId`),
        level: parseNumber(prerequisite.level, `${prerequisitePath}.level`),
      },
      jobIds: parseArray(dto.jobIds, `${path}.jobIds`, parseString),
      levels: parseArray(dto.levels, `${path}.levels`, parseLevel),
    })
  },

  // VO 的條件、消耗、效果只有該種類的欄位，直接就是 API 的形狀
  toDto(vo: SkillVo): SkillDto {
    return structuredClone({
      id: vo.id,
      name: vo.name,
      slotType: vo.slotType,
      timing: vo.timing,
      description: vo.description,
      maxLevel: vo.maxLevel,
      prerequisite: vo.prerequisite,
      jobIds: vo.jobIds,
      levels: vo.levels,
    }) as SkillDto
  },
}
