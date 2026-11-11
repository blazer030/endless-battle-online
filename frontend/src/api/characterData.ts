import {
  attackKinds,
  CharacterVo,
  type AdventureSettings,
  type BaseStats,
  type CharacterStats,
  type CreateCharacterCommand,
} from "../game/character"
import {
  parseArray,
  parseNullableNumber,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
} from "./parse"
import type { components } from "./schema"

type Schemas = components["schemas"]
export type CharacterDto = Schemas["CharacterResponse"]
export type CreateCharacterDto = Schemas["CreateCharacterRequest"]
export type AdventureSettingsDto = Schemas["AdventureSettingsRequest"]

export const toBaseStats = (value: unknown, path: string): BaseStats => {
  const dto = parseObject(value, path)
  return {
    strength: parseNumber(dto.strength, `${path}.strength`),
    intelligence: parseNumber(dto.intelligence, `${path}.intelligence`),
    life: parseNumber(dto.life, `${path}.life`),
    vitality: parseNumber(dto.vitality, `${path}.vitality`),
    agility: parseNumber(dto.agility, `${path}.agility`),
    charisma: parseNumber(dto.charisma, `${path}.charisma`),
    faith: parseNumber(dto.faith, `${path}.faith`),
  }
}

const toStats = (value: unknown, path: string): CharacterStats => {
  const dto = parseObject(value, path)
  return {
    ...toBaseStats(dto, path),
    attack: parseNumber(dto.attack, `${path}.attack`),
    magicAttack: parseNumber(dto.magicAttack, `${path}.magicAttack`),
    defense: parseNumber(dto.defense, `${path}.defense`),
    magicDefense: parseNumber(dto.magicDefense, `${path}.magicDefense`),
    maxHealthPoints: parseNumber(dto.maxHealthPoints, `${path}.maxHealthPoints`),
    maxManaPoints: parseNumber(dto.maxManaPoints, `${path}.maxManaPoints`),
  }
}

const toSettings = (value: unknown, path: string): AdventureSettings => {
  const dto = parseObject(value, path)
  const percent = (key: string) => parseNumber(dto[key], `${path}.${key}`)
  return {
    attack: dto.attack === null ? null : parseOneOf(dto.attack, attackKinds, `${path}.attack`),
    potionHealthPointsPercent: percent("potionHealthPointsPercent"),
    potionManaPointsPercent: percent("potionManaPointsPercent"),
    restHealthPointsPercent: percent("restHealthPointsPercent"),
    restManaPointsPercent: percent("restManaPointsPercent"),
    escapeHealthPointsPercent: percent("escapeHealthPointsPercent"),
  }
}

export const CharacterConverter = {
  toVo(value: unknown, path = "character"): CharacterVo {
    const dto = parseObject(value, path)
    return CharacterVo.create({
      name: parseString(dto.name, `${path}.name`),
      level: parseNumber(dto.level, `${path}.level`),
      experience: parseNumber(dto.experience, `${path}.experience`),
      nextLevelExperience: parseNumber(dto.nextLevelExperience, `${path}.nextLevelExperience`),
      money: parseNumber(dto.money, `${path}.money`),
      bank: parseNumber(dto.bank, `${path}.bank`),
      healthPoints: parseNumber(dto.healthPoints, `${path}.healthPoints`),
      manaPoints: parseNumber(dto.manaPoints, `${path}.manaPoints`),
      sex: parseNumber(dto.sex, `${path}.sex`),
      element: parseNumber(dto.element, `${path}.element`),
      birth: parseNumber(dto.birth, `${path}.birth`),
      jobId: parseString(dto.jobId, `${path}.jobId`),
      jobName: parseString(dto.jobName, `${path}.jobName`),
      stats: toStats(dto.stats, `${path}.stats`),
      baseStats: toBaseStats(dto.baseStats, `${path}.baseStats`),
      placeId: parseNumber(dto.placeId, `${path}.placeId`),
      placeName: parseString(dto.placeName, `${path}.placeName`),
      subPlace: parseNullableNumber(dto.subPlace, `${path}.subPlace`),
      subPlaceName: parseNullableString(dto.subPlaceName, `${path}.subPlaceName`),
      areaName: parseString(dto.areaName, `${path}.areaName`),
      jobProficiency: parseArray(dto.jobProficiency, `${path}.jobProficiency`, (item, itemPath) => {
        const row = parseObject(item, itemPath)
        return {
          jobId: parseString(row.jobId, `${itemPath}.jobId`),
          proficiency: parseNumber(row.proficiency, `${itemPath}.proficiency`),
          skillPoints: parseNumber(row.skillPoints, `${itemPath}.skillPoints`),
        }
      }),
      settings: toSettings(dto.settings, `${path}.settings`),
    })
  },
}

export const CreateCharacterConverter = {
  toDto(command: CreateCharacterCommand): CreateCharacterDto {
    return {
      name: command.name,
      sex: command.sex,
      element: command.element,
      birth: command.birth,
      jobId: command.jobId,
      allocation: { ...command.allocation },
    }
  },
}

export const AdventureSettingsConverter = {
  toDto(settings: AdventureSettings): AdventureSettingsDto {
    return { ...settings }
  },
}
