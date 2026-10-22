import { translateReasons } from "./reasons"
import type { MercenaryCatalogueEntry, MercenaryContract, WorldArea, WorldPlace } from "./types"

const format = (value: number) => value.toLocaleString("zh-TW")

export type CatalogueRow = {
  name: string
  price: string
  fights: string
  money: string
  drop: string
  exp: string
  deathCost: string
  // 價格為 null 的傭兵只能在勳章所兌換
  hireable: boolean
}

export const catalogueRow = (entry: MercenaryCatalogueEntry): CatalogueRow => ({
  name: entry.name,
  price: entry.price === null ? "勳章所兌換" : format(entry.price),
  fights: format(entry.fights),
  money: `${entry.moneyPercent}%`,
  drop: `${entry.dropPercent}%`,
  exp: `${entry.expPercent}%`,
  deathCost: format(entry.deathCost),
  hireable: entry.price !== null,
})

export const contractStatus = (contract: MercenaryContract): string[] => [
  `狀態：${contract.fighting ? "戰鬥中" : "休息中"}`,
  ...(contract.fighting && contract.placeName !== null ? [`冒險地：${contract.placeName}`] : []),
  `剩餘次數 ${format(contract.remaining)}`,
]

// 禁區（restricted）不能派傭兵
export const deployablePlaces = (areas: WorldArea[]): WorldPlace[] =>
  areas.flatMap((area) => area.places).filter((place) => !place.restricted)

const reasonTexts: Record<string, string> = {
  mercenaryUnknown: "沒有這位傭兵",
  honourOnly: "這位傭兵只能在勳章所兌換",
  mercenaryHired: "只能僱用一位傭兵",
  noMercenary: "還沒有僱用傭兵",
  placeUnknown: "沒有選擇冒險地點",
  placeRestricted: "傭兵不能前往禁區",
  nameInvalid: "名稱長度不符",
  notEnoughMoney: "金錢不足",
}

export const mercenaryErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)
