import { MonsterVo, type ContentElement } from "./MonsterVo"

export type MonsterFormValues = {
  id: string
  name: string
  level: number
  hitPoints: number
  skillPoints: number
  attack: number
  magicAttack: number
  defense: number
  magicDefense: number
  strength: number
  intelligence: number
  vitality: number
  agility: number
  charisma: number
  faith: number
  element: ContentElement
  jobExperience: number
  encounterRate: number
  placeId: number
  subPlaceIndexes: number[]
  drops: { itemId: string; dropRarity: number }[]
  skills: { skillId: string; level: number; activationRate: number }[]
  // 空字串代表「無」：下拉選單的值不能是 null
  exclusiveMissionId: string
  isNpc: boolean
  npcMessage: string
}

export const blankMonsterValues: MonsterFormValues = {
  id: "",
  name: "",
  level: 0,
  hitPoints: 0,
  skillPoints: 0,
  attack: 0,
  magicAttack: 0,
  defense: 0,
  magicDefense: 0,
  strength: 0,
  intelligence: 0,
  vitality: 0,
  agility: 0,
  charisma: 0,
  faith: 0,
  element: "None",
  jobExperience: 0,
  encounterRate: 0,
  placeId: 0,
  subPlaceIndexes: [],
  drops: [],
  skills: [],
  exclusiveMissionId: "",
  isNpc: false,
  npcMessage: "",
}

export function monsterToValues(monster: MonsterVo): MonsterFormValues {
  return {
    id: monster.id,
    name: monster.name,
    level: monster.level,
    hitPoints: monster.hitPoints,
    skillPoints: monster.skillPoints,
    attack: monster.attack,
    magicAttack: monster.magicAttack,
    defense: monster.defense,
    magicDefense: monster.magicDefense,
    strength: monster.strength,
    intelligence: monster.intelligence,
    vitality: monster.vitality,
    agility: monster.agility,
    charisma: monster.charisma,
    faith: monster.faith,
    element: monster.element,
    jobExperience: monster.jobExperience,
    encounterRate: monster.encounterRate,
    placeId: monster.placeId,
    subPlaceIndexes: [...monster.subPlaceIndexes],
    drops: monster.drops.map((drop) => ({ ...drop })),
    skills: monster.skills.map((skill) => ({ ...skill })),
    exclusiveMissionId: monster.exclusiveMissionId ?? "",
    isNpc: monster.isNpc,
    npcMessage: monster.npcMessage ?? "",
  }
}

// NPC 不戰鬥：隱藏的戰鬥欄位存回 0，掉落與技能清空
const npcCombatValues = {
  level: 0,
  hitPoints: 0,
  skillPoints: 0,
  attack: 0,
  magicAttack: 0,
  defense: 0,
  magicDefense: 0,
  strength: 0,
  intelligence: 0,
  vitality: 0,
  agility: 0,
  charisma: 0,
  faith: 0,
  element: "None",
  jobExperience: 0,
  drops: [],
  skills: [],
} as const

export function monsterFromValues(values: MonsterFormValues): MonsterVo {
  return MonsterVo.create({
    ...values,
    ...(values.isNpc ? npcCombatValues : {}),
    id: values.id.trim(),
    exclusiveMissionId: values.exclusiveMissionId === "" ? null : values.exclusiveMissionId,
    npcMessage: values.isNpc && values.npcMessage !== "" ? values.npcMessage : null,
  })
}
