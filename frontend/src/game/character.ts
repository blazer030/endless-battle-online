// 角色資料（來自 /api/characters/me 等回應），由 api/characterData.ts 轉換
export const attackKinds = ["Physical", "Magic"] as const

export type AttackKind = (typeof attackKinds)[number]

export type BaseStats = {
  readonly strength: number
  readonly intelligence: number
  readonly life: number
  readonly vitality: number
  readonly agility: number
  readonly charisma: number
  readonly faith: number
}

export type DerivedStats = {
  readonly attack: number
  readonly magicAttack: number
  readonly defense: number
  readonly magicDefense: number
  readonly maxHealthPoints: number
  readonly maxManaPoints: number
}

// 有效值（裸點 + 裝備）
export type CharacterStats = BaseStats & DerivedStats

export type JobProficiency = {
  readonly jobId: string
  readonly proficiency: number
  readonly skillPoints: number
}

// attack 為 null 表示自動；補給與住宿 -1 表示略過該條件；逃跑 0 表示不逃
export type AdventureSettings = {
  readonly attack: AttackKind | null
  readonly potionHealthPointsPercent: number
  readonly potionManaPointsPercent: number
  readonly restHealthPointsPercent: number
  readonly restManaPointsPercent: number
  readonly escapeHealthPointsPercent: number
}

export type StatAllocation = {
  readonly strength: number
  readonly intelligence: number
  readonly life: number
  readonly agility: number
}

export type CreateCharacterCommand = {
  readonly name: string
  readonly sex: number
  readonly element: number
  readonly birth: number
  readonly jobId: string
  readonly allocation: StatAllocation
}

type CharacterVoParams = {
  name: string
  level: number
  experience: number
  nextLevelExperience: number
  money: number
  bank: number
  healthPoints: number
  manaPoints: number
  sex: number
  element: number
  birth: number
  jobId: string
  jobName: string
  stats: CharacterStats
  baseStats: BaseStats
  placeId: number
  placeName: string
  subPlace: number | null
  subPlaceName: string | null
  areaName: string
  jobProficiency: JobProficiency[]
  settings: AdventureSettings
}

export class CharacterVo {
  readonly name: string
  readonly level: number
  readonly experience: number
  readonly nextLevelExperience: number
  readonly money: number
  readonly bank: number
  readonly healthPoints: number
  readonly manaPoints: number
  readonly sex: number
  readonly element: number
  readonly birth: number
  readonly jobId: string
  readonly jobName: string
  readonly stats: CharacterStats
  // 裸點；畫面顯示「裸點（+裝備）」
  readonly baseStats: BaseStats
  readonly placeId: number
  readonly placeName: string
  // 沒有子地區的地區為 null
  readonly subPlace: number | null
  readonly subPlaceName: string | null
  readonly areaName: string
  readonly jobProficiency: readonly JobProficiency[]
  readonly settings: AdventureSettings

  private constructor(params: CharacterVoParams) {
    this.name = params.name
    this.level = params.level
    this.experience = params.experience
    this.nextLevelExperience = params.nextLevelExperience
    this.money = params.money
    this.bank = params.bank
    this.healthPoints = params.healthPoints
    this.manaPoints = params.manaPoints
    this.sex = params.sex
    this.element = params.element
    this.birth = params.birth
    this.jobId = params.jobId
    this.jobName = params.jobName
    this.stats = params.stats
    this.baseStats = params.baseStats
    this.placeId = params.placeId
    this.placeName = params.placeName
    this.subPlace = params.subPlace
    this.subPlaceName = params.subPlaceName
    this.areaName = params.areaName
    this.jobProficiency = params.jobProficiency
    this.settings = params.settings
  }

  static create(params: CharacterVoParams): CharacterVo {
    return new CharacterVo(params)
  }
}
