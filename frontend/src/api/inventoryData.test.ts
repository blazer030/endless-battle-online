import { expect, test } from "vitest"
import { InventoryVo } from "../game/inventory"
import { makeInventory, makeItem, makeShopItem } from "../test/fixtures"
import {
  DepotConverter,
  DepotMoveConverter,
  InventoryConverter,
  ItemUseConverter,
  ShopItemConverter,
} from "./inventoryData"
import { ParseError } from "./parse"

// 回應本體就是 JSON：Vo 的欄位名稱與 API 相同，序列化後當成 DTO
const asDto = (value: unknown): Record<string, unknown> => JSON.parse(JSON.stringify(value))

const sword = makeItem({
  inventoryId: 7,
  refineLevel: 2,
  holes: 2,
  sockets: [
    {
      hole: 1,
      stoneId: "attack-stone",
      stoneName: "攻擊魔石",
      rolled: { ...makeItem().refineBonus, attack: 5 },
    },
  ],
  weaponSkill: { name: "爆氣", level: 2, rate: 15 },
})
const supply = makeItem({
  inventoryId: 8,
  itemId: "test-supply",
  name: "測試補給藥劑",
  category: "Consumable",
  consumable: "Supply",
  quantity: 20,
  supplyHealthPoints: 50,
  supplyManaPoints: 25,
})
const inventory = makeInventory([sword, supply], {
  equipment: [{ slot: "Supply", quantity: 5, item: supply }],
})
const inventoryDto = asDto(inventory)
const depotDto = asDto({ entries: [makeItem({ inventoryId: 11 })], capacity: 30 })

const parseFailure = (convert: () => unknown) => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("InventoryConverter_ConvertsTheWholeResponse", () => {
  const converted = InventoryConverter.toVo(inventoryDto)

  expect(converted).toBeInstanceOf(InventoryVo)
  expect(converted).toEqual(inventory)
})

test("InventoryConverter_MissingSupplyAmountNamesThePath", () => {
  const dto = asDto(inventoryDto)
  const consumables = (dto.bag as { category: string; items: Record<string, unknown>[] }[]).find(
    (entry) => entry.category === "Consumable",
  )!
  delete consumables.items[0].supplyHealthPoints

  expect(parseFailure(() => InventoryConverter.toVo(dto)).path).toBe(
    "inventory.bag[6].items[0].supplyHealthPoints",
  )
})

test("InventoryConverter_RejectsAnUnknownCategory", () => {
  const dto = asDto(inventoryDto)
  ;(dto.bag as Record<string, unknown>[])[0].category = "Ring"

  expect(parseFailure(() => InventoryConverter.toVo(dto)).path).toBe("inventory.bag[0].category")
})

test("InventoryConverter_IgnoresUnknownFields", () => {
  const converted = InventoryConverter.toVo({ ...inventoryDto, weight: 12 })

  expect(converted).not.toHaveProperty("weight")
})

test("ShopItemConverter_ConvertsTheWholeResponse", () => {
  const item = makeShopItem({
    requires: { strength: 10, intelligence: 0, agility: 3, charisma: 0 },
  })

  expect(ShopItemConverter.toVo(asDto(item))).toEqual(item)
})

test("ShopItemConverter_MissingRequirementNamesThePath", () => {
  const dto = asDto(makeShopItem())
  delete (dto.requires as Record<string, unknown>).agility

  expect(parseFailure(() => ShopItemConverter.toVo(dto, "shop[2]")).path).toBe(
    "shop[2].requires.agility",
  )
})

test("ShopItemConverter_IgnoresUnknownFields", () => {
  expect(ShopItemConverter.toVo({ ...asDto(makeShopItem()), stock: 3 })).not.toHaveProperty("stock")
})

test("DepotConverter_ConvertsTheWholeResponse", () => {
  const depot = DepotConverter.toVo(depotDto)

  expect(depot.capacity).toBe(30)
  expect(depot.entries.map((entry) => entry.inventoryId)).toEqual([11])
})

test("DepotConverter_MissingCapacityNamesThePath", () => {
  const dto = asDto(depotDto)
  delete dto.capacity

  expect(parseFailure(() => DepotConverter.toVo(dto)).path).toBe("depot.capacity")
})

test("DepotConverter_IgnoresUnknownFields", () => {
  expect(DepotConverter.toVo({ ...depotDto, fee: 10 })).not.toHaveProperty("fee")
})

test("DepotMoveConverter_ConvertsBothSides", () => {
  const moved = DepotMoveConverter.toVo({ inventory: inventoryDto, depot: depotDto })

  expect(moved.inventory).toEqual(inventory)
  expect(moved.depot.capacity).toBe(30)
})

test("DepotMoveConverter_MissingDepotNamesThePath", () => {
  expect(parseFailure(() => DepotMoveConverter.toVo({ inventory: inventoryDto })).path).toBe(
    "depotMove.depot",
  )
})

test("DepotMoveConverter_IgnoresUnknownFields", () => {
  expect(
    DepotMoveConverter.toVo({ inventory: inventoryDto, depot: depotDto, note: "ok" }),
  ).not.toHaveProperty("note")
})

const useDto = {
  inventory: inventoryDto,
  obtained: ["攻擊魔石"],
  petName: null,
  skillPoints: 0,
  levelsGained: 0,
  buff: { kind: "Experience", multiplier: 1.5, endsAt: "2026-11-12T08:30:00Z" },
}

test("ItemUseConverter_ConvertsTheBuffEndToADate", () => {
  const used = ItemUseConverter.toVo(useDto)

  expect(used.obtained).toEqual(["攻擊魔石"])
  expect(used.buff).toEqual({
    kind: "Experience",
    multiplier: 1.5,
    endsAt: new Date(Date.UTC(2026, 10, 12, 8, 30)),
  })
})

test("ItemUseConverter_KeepsANoBuffResultAsNull", () => {
  expect(ItemUseConverter.toVo({ ...useDto, buff: null }).buff).toBeNull()
})

test("ItemUseConverter_MissingInventoryFieldNamesThePath", () => {
  const dto = { ...useDto, inventory: { ...inventoryDto, bonus: undefined } }

  expect(parseFailure(() => ItemUseConverter.toVo(dto)).path).toBe("itemUse.inventory.bonus")
})

test("ItemUseConverter_IgnoresUnknownFields", () => {
  expect(ItemUseConverter.toVo({ ...useDto, sound: "pop" })).not.toHaveProperty("sound")
})
