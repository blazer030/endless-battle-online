import { formatNumber } from "./format"
import { translateReasons } from "./reasons"
import type { HonourCost, HonourExchange, HonourShop } from "./types"

export const medalText = (medal: HonourShop["medals"][number]) =>
  `${medal.name} ×${formatNumber(medal.owned)}`

export const rewardText = (exchange: Pick<HonourExchange, "rewardName" | "quantity">) =>
  `${exchange.rewardName} ×${formatNumber(exchange.quantity)}`

export const costText = (cost: HonourCost) => `${cost.name} ×${formatNumber(cost.quantity)}`

export const isShort = (cost: HonourCost) => cost.owned < cost.quantity

const groups: { kind: string; title: string }[] = [
  { kind: "mercenary", title: "傭兵" },
  { kind: "item", title: "道具" },
]

export const exchangeGroups = (exchanges: HonourExchange[]) =>
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
