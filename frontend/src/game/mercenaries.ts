import type { CharacterVo } from "./character"

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
