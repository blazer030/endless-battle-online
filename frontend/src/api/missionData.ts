import {
  MissionBoardVo,
  MissionCompletionVo,
  type Mission,
  type MissionDeliver,
  type MissionKill,
  type MissionPet,
  type MissionRequiredSkill,
  type MissionRequirements,
  type MissionReward,
  type MissionRewardItem,
  type MissionRewardPet,
  type MissionRewardSkill,
} from "../game/missions"
import { CharacterConverter } from "./characterData"
import {
  parseArray,
  parseBoolean,
  parseNullableNumber,
  parseNullableString,
  parseNumber,
  parseObject,
  parseString,
  parseTimestamp,
} from "./parse"

const toKill = (value: unknown, path: string): MissionKill => {
  const dto = parseObject(value, path)
  return {
    monsterId: parseString(dto.monsterId, `${path}.monsterId`),
    monsterName: parseString(dto.monsterName, `${path}.monsterName`),
    placeId: parseNumber(dto.placeId, `${path}.placeId`),
    placeName: parseString(dto.placeName, `${path}.placeName`),
    count: parseNumber(dto.count, `${path}.count`),
    progress: parseNumber(dto.progress, `${path}.progress`),
  }
}

const toDeliver = (value: unknown, path: string): MissionDeliver => {
  const dto = parseObject(value, path)
  return {
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
    owned: parseNumber(dto.owned, `${path}.owned`),
  }
}

const toRequirements = (value: unknown, path: string): MissionRequirements => {
  const dto = parseObject(value, path)
  return {
    jobId: parseNullableString(dto.jobId, `${path}.jobId`),
    sex: parseNullableNumber(dto.sex, `${path}.sex`),
    birth: parseNullableNumber(dto.birth, `${path}.birth`),
    prerequisiteId: parseNullableString(dto.prerequisiteId, `${path}.prerequisiteId`),
    prerequisiteTitle: parseNullableString(dto.prerequisiteTitle, `${path}.prerequisiteTitle`),
  }
}

const toRewardItem = (value: unknown, path: string): MissionRewardItem => {
  const dto = parseObject(value, path)
  return {
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
  }
}

const toRewardSkill = (value: unknown, path: string): MissionRewardSkill | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    skillId: parseString(dto.skillId, `${path}.skillId`),
    name: parseString(dto.name, `${path}.name`),
    level: parseNumber(dto.level, `${path}.level`),
  }
}

const toRewardPet = (value: unknown, path: string): MissionRewardPet | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    monsterId: parseString(dto.monsterId, `${path}.monsterId`),
    name: parseString(dto.name, `${path}.name`),
    level: parseNumber(dto.level, `${path}.level`),
  }
}

const toReward = (value: unknown, path: string): MissionReward => {
  const dto = parseObject(value, path)
  return {
    experience: parseNumber(dto.experience, `${path}.experience`),
    money: parseNumber(dto.money, `${path}.money`),
    items: parseArray(dto.items, `${path}.items`, toRewardItem),
    skill: toRewardSkill(dto.skill, `${path}.skill`),
    pet: toRewardPet(dto.pet, `${path}.pet`),
  }
}

const toMissionPet = (value: unknown, path: string): MissionPet | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    monsterId: parseString(dto.monsterId, `${path}.monsterId`),
    name: parseString(dto.name, `${path}.name`),
    carried: parseBoolean(dto.carried, `${path}.carried`),
  }
}

const toRequiredSkill = (value: unknown, path: string): MissionRequiredSkill | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    skillId: parseString(dto.skillId, `${path}.skillId`),
    name: parseString(dto.name, `${path}.name`),
    level: parseNumber(dto.level, `${path}.level`),
    met: parseBoolean(dto.met, `${path}.met`),
  }
}

const toMission = (value: unknown, path: string): Mission => {
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    giver: parseString(dto.giver, `${path}.giver`),
    title: parseString(dto.title, `${path}.title`),
    description: parseString(dto.description, `${path}.description`),
    level: parseNumber(dto.level, `${path}.level`),
    placeId: parseNullableNumber(dto.placeId, `${path}.placeId`),
    placeName: parseNullableString(dto.placeName, `${path}.placeName`),
    daily: parseBoolean(dto.daily, `${path}.daily`),
    kills: parseArray(dto.kills, `${path}.kills`, toKill),
    deliver: parseArray(dto.deliver, `${path}.deliver`, toDeliver),
    requirements: toRequirements(dto.requirements, `${path}.requirements`),
    rewards: toReward(dto.rewards, `${path}.rewards`),
    ready: parseBoolean(dto.ready, `${path}.ready`),
    expiresAt: dto.expiresAt === null ? null : parseTimestamp(dto.expiresAt, `${path}.expiresAt`),
    deliverPet: toMissionPet(dto.deliverPet, `${path}.deliverPet`),
    requiredSkill: toRequiredSkill(dto.requiredSkill, `${path}.requiredSkill`),
  }
}

export const MissionBoardConverter = {
  toVo(value: unknown, path = "missions"): MissionBoardVo {
    const dto = parseObject(value, path)
    return MissionBoardVo.create({
      available: parseArray(dto.available, `${path}.available`, toMission),
      active: parseArray(dto.active, `${path}.active`, toMission),
      completed: parseArray(dto.completed, `${path}.completed`, toMission),
      activeLimit: parseNumber(dto.activeLimit, `${path}.activeLimit`),
      nextDailyReset: parseTimestamp(dto.nextDailyReset, `${path}.nextDailyReset`),
    })
  },
}

export const MissionCompletionConverter = {
  toVo(value: unknown, path = "missionCompletion"): MissionCompletionVo {
    const dto = parseObject(value, path)
    return MissionCompletionVo.create({
      board: MissionBoardConverter.toVo(dto.board, `${path}.board`),
      character: CharacterConverter.toVo(dto.character, `${path}.character`),
      completionMessage: parseString(dto.completionMessage, `${path}.completionMessage`),
      experience: parseNumber(dto.experience, `${path}.experience`),
      money: parseNumber(dto.money, `${path}.money`),
      items: parseArray(dto.items, `${path}.items`, toRewardItem),
      skill: toRewardSkill(dto.skill, `${path}.skill`),
      skillGranted: parseBoolean(dto.skillGranted, `${path}.skillGranted`),
      levelsGained: parseNumber(dto.levelsGained, `${path}.levelsGained`),
      pet: toRewardPet(dto.pet, `${path}.pet`),
    })
  },
}
