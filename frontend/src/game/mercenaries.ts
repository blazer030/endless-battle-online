import { formatNumber } from "./format"
import { translateReasons } from "./reasons"
import type { CharacterVo } from "./character"
import type { WorldAreaVo, WorldPlace } from "./world"

// 傭兵所的資料（來自 /api/mercenaries），由 api/companionData.ts 轉換
// price 為 null 的傭兵只能在勳章所兌換；百分比是金錢、掉落、經驗的分成
export type MercenaryCatalogueEntry = {
  readonly id: string
  readonly name: string
  readonly price: number | null
  readonly fights: number
  readonly moneyPercent: number
  readonly dropPercent: number
  readonly experiencePercent: number
  readonly deathCost: number
}

// nextFightAt 只在出擊中才有值
export type MercenaryContract = {
  readonly mercenaryId: string
  readonly name: string
  readonly remaining: number
  readonly placeId: number | null
  readonly placeName: string | null
  readonly fighting: boolean
  readonly nextFightAt: Date | null
}

type MercenaryOverviewVoParams = {
  catalogue: readonly MercenaryCatalogueEntry[]
  contract: MercenaryContract | null
}

export class MercenaryOverviewVo {
  readonly catalogue: readonly MercenaryCatalogueEntry[]
  readonly contract: MercenaryContract | null

  private constructor(params: MercenaryOverviewVoParams) {
    this.catalogue = params.catalogue
    this.contract = params.contract
  }

  static create(params: MercenaryOverviewVoParams): MercenaryOverviewVo {
    return new MercenaryOverviewVo(params)
  }
}

// 僱用會扣錢，回應帶著更新後的角色
type MercenaryHireVoParams = { mercenaries: MercenaryOverviewVo; character: CharacterVo }

export class MercenaryHireVo {
  readonly mercenaries: MercenaryOverviewVo
  readonly character: CharacterVo

  private constructor(params: MercenaryHireVoParams) {
    this.mercenaries = params.mercenaries
    this.character = params.character
  }

  static create(params: MercenaryHireVoParams): MercenaryHireVo {
    return new MercenaryHireVo(params)
  }
}

export type CatalogueRow = {
  name: string
  price: string
  fights: string
  money: string
  drop: string
  experience: string
  deathCost: string
  // 價格為 null 的傭兵只能在勳章所兌換
  hireable: boolean
}

export const catalogueRow = (entry: MercenaryCatalogueEntry): CatalogueRow => ({
  name: entry.name,
  price: entry.price === null ? "勳章所兌換" : formatNumber(entry.price),
  fights: formatNumber(entry.fights),
  money: `${entry.moneyPercent}%`,
  drop: `${entry.dropPercent}%`,
  experience: `${entry.experiencePercent}%`,
  deathCost: formatNumber(entry.deathCost),
  hireable: entry.price !== null,
})

export const contractStatus = (contract: MercenaryContract): string[] => [
  `狀態：${contract.fighting ? "戰鬥中" : "休息中"}`,
  ...(contract.fighting && contract.placeName !== null ? [`冒險地：${contract.placeName}`] : []),
  `剩餘次數 ${formatNumber(contract.remaining)}`,
]

// 禁區（restricted）不能派傭兵
export const deployablePlaces = (areas: WorldAreaVo[]): WorldPlace[] =>
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
