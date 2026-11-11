import { expect, test } from "vitest"
import { ForgeResultVo, RecipeVo } from "../game/forge"
import { makeInventory, makeItem } from "../test/fixtures"
import {
  ForgeResultConverter,
  RecipeConverter,
  SynthesisConverter,
  UnsocketConverter,
} from "./forgeData"
import { ParseError } from "./parse"

const asDto = (value: unknown): Record<string, unknown> => JSON.parse(JSON.stringify(value))

const inventory = makeInventory([makeItem({ inventoryId: 7, refineLevel: 3 })])
const inventoryDto = asDto(inventory)

const parseFailure = (convert: () => unknown) => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("ForgeResultConverter_ConvertsTheWholeResponse", () => {
  const result = ForgeResultConverter.toVo({ success: true, inventory: inventoryDto })

  expect(result).toBeInstanceOf(ForgeResultVo)
  expect(result.success).toBe(true)
  expect(result.inventory).toEqual(inventory)
})

// 規格 Missing field in an embedded inventory names its path
test("ForgeResultConverter_MissingCapacityInTheInventoryNamesThePath", () => {
  const dto = asDto(inventoryDto)
  delete (dto.bag as Record<string, unknown>[])[0].capacity

  expect(
    parseFailure(() => ForgeResultConverter.toVo({ success: true, inventory: dto })).path,
  ).toBe("forge.inventory.bag[0].capacity")
})

test("ForgeResultConverter_IgnoresUnknownFields", () => {
  expect(
    ForgeResultConverter.toVo({ success: false, inventory: inventoryDto, sparks: 3 }),
  ).not.toHaveProperty("sparks")
})

test("UnsocketConverter_ConvertsTheWholeResponse", () => {
  const result = UnsocketConverter.toVo({ success: true, recovered: true, inventory: inventoryDto })

  expect([result.success, result.recovered]).toEqual([true, true])
  expect(result.inventory).toEqual(inventory)
})

test("UnsocketConverter_MissingRecoveredNamesThePath", () => {
  expect(
    parseFailure(() => UnsocketConverter.toVo({ success: true, inventory: inventoryDto })).path,
  ).toBe("unsocket.recovered")
})

test("UnsocketConverter_IgnoresUnknownFields", () => {
  expect(
    UnsocketConverter.toVo({ success: true, recovered: false, inventory: inventoryDto, dust: 1 }),
  ).not.toHaveProperty("dust")
})

const recipeDto = {
  id: "iron-ingot",
  resultItemId: "item-iron-ingot",
  resultName: "鐵錠",
  resultQuantity: 1,
  materials: [{ itemId: "iron-ore", name: "鐵礦", quantity: 3 }],
  successRate: 80,
  costPerAttempt: 100,
  requiredMission: { id: "smith-1", title: "鐵匠的請託", completed: false },
}

test("RecipeConverter_ConvertsTheWholeResponse", () => {
  const recipe = RecipeConverter.toVo(recipeDto)

  expect(recipe).toBeInstanceOf(RecipeVo)
  expect(recipe).toEqual(RecipeVo.create(recipeDto))
})

test("RecipeConverter_MissingMaterialQuantityNamesThePath", () => {
  const dto = { ...recipeDto, materials: [{ itemId: "iron-ore", name: "鐵礦" }] }

  expect(parseFailure(() => RecipeConverter.toVo(dto, "recipes[1]")).path).toBe(
    "recipes[1].materials[0].quantity",
  )
})

test("RecipeConverter_IgnoresUnknownFields", () => {
  expect(RecipeConverter.toVo({ ...recipeDto, hidden: true })).not.toHaveProperty("hidden")
})

test("SynthesisConverter_ConvertsTheWholeResponse", () => {
  const result = SynthesisConverter.toVo({
    successes: 4,
    attempts: 5,
    cost: 500,
    inventory: inventoryDto,
  })

  expect([result.successes, result.attempts, result.cost]).toEqual([4, 5, 500])
  expect(result.inventory).toEqual(inventory)
})

test("SynthesisConverter_MissingCostNamesThePath", () => {
  expect(
    parseFailure(() =>
      SynthesisConverter.toVo({ successes: 4, attempts: 5, inventory: inventoryDto }),
    ).path,
  ).toBe("synthesis.cost")
})

test("SynthesisConverter_IgnoresUnknownFields", () => {
  expect(
    SynthesisConverter.toVo({
      successes: 0,
      attempts: 1,
      cost: 100,
      inventory: inventoryDto,
      x: 1,
    }),
  ).not.toHaveProperty("x")
})
