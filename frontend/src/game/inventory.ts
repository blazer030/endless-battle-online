import { formatNumber } from "./format"
import { buffLabel, durationMinutes } from "./buffs"
import { statLabel } from "./stats"
import type {
  BagCategoryView,
  CharacterView,
  EquipmentSlot,
  InventoryItem,
  InventoryView,
  ItemCategory,
  ItemUseResult,
  ItemRequirement,
  StatBonusView,
} from "./types"

// 與後端 InventoryRules／Shop／Bank 的常數一致
export const MaxStackQuantity = 99
export const MinShopLevel = 1
export const MaxShopLevel = 7
export const BankMinimumLevel = 15
const SellRate = 0.5

export const itemCategories: { id: ItemCategory; label: string }[] = [
  { id: "Weapon", label: "武器" },
  { id: "Head", label: "頭部" },
  { id: "Body", label: "身體" },
  { id: "Hand", label: "手部" },
  { id: "Foot", label: "腳部" },
  { id: "Accessory", label: "飾品" },
  { id: "Consumable", label: "道具" },
  { id: "Stone", label: "魔石" },
  { id: "RefineStone", label: "精鍊石" },
  { id: "Material", label: "材料" },
  { id: "Medal", label: "勳章" },
  { id: "Key", label: "鑰匙" },
]

// 勳章只靠掉落與任務取得、鑰匙只在勳章所兌換，商店沒有這兩個分類
export const shopCategories = itemCategories.filter(({ id }) => id !== "Medal" && id !== "Key")

export const categoryLabel = (category: ItemCategory) =>
  itemCategories.find(({ id }) => id === category)?.label ?? category

export const equipmentSlots: { id: EquipmentSlot; label: string }[] = [
  { id: "Weapon", label: "武器" },
  { id: "Head", label: "頭部" },
  { id: "Body", label: "身體" },
  { id: "Hand", label: "手部" },
  { id: "Foot", label: "腳部" },
  { id: "Accessory", label: "飾品" },
  { id: "Supply", label: "補給" },
]

const equipmentCategories: ReadonlySet<ItemCategory> = new Set([
  "Weapon",
  "Head",
  "Body",
  "Hand",
  "Foot",
  "Accessory",
])

export const isEquipment = (category: ItemCategory) => equipmentCategories.has(category)

// 補給欄放補給品或捕捉道具
export const isSupply = (item: InventoryItem) =>
  item.consumable === "Supply" || item.consumable === "Capture"

const usableKinds: ReadonlySet<InventoryItem["consumable"]> = new Set([
  "StatPotion",
  "BagExpansion",
  "DepotExpansion",
  "StoneBox",
  "RefineBox",
  "PetEgg",
  "ProficiencyBook",
  "ExperienceCapsule",
  "Elixir",
])

// 能從背包直接使用的消耗品；補給品與捕捉道具要放到補給欄
export const isUsable = (item: InventoryItem) => usableKinds.has(item.consumable)

// 使用後的結果行；寶箱沿用 WOG xmas_item 的「恭喜抽到」，名稱以空白分隔
export const itemUseResultLines = (
  item: Pick<InventoryItem, "consumable" | "amount" | "name">,
  result: Pick<ItemUseResult, "obtained" | "petName" | "skillPoints" | "levelsGained" | "buff">,
  now: Date,
): string[] => {
  const lines: string[] = []
  if (result.obtained.length > 0) lines.push(`恭喜抽到 ${result.obtained.join(" ")}`)
  if (result.petName !== null) lines.push(`獲得寵物 ${result.petName}`)
  if (result.skillPoints > 0) lines.push(`技能點 +${formatNumber(result.skillPoints)}`)
  if (item.consumable === "ExperienceCapsule")
    lines.push(`經驗值 +${formatNumber(item.amount)}${result.levelsGained > 0 ? "，升級了！" : ""}`)
  if (result.buff !== null)
    lines.push(
      `${item.name}生效：${buffLabel(result.buff.kind)} ×${result.buff.multiplier}，持續 ${durationMinutes(result.buff.endsAt, now)} 分鐘`,
    )
  return lines
}

export const bagSlotsOf = (inventory: InventoryView, category: ItemCategory): BagCategoryView =>
  inventory.bag.find((entry) => entry.category === category) ?? {
    category,
    capacity: 0,
    items: [],
  }

// 每一列佔一格：裝備一件一格，可堆疊的一疊一格
export const usageText = (slots: BagCategoryView) => `${slots.items.length}/${slots.capacity}`

export const bagItems = (inventory: InventoryView) =>
  itemCategories.flatMap(({ id }) => bagSlotsOf(inventory, id).items)

export const itemTitle = (item: InventoryItem) =>
  isEquipment(item.category)
    ? `${item.name} +${item.refineLevel}`
    : `${item.name} ×${item.quantity}`

export const holeMarks = (item: InventoryItem) =>
  "◆".repeat(item.sockets.length) + "◇".repeat(Math.max(0, item.holes - item.sockets.length))

const bonusLabels: { key: keyof StatBonusView; label: string }[] = [
  { key: "at", label: "物攻" },
  { key: "mat", label: "魔攻" },
  { key: "df", label: "物防" },
  { key: "mdf", label: "魔防" },
  { key: "hpMax", label: "HP 上限" },
  { key: "str", label: "力量" },
  { key: "smart", label: "智力" },
  { key: "life", label: "生命" },
  { key: "vit", label: "體質" },
  { key: "agi", label: "敏捷" },
  { key: "au", label: "魅力" },
  { key: "be", label: "信仰" },
]

export const bonusText = (bonus: StatBonusView) =>
  bonusLabels
    .filter(({ key }) => bonus[key] !== 0)
    .map(({ key, label }) => `${label} +${bonus[key]}`)
    .join("、")

export const supplyText = (item: Pick<InventoryItem, "supplyHp" | "supplySp">) =>
  `HP +${item.supplyHp}、SP +${item.supplySp}`

export const candidatesFor = (inventory: InventoryView, slot: EquipmentSlot) =>
  slot === "Supply"
    ? bagSlotsOf(inventory, "Consumable").items.filter(isSupply)
    : bagSlotsOf(inventory, slot).items

export const equippedIn = (inventory: InventoryView, slot: EquipmentSlot) =>
  inventory.equipment.find((entry) => entry.slot === slot) ?? null

export type Requirement = { text: string; met: boolean }

export type JobName = { id: string; name: string }

const requirementStats: (keyof ItemRequirement)[] = ["str", "smart", "agi", "au"]

// 能力需求比對裸點（baseStats），不含裝備加成；jobId 為 null 表示不限職業
export const requirementRows = (
  item: Pick<InventoryItem, "requires" | "jobId">,
  character: Pick<CharacterView, "baseStats" | "jobId">,
  jobNames: JobName[] = [],
): Requirement[] => {
  const rows: Requirement[] = requirementStats
    .filter((stat) => item.requires[stat] > 0)
    .map((stat) => ({
      text: `${statLabel(stat)} ≥ ${item.requires[stat]}`,
      met: character.baseStats[stat] >= item.requires[stat],
    }))
  if (item.jobId !== null) {
    const jobId = item.jobId
    const name = jobNames.find((job) => job.id === jobId)?.name ?? jobId
    rows.push({ text: `職業：${name}`, met: character.jobId === jobId })
  }
  return rows
}

export const meetsRequirements = (
  item: Pick<InventoryItem, "requires" | "jobId">,
  character: Pick<CharacterView, "baseStats" | "jobId">,
) => requirementRows(item, character).every((row) => row.met)

export const sellPrice = (price: number, quantity: number) =>
  Math.floor(price * SellRate * quantity)

// 補給欄預設放整疊，上限 99（背包頁與裝備頁共用）
export const supplyEquipQuantity = (item: Pick<InventoryItem, "quantity">) =>
  Math.min(MaxStackQuantity, item.quantity)

// 商店的裝備一次只能買一件
export const maxBuyQuantity = (category: ItemCategory) =>
  isEquipment(category) ? 1 : MaxStackQuantity

export const clampQuantity = (value: number, max: number) =>
  Number.isFinite(value) ? Math.min(Math.max(1, Math.trunc(value)), Math.max(1, max)) : 1

export const canAfford = (money: number, price: number, quantity: number) =>
  money >= price * quantity

export const isBankLocked = (level: number) => level < BankMinimumLevel

// 鍛造消耗的道具（與後端 ForgeTools 一致）
export const ForgeTools = {
  safeRefineStone: "safe-refine-stone",
  stoneRemover: "item-1378",
  holeDriller: "item-3361",
} as const

const toolNames: Record<string, string> = {
  [ForgeTools.safeRefineStone]: "安全精鍊石",
  [ForgeTools.stoneRemover]: "拆石器",
  [ForgeTools.holeDriller]: "鑽孔器",
}

const fixedReasonTexts: Record<string, string> = {
  alreadyEquipped: "這件道具已經裝備中",
  atRefineMax: "已達精鍊上限",
  cannotMerge: "這種精鍊石無法再合成",
  duplicateStone: "同一件裝備不能鑲嵌同種魔石",
  holeEmpty: "這個孔沒有魔石",
  holeOccupied: "這個孔已經鑲嵌了魔石",
  holeOutOfRange: "沒有這個孔位",
  maxHoles: "孔數已達上限",
  missingMaterials: "材料不足",
  notRefineStone: "這不是精鍊石",
  notStone: "這不是魔石",
  wrongStoneLevel: "精鍊石等級不符",
  bagFull: "背包已滿",
  depotFull: "倉庫已滿",
  equipped: "請先卸下這件裝備",
  invalidAmount: "金額不正確",
  invalidLevel: "商店等級不正確",
  invalidQuantity: "數量不正確",
  itemNotFound: "找不到這件道具",
  jobMismatch: "職業不符",
  levelTooLow: `Lv ${BankMinimumLevel} 才能存款`,
  noEffect: "使用後沒有效果",
  notEnoughBank: "存款不足",
  notEnoughMoney: "金錢不足",
  notEquipment: "這不是裝備",
  notForSale: "商店沒有販售這個品項",
  notUsable: "這個道具不能直接使用",
  petsFull: "牧場已滿，最多只能擁有3隻寵物",
  slotEmpty: "這一欄沒有裝備",
  stackLimit: "已達堆疊上限",
  unknownRoom: "沒有這個房間",
}

const StatTooLowPrefix = "statTooLow:"
const MissingToolPrefix = "missingTool:"

export const reasonText = (code: string) => {
  if (code.startsWith(StatTooLowPrefix))
    return `${statLabel(code.slice(StatTooLowPrefix.length))}不足`
  if (code.startsWith(MissingToolPrefix))
    return `缺少${toolNames[code.slice(MissingToolPrefix.length)] ?? "需要的道具"}`
  return fixedReasonTexts[code] ?? `無法完成（${code}）`
}

export const errorMessage = (detail: string, reasons: string[]) =>
  reasons.length === 0 ? detail : reasons.map(reasonText).join("、")
