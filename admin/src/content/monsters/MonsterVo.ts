export const contentElements = ["None", "Earth", "Water", "Fire", "Wood", "Wind", "Poison"] as const

export type ContentElement = (typeof contentElements)[number]

export type MonsterDropVo = { readonly itemId: string; readonly dropRarity: number }

export type MonsterSkillVo = {
  readonly skillId: string
  readonly level: number
  readonly activationRate: number
}

type MonsterVoParams = {
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
  subPlaceIndexes: readonly number[]
  drops: readonly MonsterDropVo[]
  skills: readonly MonsterSkillVo[]
  exclusiveMissionId: string | null
  isNpc: boolean
  npcMessage: string | null
}

export class MonsterVo {
  readonly id: string
  readonly name: string
  readonly level: number
  readonly hitPoints: number
  readonly skillPoints: number
  readonly attack: number
  readonly magicAttack: number
  readonly defense: number
  readonly magicDefense: number
  readonly strength: number
  readonly intelligence: number
  readonly vitality: number
  readonly agility: number
  readonly charisma: number
  readonly faith: number
  readonly element: ContentElement
  readonly jobExperience: number
  readonly encounterRate: number
  readonly placeId: number
  readonly subPlaceIndexes: readonly number[]
  readonly drops: readonly MonsterDropVo[]
  readonly skills: readonly MonsterSkillVo[]
  readonly exclusiveMissionId: string | null
  readonly isNpc: boolean
  readonly npcMessage: string | null

  private constructor(params: MonsterVoParams) {
    this.id = params.id
    this.name = params.name
    this.level = params.level
    this.hitPoints = params.hitPoints
    this.skillPoints = params.skillPoints
    this.attack = params.attack
    this.magicAttack = params.magicAttack
    this.defense = params.defense
    this.magicDefense = params.magicDefense
    this.strength = params.strength
    this.intelligence = params.intelligence
    this.vitality = params.vitality
    this.agility = params.agility
    this.charisma = params.charisma
    this.faith = params.faith
    this.element = params.element
    this.jobExperience = params.jobExperience
    this.encounterRate = params.encounterRate
    this.placeId = params.placeId
    this.subPlaceIndexes = params.subPlaceIndexes
    this.drops = params.drops
    this.skills = params.skills
    this.exclusiveMissionId = params.exclusiveMissionId
    this.isNpc = params.isNpc
    this.npcMessage = params.npcMessage
  }

  static create(params: MonsterVoParams): MonsterVo {
    return new MonsterVo(params)
  }
}
