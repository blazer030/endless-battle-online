import type { CharacterVo } from "./character"

// 牧場的資料（來自 /api/pets），由 api/companionData.ts 轉換
export const petPersonalities = ["Aggressive", "Cold", "Ironwall", "Desperate"] as const
export type PetPersonality = (typeof petPersonalities)[number]

// 果實加的能力
export const petFoodStats = ["Attack", "Magic", "Defense", "Spirit"] as const
export type PetFoodStat = (typeof petFoodStats)[number]

// nextFeedAt 之後才能再餵；activationPercent 是對魅力與玩家相同的怪物的出擊率
export type Pet = {
  readonly id: number
  readonly monsterId: string
  readonly species: string
  readonly name: string
  readonly personality: PetPersonality
  readonly attack: number
  readonly magic: number
  readonly defense: number
  readonly spirit: number
  readonly satiety: number
  readonly intimacy: number
  readonly carried: boolean
  readonly age: number
  readonly nextFeedAt: Date
  readonly activationPercent: number
}

export type PetFood = {
  readonly id: string
  readonly name: string
  readonly stat: PetFoodStat
  readonly amount: number
  readonly cost: number
}

type PetsVoParams = { pets: readonly Pet[]; maxPets: number; foods: readonly PetFood[] }

export class PetsVo {
  readonly pets: readonly Pet[]
  readonly maxPets: number
  readonly foods: readonly PetFood[]

  private constructor(params: PetsVoParams) {
    this.pets = params.pets
    this.maxPets = params.maxPets
    this.foods = params.foods
  }

  static create(params: PetsVoParams): PetsVo {
    return new PetsVo(params)
  }
}

// 餵食會扣錢，回應帶著更新後的角色
type PetFeedVoParams = { pets: PetsVo; character: CharacterVo }

export class PetFeedVo {
  readonly pets: PetsVo
  readonly character: CharacterVo

  private constructor(params: PetFeedVoParams) {
    this.pets = params.pets
    this.character = params.character
  }

  static create(params: PetFeedVoParams): PetFeedVo {
    return new PetFeedVo(params)
  }
}

// WOG js/wog_pet.js 的 pet_type
const personalityLabels: Record<PetPersonality, string> = {
  Aggressive: "積極",
  Cold: "冷酷",
  Ironwall: "鐵壁",
  Desperate: "危急",
}

export const personalityLabel = (personality: PetPersonality) => personalityLabels[personality]
