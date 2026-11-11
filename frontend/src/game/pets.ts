import { formatNumber } from "./format"
import { translateReasons } from "./reasons"
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

const statLabels: Record<PetFoodStat, string> = {
  Attack: "AT",
  Magic: "MT",
  Defense: "DEF",
  Spirit: "出擊值",
}

export const personalityLabel = (personality: PetPersonality) => personalityLabels[personality]

export const petTitle = (pet: Pick<Pet, "name" | "species" | "age">) =>
  `${pet.name}－${pet.species} ${pet.age}歲`

export const petCountText = (view: Pick<PetsVo, "pets" | "maxPets">) =>
  `寵物 ${view.pets.length}/${view.maxPets}`

export const petStats = (pet: Pet): { label: string; value: string }[] => [
  { label: "AT", value: formatNumber(pet.attack) },
  { label: "MT", value: formatNumber(pet.magic) },
  { label: "DEF", value: formatNumber(pet.defense) },
  { label: "飽腹", value: formatNumber(pet.satiety) },
  { label: "個性", value: personalityLabel(pet.personality) },
  { label: "出擊值", value: formatNumber(pet.spirit) },
  { label: "親密度", value: formatNumber(pet.intimacy) },
]

export const activationText = (pet: Pick<Pet, "activationPercent">) =>
  `出擊率約 ${formatNumber(pet.activationPercent)}%（對魅力與你相同的怪物，上限 35%）`

export const feedOptionLabel = (food: PetFood) =>
  `${food.name}（${statLabels[food.stat]}+${formatNumber(food.amount)}，${formatNumber(food.cost)} 金）`

export const canFeed = (pet: Pick<Pet, "nextFeedAt">, now: Date) =>
  now.getTime() >= pet.nextFeedAt.getTime()

// WOG 原文者照抄
const reasonTexts: Record<string, string> = {
  petUnknown: "沒有寵物資料",
  foodUnknown: "沒有選擇項目",
  tooSoon: "10 秒內無法餵食",
  full: "飽腹大於100無法餵食",
  overOwner: "寵物屬性不能超過玩家",
  nameInvalid: "名稱長度不符",
  notEnoughMoney: "金錢不足",
}

export const petErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)
