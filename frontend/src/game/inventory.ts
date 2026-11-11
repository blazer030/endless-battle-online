import { formatNumber } from "./format"
import { buffLabel, durationMinutes, type Buff } from "./buffs"
import { statLabel } from "./stats"
import type { CharacterVo } from "./character"

// 背包、商店、倉庫與道具使用的資料（來自 /api/inventory、/api/shop、/api/depot），由 api/inventoryData.ts 轉換
export const itemCategoryIds = [
  "Weapon",
  "Head",
  "Body",
  "Hand",
  "Foot",
  "Accessory",
  "Consumable",
  "Stone",
  "RefineStone",
  "Material",
  "Medal",
  "Key",
] as const
export type ItemCategory = (typeof itemCategoryIds)[number]

export const consumableKinds = [
  "None",
  "Supply",
  "StatPotion",
  "BagExpansion",
  "DepotExpansion",
  "Capture",
  "StoneBox",
  "RefineBox",
  "PetEgg",
  "ProficiencyBook",
  "ExperienceCapsule",
  "Elixir",
] as const
export type ConsumableKind = (typeof consumableKinds)[number]

export const equipmentSlotIds = [
  "Weapon",
  "Head",
  "Body",
  "Hand",
  "Foot",
  "Accessory",
  "Supply",
] as const
export type EquipmentSlot = (typeof equipmentSlotIds)[number]

export type StatBonus = {
  readonly strength: number
  readonly intelligence: number
  readonly life: number
  readonly vitality: number
  readonly agility: number
  readonly charisma: number
  readonly faith: number
  readonly attack: number
  readonly magicAttack: number
  readonly defense: number
  readonly magicDefense: number
  readonly maxHealthPoints: number
}

// 裝備需求只比對這四項裸點
export type EquipmentRequirement = {
  readonly strength: number
  readonly intelligence: number
  readonly agility: number
  readonly charisma: number
}

export type Socket = {
  readonly hole: number
  readonly stoneId: string
  readonly stoneName: string
  readonly rolled: StatBonus
}

export type WeaponSkill = { readonly name: string; readonly level: number; readonly rate: number }

// refineStoneLevel 只有精鍊石才有值；jobId 為 null 表示不限職業
export type InventoryItem = {
  readonly inventoryId: number
  readonly itemId: string
  readonly name: string
  readonly category: ItemCategory
  readonly consumable: ConsumableKind
  readonly quantity: number
  readonly bonus: StatBonus
  readonly requires: EquipmentRequirement
  readonly jobId: string | null
  readonly supplyHealthPoints: number
  readonly supplyManaPoints: number
  readonly amount: number
  readonly refineLevel: number
  readonly refineMax: number
  readonly refineBonus: StatBonus
  readonly holes: number
  readonly sockets: readonly Socket[]
  readonly price: number
  readonly bound: boolean
  readonly refineStoneLevel: number | null
  readonly enhanced: boolean
  readonly weaponSkill: WeaponSkill | null
}

export type BagCategory = {
  readonly category: ItemCategory
  readonly capacity: number
  readonly items: readonly InventoryItem[]
}

export type EquippedItem = {
  readonly slot: EquipmentSlot
  readonly quantity: number
  readonly item: InventoryItem
}

type InventoryVoParams = {
  bag: readonly BagCategory[]
  equipment: readonly EquippedItem[]
  bonus: StatBonus
}

export class InventoryVo {
  readonly bag: readonly BagCategory[]
  readonly equipment: readonly EquippedItem[]
  readonly bonus: StatBonus

  private constructor(params: InventoryVoParams) {
    this.bag = params.bag
    this.equipment = params.equipment
    this.bonus = params.bonus
  }

  static create(params: InventoryVoParams): InventoryVo {
    return new InventoryVo(params)
  }
}

type ShopItemVoParams = {
  id: string
  name: string
  category: ItemCategory
  price: number
  bonus: StatBonus
  requires: EquipmentRequirement
  holes: number
  refineMax: number
}

export class ShopItemVo {
  readonly id: string
  readonly name: string
  readonly category: ItemCategory
  readonly price: number
  readonly bonus: StatBonus
  readonly requires: EquipmentRequirement
  readonly holes: number
  readonly refineMax: number

  private constructor(params: ShopItemVoParams) {
    this.id = params.id
    this.name = params.name
    this.category = params.category
    this.price = params.price
    this.bonus = params.bonus
    this.requires = params.requires
    this.holes = params.holes
    this.refineMax = params.refineMax
  }

  static create(params: ShopItemVoParams): ShopItemVo {
    return new ShopItemVo(params)
  }
}

// 倉庫的一筆的 inventoryId 是倉庫的 id（取出時用）
type DepotVoParams = { entries: readonly InventoryItem[]; capacity: number }

export class DepotVo {
  readonly entries: readonly InventoryItem[]
  readonly capacity: number

  private constructor(params: DepotVoParams) {
    this.entries = params.entries
    this.capacity = params.capacity
  }

  static create(params: DepotVoParams): DepotVo {
    return new DepotVo(params)
  }
}

type DepotMoveVoParams = { inventory: InventoryVo; depot: DepotVo }

export class DepotMoveVo {
  readonly inventory: InventoryVo
  readonly depot: DepotVo

  private constructor(params: DepotMoveVoParams) {
    this.inventory = params.inventory
    this.depot = params.depot
  }

  static create(params: DepotMoveVoParams): DepotMoveVo {
    return new DepotMoveVo(params)
  }
}

// obtained 是寶箱抽到的道具名稱；petName 是孵出的寵物；buff 是喝下的密藥
type ItemUseVoParams = {
  inventory: InventoryVo
  obtained: readonly string[]
  petName: string | null
  skillPoints: number
  levelsGained: number
  buff: Buff | null
}

export class ItemUseVo {
  readonly inventory: InventoryVo
  readonly obtained: readonly string[]
  readonly petName: string | null
  readonly skillPoints: number
  readonly levelsGained: number
  readonly buff: Buff | null

  private constructor(params: ItemUseVoParams) {
    this.inventory = params.inventory
    this.obtained = params.obtained
    this.petName = params.petName
    this.skillPoints = params.skillPoints
    this.levelsGained = params.levelsGained
    this.buff = params.buff
  }

  static create(params: ItemUseVoParams): ItemUseVo {
    return new ItemUseVo(params)
  }
}

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
  result: Pick<ItemUseVo, "obtained" | "petName" | "skillPoints" | "levelsGained" | "buff">,
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

export const bagSlotsOf = (
  inventory: Pick<InventoryVo, "bag">,
  category: ItemCategory,
): BagCategory =>
  inventory.bag.find((entry) => entry.category === category) ?? {
    category,
    capacity: 0,
    items: [],
  }

// 每一列佔一格：裝備一件一格，可堆疊的一疊一格
export const usageText = (slots: BagCategory) => `${slots.items.length}/${slots.capacity}`

export const bagItems = (inventory: Pick<InventoryVo, "bag">) =>
  itemCategories.flatMap(({ id }) => bagSlotsOf(inventory, id).items)

export const itemTitle = (item: InventoryItem) =>
  isEquipment(item.category)
    ? `${item.name} +${item.refineLevel}`
    : `${item.name} ×${item.quantity}`

export const holeMarks = (item: InventoryItem) =>
  "◆".repeat(item.sockets.length) + "◇".repeat(Math.max(0, item.holes - item.sockets.length))

const bonusLabels: { key: keyof StatBonus; label: string }[] = [
  { key: "attack", label: "物攻" },
  { key: "magicAttack", label: "魔攻" },
  { key: "defense", label: "物防" },
  { key: "magicDefense", label: "魔防" },
  { key: "maxHealthPoints", label: "HP 上限" },
  { key: "strength", label: "力量" },
  { key: "intelligence", label: "智力" },
  { key: "life", label: "生命" },
  { key: "vitality", label: "體質" },
  { key: "agility", label: "敏捷" },
  { key: "charisma", label: "魅力" },
  { key: "faith", label: "信仰" },
]

export const bonusText = (bonus: StatBonus) =>
  bonusLabels
    .filter(({ key }) => bonus[key] !== 0)
    .map(({ key, label }) => `${label} +${bonus[key]}`)
    .join("、")

export const supplyText = (item: Pick<InventoryItem, "supplyHealthPoints" | "supplyManaPoints">) =>
  `HP +${item.supplyHealthPoints}、SP +${item.supplyManaPoints}`

export const candidatesFor = (inventory: Pick<InventoryVo, "bag">, slot: EquipmentSlot) =>
  slot === "Supply"
    ? bagSlotsOf(inventory, "Consumable").items.filter(isSupply)
    : bagSlotsOf(inventory, slot).items

export const equippedIn = (inventory: Pick<InventoryVo, "equipment">, slot: EquipmentSlot) =>
  inventory.equipment.find((entry) => entry.slot === slot) ?? null

export type Requirement = { text: string; met: boolean }

export type JobName = { id: string; name: string }

const requirementStats: (keyof EquipmentRequirement)[] = [
  "strength",
  "intelligence",
  "agility",
  "charisma",
]

// 能力需求比對裸點（baseStats），不含裝備加成；jobId 為 null 表示不限職業
export const requirementRows = (
  item: Pick<InventoryItem, "requires" | "jobId">,
  character: Pick<CharacterVo, "baseStats" | "jobId">,
  jobNames: readonly JobName[] = [],
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
  character: Pick<CharacterVo, "baseStats" | "jobId">,
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
  missionIncomplete: "需先完成前置任務",
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
