import type { BuffKind } from "./buffs"
import { formatNumber } from "./format"
import type { ConsumableKind, ItemCategory, StatBonus } from "./inventory"
import { translateReasons } from "./reasons"

// 勳章所的資料（來自 /api/honour），由 api/companionData.ts 轉換
export type HonourMedal = { readonly itemId: string; readonly name: string; readonly owned: number }

export type HonourCost = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
  readonly owned: number
}

export const honourExchangeKinds = ["mercenary", "item"] as const
export type HonourExchangeKind = (typeof honourExchangeKinds)[number]

export const honourRequirementStats = ["strength", "intelligence", "agility", "charisma"] as const
export type HonourRequirementStat = (typeof honourRequirementStats)[number]

// met：角色裸點是否達到（伺服器判斷）
export type HonourRequirement = {
  readonly stat: HonourRequirementStat
  readonly minimum: number
  readonly met: boolean
}

// 兌換道具的詳情；elixir 只有密藥有、key 只有鑰匙有
export type HonourItemDetail = {
  readonly category: ItemCategory
  readonly consumable: ConsumableKind
  readonly bonus: StatBonus
  readonly requirements: readonly HonourRequirement[]
  readonly holes: number
  readonly refineMax: number
  readonly bound: boolean
  readonly supplyHealthPoints: number
  readonly supplyManaPoints: number
  readonly amount: number
  readonly elixir: {
    readonly kind: BuffKind
    readonly multiplier: number
    readonly seconds: number
  } | null
  readonly key: { readonly placeName: string; readonly seconds: number } | null
}

export type HonourMercenaryDetail = {
  readonly fights: number
  readonly moneyPercent: number
  readonly dropPercent: number
  readonly experiencePercent: number
  readonly deathCost: number
}

// rewardId 是傭兵或道具的 id，依 kind 而定；item 與 mercenary 依 kind 恰好一個有值
export type HonourExchange = {
  readonly id: string
  readonly kind: HonourExchangeKind
  readonly rewardId: string
  readonly rewardName: string
  readonly quantity: number
  readonly cost: readonly HonourCost[]
  readonly affordable: boolean
  readonly item: HonourItemDetail | null
  readonly mercenary: HonourMercenaryDetail | null
}

type HonourShopVoParams = {
  medals: readonly HonourMedal[]
  exchanges: readonly HonourExchange[]
  hiredMercenaryName: string | null
}

export class HonourShopVo {
  readonly medals: readonly HonourMedal[]
  readonly exchanges: readonly HonourExchange[]
  // 目前契約的名字，沒有契約為 null
  readonly hiredMercenaryName: string | null

  private constructor(params: HonourShopVoParams) {
    this.medals = params.medals
    this.exchanges = params.exchanges
    this.hiredMercenaryName = params.hiredMercenaryName
  }

  static create(params: HonourShopVoParams): HonourShopVo {
    return new HonourShopVo(params)
  }
}

export const medalText = (medal: HonourMedal) => `${medal.name} ×${formatNumber(medal.owned)}`

export const rewardText = (exchange: Pick<HonourExchange, "rewardName" | "quantity">) =>
  `${exchange.rewardName} ×${formatNumber(exchange.quantity)}`

export const costText = (cost: HonourCost) => `${cost.name} ×${formatNumber(cost.quantity)}`

export const isShort = (cost: HonourCost) => cost.owned < cost.quantity

const groups: { kind: HonourExchangeKind; title: string }[] = [
  { kind: "mercenary", title: "傭兵" },
  { kind: "item", title: "道具" },
]

export const exchangeGroups = (exchanges: readonly HonourExchange[]) =>
  groups
    .map(({ kind, title }) => ({
      title,
      exchanges: exchanges.filter((exchange) => exchange.kind === kind),
    }))
    .filter((group) => group.exchanges.length > 0)

const reasonTexts: Record<string, string> = {
  honourUnknown: "沒有這個兌換項目",
  medalsMissing: "勳章不足",
  mercenaryHired: "只能僱用一位傭兵",
  bagFull: "背包已滿",
}

export const honourErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)
