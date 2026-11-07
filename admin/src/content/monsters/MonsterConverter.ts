import type { components } from "../../api/schema"
import { contentElements } from "../shared/elements"
import {
  parseArray,
  parseBoolean,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
} from "../shared/parse"
import { MonsterVo } from "./MonsterVo"

export type MonsterDto = components["schemas"]["MonsterRecord"]

export const MonsterConverter = {
  // 回應只有執行時才知道真正的形狀，所以逐欄檢查；不認識的欄位不會進到 VO
  toVo(dto: MonsterDto, path = "monster"): MonsterVo {
    const field = (name: keyof MonsterDto) => `${path}.${name}`
    return MonsterVo.create({
      id: parseString(dto.id, field("id")),
      name: parseString(dto.name, field("name")),
      level: parseNumber(dto.level, field("level")),
      hitPoints: parseNumber(dto.hitPoints, field("hitPoints")),
      skillPoints: parseNumber(dto.skillPoints, field("skillPoints")),
      attack: parseNumber(dto.attack, field("attack")),
      magicAttack: parseNumber(dto.magicAttack, field("magicAttack")),
      defense: parseNumber(dto.defense, field("defense")),
      magicDefense: parseNumber(dto.magicDefense, field("magicDefense")),
      strength: parseNumber(dto.strength, field("strength")),
      intelligence: parseNumber(dto.intelligence, field("intelligence")),
      vitality: parseNumber(dto.vitality, field("vitality")),
      agility: parseNumber(dto.agility, field("agility")),
      charisma: parseNumber(dto.charisma, field("charisma")),
      faith: parseNumber(dto.faith, field("faith")),
      element: parseOneOf(dto.element, field("element"), contentElements),
      jobExperience: parseNumber(dto.jobExperience, field("jobExperience")),
      encounterRate: parseNumber(dto.encounterRate, field("encounterRate")),
      placeId: parseNumber(dto.placeId, field("placeId")),
      subPlaceIndexes: parseArray(dto.subPlaceIndexes, field("subPlaceIndexes"), parseNumber),
      drops: parseArray(dto.drops, field("drops"), (item, itemPath) => {
        const drop = parseObject(item, itemPath)
        return {
          itemId: parseString(drop.itemId, `${itemPath}.itemId`),
          dropRarity: parseNumber(drop.dropRarity, `${itemPath}.dropRarity`),
        }
      }),
      skills: parseArray(dto.skills, field("skills"), (item, itemPath) => {
        const skill = parseObject(item, itemPath)
        return {
          skillId: parseString(skill.skillId, `${itemPath}.skillId`),
          level: parseNumber(skill.level, `${itemPath}.level`),
          activationRate: parseNumber(skill.activationRate, `${itemPath}.activationRate`),
        }
      }),
      exclusiveMissionId: parseNullableString(dto.exclusiveMissionId, field("exclusiveMissionId")),
      isNpc: parseBoolean(dto.isNpc, field("isNpc")),
      npcMessage: parseNullableString(dto.npcMessage, field("npcMessage")),
    })
  },

  toDto(vo: MonsterVo): MonsterDto {
    return {
      id: vo.id,
      name: vo.name,
      level: vo.level,
      hitPoints: vo.hitPoints,
      skillPoints: vo.skillPoints,
      attack: vo.attack,
      magicAttack: vo.magicAttack,
      defense: vo.defense,
      magicDefense: vo.magicDefense,
      strength: vo.strength,
      intelligence: vo.intelligence,
      vitality: vo.vitality,
      agility: vo.agility,
      charisma: vo.charisma,
      faith: vo.faith,
      element: vo.element,
      jobExperience: vo.jobExperience,
      encounterRate: vo.encounterRate,
      placeId: vo.placeId,
      subPlaceIndexes: [...vo.subPlaceIndexes],
      drops: vo.drops.map((drop) => ({ ...drop })),
      skills: vo.skills.map((skill) => ({ ...skill })),
      exclusiveMissionId: vo.exclusiveMissionId,
      isNpc: vo.isNpc,
      npcMessage: vo.npcMessage,
    }
  },
}
