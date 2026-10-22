import { statLabel } from "./stats"
import type {
  BagCategoryView,
  CharacterView,
  EquipmentSlot,
  InventoryItem,
  InventoryView,
  ItemCategory,
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
]

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

export const isSupply = (item: InventoryItem) => item.consumable === "Supply"

// 能從背包直接使用的消耗品；補給品要放到補給欄
export const isUsable = (item: InventoryItem) =>
  item.consumable === "StatPotion" ||
  item.consumable === "StaminaPotion" ||
  item.consumable === "BagExpansion" ||
  item.consumable === "DepotExpansion"

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

// 商店的裝備一次只能買一件
export const maxBuyQuantity = (category: ItemCategory) =>
  isEquipment(category) ? 1 : MaxStackQuantity

export const clampQuantity = (value: number, max: number) =>
  Number.isFinite(value) ? Math.min(Math.max(1, Math.trunc(value)), Math.max(1, max)) : 1

export const canAfford = (money: number, price: number, quantity: number) =>
  money >= price * quantity

export const isBankLocked = (level: number) => level < BankMinimumLevel

// 角色回應目前沒有存款欄位；後端補上 bank 後即會顯示
export const bankBalanceOf = (character: CharacterView): number | null =>
  "bank" in character && typeof character.bank === "number" ? character.bank : null

const fixedReasonTexts: Record<string, string> = {
  alreadyEquipped: "這件道具已經裝備中",
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
  slotEmpty: "這一欄沒有裝備",
  stackLimit: "已達堆疊上限",
}

const StatTooLowPrefix = "statTooLow:"

export const reasonText = (code: string) => {
  if (code.startsWith(StatTooLowPrefix))
    return `${statLabel(code.slice(StatTooLowPrefix.length))}不足`
  return fixedReasonTexts[code] ?? `無法完成（${code}）`
}

export const errorMessage = (detail: string, reasons: string[]) =>
  reasons.length === 0 ? detail : reasons.map(reasonText).join("、")
