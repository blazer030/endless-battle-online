import { formatNumber } from "./format"
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

// rewardId 是傭兵或道具的 id，依 kind 而定
export type HonourExchange = {
  readonly id: string
  readonly kind: HonourExchangeKind
  readonly rewardId: string
  readonly rewardName: string
  readonly quantity: number
  readonly cost: readonly HonourCost[]
  readonly affordable: boolean
}

type HonourShopVoParams = { medals: readonly HonourMedal[]; exchanges: readonly HonourExchange[] }

export class HonourShopVo {
  readonly medals: readonly HonourMedal[]
  readonly exchanges: readonly HonourExchange[]

  private constructor(params: HonourShopVoParams) {
    this.medals = params.medals
    this.exchanges = params.exchanges
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
