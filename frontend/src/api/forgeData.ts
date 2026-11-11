import {
  ForgeResultVo,
  RecipeVo,
  SynthesisVo,
  UnsocketVo,
  type RecipeMaterial,
  type RecipeRequiredMission,
} from "../game/forge"
import { InventoryConverter } from "./inventoryData"
import { parseArray, parseBoolean, parseNumber, parseObject, parseString } from "./parse"

export const ForgeResultConverter = {
  toVo(value: unknown, path = "forge"): ForgeResultVo {
    const dto = parseObject(value, path)
    return ForgeResultVo.create({
      success: parseBoolean(dto.success, `${path}.success`),
      inventory: InventoryConverter.toVo(dto.inventory, `${path}.inventory`),
    })
  },
}

export const UnsocketConverter = {
  toVo(value: unknown, path = "unsocket"): UnsocketVo {
    const dto = parseObject(value, path)
    return UnsocketVo.create({
      success: parseBoolean(dto.success, `${path}.success`),
      recovered: parseBoolean(dto.recovered, `${path}.recovered`),
      inventory: InventoryConverter.toVo(dto.inventory, `${path}.inventory`),
    })
  },
}

const toMaterial = (value: unknown, path: string): RecipeMaterial => {
  const dto = parseObject(value, path)
  return {
    itemId: parseString(dto.itemId, `${path}.itemId`),
    name: parseString(dto.name, `${path}.name`),
    quantity: parseNumber(dto.quantity, `${path}.quantity`),
  }
}

const toRequiredMission = (value: unknown, path: string): RecipeRequiredMission | null => {
  if (value === null) return null
  const dto = parseObject(value, path)
  return {
    id: parseString(dto.id, `${path}.id`),
    title: parseString(dto.title, `${path}.title`),
    completed: parseBoolean(dto.completed, `${path}.completed`),
  }
}

export const RecipeConverter = {
  toVo(value: unknown, path = "recipes"): RecipeVo {
    const dto = parseObject(value, path)
    return RecipeVo.create({
      id: parseString(dto.id, `${path}.id`),
      resultItemId: parseString(dto.resultItemId, `${path}.resultItemId`),
      resultName: parseString(dto.resultName, `${path}.resultName`),
      resultQuantity: parseNumber(dto.resultQuantity, `${path}.resultQuantity`),
      materials: parseArray(dto.materials, `${path}.materials`, toMaterial),
      successRate: parseNumber(dto.successRate, `${path}.successRate`),
      costPerAttempt: parseNumber(dto.costPerAttempt, `${path}.costPerAttempt`),
      requiredMission: toRequiredMission(dto.requiredMission, `${path}.requiredMission`),
    })
  },
}

export const SynthesisConverter = {
  toVo(value: unknown, path = "synthesis"): SynthesisVo {
    const dto = parseObject(value, path)
    return SynthesisVo.create({
      successes: parseNumber(dto.successes, `${path}.successes`),
      attempts: parseNumber(dto.attempts, `${path}.attempts`),
      cost: parseNumber(dto.cost, `${path}.cost`),
      inventory: InventoryConverter.toVo(dto.inventory, `${path}.inventory`),
    })
  },
}
