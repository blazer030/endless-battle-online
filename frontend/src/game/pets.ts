import { translateReasons } from "./reasons"
import type { PetFood, PetPersonality, PetsView, PetView } from "./types"

const format = (value: number) => value.toLocaleString("zh-TW")

// WOG js/wog_pet.js 的 pet_type
const personalityLabels: Record<PetPersonality, string> = {
  Aggressive: "積極",
  Cold: "冷酷",
  Ironwall: "鐵壁",
  Desperate: "危急",
}

const statLabels: Record<PetFood["stat"], string> = {
  Attack: "AT",
  Magic: "MT",
  Defense: "DEF",
  Spirit: "出擊值",
}

export const personalityLabel = (personality: PetPersonality) => personalityLabels[personality]

export const petTitle = (pet: Pick<PetView, "name" | "species" | "age">) =>
  `${pet.name}－${pet.species} ${pet.age}歲`

export const petCountText = (view: Pick<PetsView, "pets" | "maxPets">) =>
  `寵物 ${view.pets.length}/${view.maxPets}`

export const petStats = (pet: PetView): { label: string; value: string }[] => [
  { label: "AT", value: format(pet.attack) },
  { label: "MT", value: format(pet.magic) },
  { label: "DEF", value: format(pet.defense) },
  { label: "飽腹", value: format(pet.satiety) },
  { label: "個性", value: personalityLabel(pet.personality) },
  { label: "出擊值", value: format(pet.spirit) },
  { label: "親密度", value: format(pet.intimacy) },
]

export const feedOptionLabel = (food: PetFood) =>
  `${food.name}（${statLabels[food.stat]}+${format(food.amount)}，${format(food.cost)} 金）`

export const canFeed = (pet: Pick<PetView, "nextFeedAt">, now: Date) =>
  now.getTime() >= new Date(pet.nextFeedAt).getTime()

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
