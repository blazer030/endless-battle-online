import { formatNumber } from "./format"
import { bagItems, bagSlotsOf, isEquipment } from "./inventory"
import type { InventoryItem, InventoryVo } from "./inventory"

// 鐵舖的回應（來自 /api/forge/...），由 api/forgeData.ts 轉換；inventory 是操作後的背包
type ForgeResultVoParams = { success: boolean; inventory: InventoryVo }

export class ForgeResultVo {
  readonly success: boolean
  readonly inventory: InventoryVo

  private constructor(params: ForgeResultVoParams) {
    this.success = params.success
    this.inventory = params.inventory
  }

  static create(params: ForgeResultVoParams): ForgeResultVo {
    return new ForgeResultVo(params)
  }
}

// recovered 表示拆下的魔石回到背包
type UnsocketVoParams = { success: boolean; recovered: boolean; inventory: InventoryVo }

export class UnsocketVo {
  readonly success: boolean
  readonly recovered: boolean
  readonly inventory: InventoryVo

  private constructor(params: UnsocketVoParams) {
    this.success = params.success
    this.recovered = params.recovered
    this.inventory = params.inventory
  }

  static create(params: UnsocketVoParams): UnsocketVo {
    return new UnsocketVo(params)
  }
}

export type RecipeMaterial = {
  readonly itemId: string
  readonly name: string
  readonly quantity: number
}

// 需要先完成的任務；completed 為 false 時不能合成
export type RecipeRequiredMission = {
  readonly id: string
  readonly title: string
  readonly completed: boolean
}

type RecipeVoParams = {
  id: string
  resultItemId: string
  resultName: string
  resultQuantity: number
  materials: readonly RecipeMaterial[]
  successRate: number
  costPerAttempt: number
  requiredMission: RecipeRequiredMission | null
}

export class RecipeVo {
  readonly id: string
  readonly resultItemId: string
  readonly resultName: string
  readonly resultQuantity: number
  readonly materials: readonly RecipeMaterial[]
  readonly successRate: number
  readonly costPerAttempt: number
  readonly requiredMission: RecipeRequiredMission | null

  private constructor(params: RecipeVoParams) {
    this.id = params.id
    this.resultItemId = params.resultItemId
    this.resultName = params.resultName
    this.resultQuantity = params.resultQuantity
    this.materials = params.materials
    this.successRate = params.successRate
    this.costPerAttempt = params.costPerAttempt
    this.requiredMission = params.requiredMission
  }

  static create(params: RecipeVoParams): RecipeVo {
    return new RecipeVo(params)
  }
}

type SynthesisVoParams = {
  successes: number
  attempts: number
  cost: number
  inventory: InventoryVo
}

export class SynthesisVo {
  readonly successes: number
  readonly attempts: number
  readonly cost: number
  readonly inventory: InventoryVo

  private constructor(params: SynthesisVoParams) {
    this.successes = params.successes
    this.attempts = params.attempts
    this.cost = params.cost
    this.inventory = params.inventory
  }

  static create(params: SynthesisVoParams): SynthesisVo {
    return new SynthesisVo(params)
  }
}

// 與後端 Socket／StoneMerge／Synthesis 的常數一致
export const MaxHoles = 4
export const DrillSuccessPercent = 8
export const MaxRefineStoneLevel = 10
export const MaxSynthesisAttempts = 99
const StonesPerPair = 2

export type ForgeOption = { stone: InventoryItem; allowed: boolean }

// 鍛造對象：先列裝備欄（補給欄除外），再列背包裡的裝備
export const forgeTargets = (inventory: InventoryVo): InventoryItem[] => [
  ...inventory.equipment.filter(({ item }) => isEquipment(item.category)).map(({ item }) => item),
  ...bagItems(inventory).filter((item) => isEquipment(item.category)),
]

export const countInBag = (inventory: InventoryVo, itemId: string) =>
  bagItems(inventory)
    .filter((item) => item.itemId === itemId)
    .reduce((total, item) => total + item.quantity, 0)

export const hasTool = (inventory: InventoryVo, toolId: string) => countInBag(inventory, toolId) > 0

// 目標等級 n = 目前 + 1，成功率 101 − 10n %
const refineSuccessPercent = (item: InventoryItem) => 101 - 10 * (item.refineLevel + 1)

export const refinePreview = (item: InventoryItem) =>
  `+${item.refineLevel} → +${item.refineLevel + 1}（成功率 ${refineSuccessPercent(item)}%）`

export const isAtRefineMax = (item: InventoryItem) => item.refineLevel >= item.refineMax

// 精鍊石等級必須等於目標等級（目前 + 1）
export const refineStoneOptions = (inventory: InventoryVo, target: InventoryItem): ForgeOption[] =>
  bagSlotsOf(inventory, "RefineStone").items.map((stone) => ({
    stone,
    allowed: stone.refineStoneLevel === target.refineLevel + 1,
  }))

export const refineResultText = (
  success: boolean,
  equipmentName: string,
  newLevel: number,
  stoneName: string,
  usedSafeStone: boolean,
) => {
  if (success) return `精鍊成功！${equipmentName} +${newLevel}`
  return `精鍊失敗，${usedSafeStone ? "安全石" : stoneName}消失了。`
}

// 同一格兩顆合一顆 +1 級；LV10 不能再合
export const mergeablePairs = (stone: InventoryItem) => {
  const level = stone.refineStoneLevel
  if (level === null || level >= MaxRefineStoneLevel) return 0
  return Math.floor(stone.quantity / StonesPerPair)
}

export const mergeableStones = (inventory: InventoryVo) =>
  bagSlotsOf(inventory, "RefineStone").items.filter((stone) => mergeablePairs(stone) > 0)

// 異類合成：同等級、不同種類（每種等級各是一個道具 id）；Lv1 不能異類合成
export const crossMergePartners = (inventory: InventoryVo, first: InventoryItem) => {
  const level = first.refineStoneLevel
  if (level === null || level <= 1) return []
  return bagSlotsOf(inventory, "RefineStone").items.filter(
    (stone) => stone.refineStoneLevel === level && stone.itemId !== first.itemId,
  )
}

export const crossMergeableStones = (inventory: InventoryVo) =>
  bagSlotsOf(inventory, "RefineStone").items.filter(
    (stone) => crossMergePartners(inventory, stone).length > 0,
  )

// 每組兩格各扣一顆
export const crossMergeMaxPairs = (first: InventoryItem, second: InventoryItem) =>
  Math.min(first.quantity, second.quantity)

export type HoleSlot = { hole: number; stoneName: string | null }

// 孔位從 1 起算
export const holeSlots = (item: InventoryItem): HoleSlot[] =>
  Array.from({ length: Math.min(item.holes, MaxHoles) }, (_, index) => {
    const hole = index + 1
    const socket = item.sockets.find((candidate) => candidate.hole === hole)
    return { hole, stoneName: socket?.stoneName ?? null }
  })

// 同一件裝備不能鑲嵌同種魔石（魔石種類 = 道具 id）
export const socketStoneOptions = (inventory: InventoryVo, target: InventoryItem): ForgeOption[] =>
  bagSlotsOf(inventory, "Stone").items.map((stone) => ({
    stone,
    allowed: !target.sockets.some((socket) => socket.stoneId === stone.itemId),
  }))

export const canDrill = (item: InventoryItem) => item.holes < MaxHoles

export const unsocketResultText = (recovered: boolean, stoneName: string) =>
  recovered ? `拆除成功，${stoneName}回到背包。` : `拆除後${stoneName}消失了。`

export const drillResultText = (success: boolean) => (success ? "打洞成功！" : "打洞失敗。")

export const recipeText = (recipe: RecipeVo) => {
  const materials = recipe.materials
    .map((material) => `${material.name} ×${material.quantity}`)
    .join("、")
  return `${recipe.resultName} ×${recipe.resultQuantity} ← ${materials}（${recipe.successRate}%）`
}

// 材料不論成敗都會用掉，次數上限取最缺的材料能支撐幾次；同種材料出現多次時合併計算
export const maxSynthesisAttempts = (recipe: RecipeVo, inventory: InventoryVo) => {
  const required = new Map<string, number>()
  for (const material of recipe.materials)
    required.set(material.itemId, (required.get(material.itemId) ?? 0) + material.quantity)
  return Math.min(
    MaxSynthesisAttempts,
    ...[...required].map(([itemId, quantity]) =>
      Math.floor(countInBag(inventory, itemId) / quantity),
    ),
  )
}

// 只有成功才扣錢，按鈕顯示全部成功時的花費上限
export const synthesisButtonText = (costPerAttempt: number, attempts: number) =>
  `合成（${formatNumber(costPerAttempt * attempts)} 金錢上限）`

export const synthesisResultText = (result: Pick<SynthesisVo, "attempts" | "successes" | "cost">) =>
  `合成 ${result.attempts} 次，成功 ${result.successes} 次，花費 ${formatNumber(result.cost)} 金錢。`
