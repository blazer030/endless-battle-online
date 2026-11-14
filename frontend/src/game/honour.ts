import { buffLabel, type BuffKind } from "./buffs"
import { formatNumber } from "./format"
import {
  bonusLabels,
  categoryLabel,
  type ConsumableKind,
  type ItemCategory,
  type StatBonus,
} from "./inventory"
import { translateReasons } from "./reasons"
import { statLabel } from "./stats"

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

// ---- 勳章所新畫面（設計板 HonourFit）的純模型：莉瑟的話、陳列板分區、詳情文字、版位 ----

// 展開圓心＝公會前「勳章所」紙籤中心（紙籤跟著全景縮放，矮舞台時被全景右緣擋住）
export const honourRevealOrigin = (stageHeight: number) => {
  const scale = stageHeight / 844
  return {
    x: Math.round(Math.max(390 - 139 * scale, 195 + 78 * scale)),
    y: Math.round(256 * scale + 19),
  }
}

// 勳章盒貼舞台下緣：寬 330～374、高＝寬 × 0.47；top 是盒上緣
export const medalCase = (stageHeight: number) => {
  const width = Math.round(330 + (stageHeight - 624) * 0.282)
  const height = Math.round(width * 0.47)
  const bottom = Math.round(20 + (stageHeight - 624) * 0.051)
  return { width, height, bottom, top: stageHeight - bottom - height }
}

// 陳列板從舞台上緣 50 開始，高＝盒上緣 − 112
export const rackHeight = (stageHeight: number) => medalCase(stageHeight).top - 112

type RackSection = { title: string; exchanges: HonourExchange[] }

const rackOrder: readonly (ItemCategory | "Mercenary")[] = [
  "Mercenary",
  "Weapon",
  "Head",
  "Body",
  "Hand",
  "Foot",
  "Accessory",
  "Key",
  "Consumable",
  "Stone",
  "RefineStone",
  "Material",
  "Medal",
]

const sectionOf = (exchange: HonourExchange) => exchange.item?.category ?? "Mercenary"

const needed = (exchange: HonourExchange, medalId: string) =>
  exchange.cost.find((cost) => cost.itemId === medalId)?.quantity

// 那種勳章換得到的全部：依分區，組內依這種勳章的需要數由少到多、同數照內容順序
export const rackSections = (
  exchanges: readonly HonourExchange[],
  medalId: string,
): RackSection[] => {
  const paid = exchanges
    .map((exchange, index) => ({ exchange, index, quantity: needed(exchange, medalId) }))
    .filter(
      (row): row is { exchange: HonourExchange; index: number; quantity: number } =>
        row.quantity !== undefined,
    )
  return rackOrder
    .map((section) => ({
      title: section === "Mercenary" ? "傭兵" : categoryLabel(section),
      exchanges: paid
        .filter((row) => sectionOf(row.exchange) === section)
        .sort((left, right) => left.quantity - right.quantity || left.index - right.index)
        .map((row) => row.exchange),
    }))
    .filter((section) => section.exchanges.length > 0)
}

export type ClerkLine = { text: string; failed: boolean }

// first：剛進來；browsed：從陳列板回來、沒換過；exchanged：換過再回來
export type CounterState = {
  load: "loading" | "ready" | "failed"
  medals: number
  visit: "first" | "browsed" | "exchanged"
}

// 櫃檯對話框裡莉瑟說的話（\n 是對話框裡的換行）
export const clerkGreeting = (state: CounterState): ClerkLine => {
  if (state.load === "loading")
    return { text: "歡迎來到勳章所！\n我翻一下您的帳冊喔……", failed: false }
  if (state.load === "failed") return { text: "哎呀，帳冊一時\n翻不到了……", failed: true }
  if (state.visit === "exchanged") return { text: "兌換好了！\n祝您冒險一路順風。", failed: false }
  if (state.visit === "browsed")
    return { text: "慢慢挑沒關係，\n陳列板不會跑掉的～", failed: false }
  if (state.medals === 0) return { text: "啊，您還沒有勳章呢……\n有了再來找我喔！", failed: false }
  return { text: "歡迎來到勳章所！\n今天想兌換些什麼呢？", failed: false }
}

export type ClerkEvent =
  | { kind: "exchangedItem" }
  | { kind: "exchangedMercenary"; name: string }
  | { kind: "refused"; reasons: readonly string[]; category: ItemCategory | null }
  | { kind: "failed" }

const ClerkFailed = "咦，剛剛沒送出去……再試一次吧？"

// 羊皮紙裡印章上方莉瑟說的一句；多個原因照伺服器順序取第一個認得的
export const clerkLine = (event: ClerkEvent): ClerkLine => {
  switch (event.kind) {
    case "exchangedItem":
      return { text: "換好了！已經幫您放進背包囉。", failed: false }
    case "exchangedMercenary":
      return { text: `${event.name}的契約簽好囉，祝您順利！`, failed: false }
    case "failed":
      return { text: ClerkFailed, failed: true }
    case "refused": {
      const category = event.category
      for (const reason of event.reasons) {
        if (reason === "medalsMissing") return { text: "勳章好像還差一點呢……", failed: true }
        if (reason === "mercenaryHired")
          return { text: "您已經有一位傭兵了，一次只能一位喔。", failed: true }
        if (reason === "bagFull" && category !== null)
          return { text: `哎呀，${categoryLabel(category)}的格子已經滿了……`, failed: true }
      }
      return { text: ClerkFailed, failed: true }
    }
  }
}

const durationText = (seconds: number) =>
  seconds % 3600 === 0 ? `${seconds / 3600} 小時` : `${Math.round(seconds / 60)} 分鐘`

// 消耗品與鑰匙的效果；裝備與材料沒有
export const effectText = (item: HonourItemDetail): string | null => {
  if (item.key !== null) return `開啟「${item.key.placeName}」${durationText(item.key.seconds)}`
  switch (item.consumable) {
    case "Supply":
      return item.supplyHealthPoints > 0
        ? `補給欄自動使用：HP 回復 ${formatNumber(item.supplyHealthPoints)}`
        : `補給欄自動使用：SP 回復 ${formatNumber(item.supplyManaPoints)}`
    case "Elixir":
      return item.elixir === null
        ? null
        : `${buffLabel(item.elixir.kind)} ×${item.elixir.multiplier}，${durationText(item.elixir.seconds)}`
    case "Capture":
      return `捕捉寵物用（捕捉力 ${item.amount}）`
    case "StoneBox":
      return `打開隨機獲得魔石 ×${item.amount}`
    case "RefineBox":
      return `打開隨機獲得精鍊石 ×${item.amount}`
    case "PetEgg":
      return "孵出一隻隨機寵物"
    default:
      return null
  }
}

// 裝備需求：夠不夠由伺服器以裸點判斷，畫面只標紅綠
export const requirementRows = (item: HonourItemDetail) =>
  item.requirements.map((requirement) => ({
    text: `${statLabel(requirement.stat)} ${requirement.minimum}`,
    met: requirement.met,
  }))

export const categoryLine = (item: HonourItemDetail) =>
  item.bound ? `${categoryLabel(item.category)}・綁定` : categoryLabel(item.category)

export const slotLine = (item: HonourItemDetail) =>
  item.holes > 0 || item.refineMax > 0 ? `孔 ${item.holes}・精鍊上限 +${item.refineMax}` : null

export const bonusRows = (bonus: StatBonus) =>
  bonusLabels
    .filter(({ key }) => bonus[key] !== 0)
    .map(({ key, label }) => ({ label, value: `+${formatNumber(bonus[key])}` }))

export const mercenaryRows = (mercenary: HonourMercenaryDetail) => [
  { label: "可戰鬥", value: `${formatNumber(mercenary.fights)} 次` },
  { label: "金錢", value: `${mercenary.moneyPercent}%` },
  { label: "寶物", value: `${mercenary.dropPercent}%` },
  { label: "經驗", value: `${mercenary.experiencePercent}%` },
  { label: "戰敗消耗", value: `${mercenary.deathCost} 次` },
]

export const quantityBadge = (quantity: number) => (quantity > 1 ? `×${quantity}` : null)

// 印章停用時旁邊的說明；已有傭兵先於勳章不足
export const sealBlock = (exchange: HonourExchange, hiredName: string | null) => {
  if (exchange.kind === "mercenary" && hiredName !== null) return `僱用中：${hiredName}`
  if (!exchange.affordable) return "勳章不足"
  return null
}
