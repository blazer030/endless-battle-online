import { describe, expect, it } from "vitest"
import { makeInventory, makeItem } from "../test/fixtures"
import {
  canDrill,
  forgeTargets,
  holeSlots,
  isAtRefineMax,
  maxSynthesisAttempts,
  mergeablePairs,
  mergeableStones,
  recipeText,
  refinePreview,
  refineResultText,
  refineStoneOptions,
  socketStoneOptions,
  synthesisButtonText,
  synthesisResultText,
  unsocketResultText,
  drillResultText,
  countInBag,
} from "./forge"
import type { Recipe } from "./types"

const sword = makeItem({ inventoryId: 1, name: "測試劍", refineLevel: 2, refineMax: 4 })
const helmet = makeItem({ inventoryId: 2, name: "皮帽", category: "Head" })
const stone = (inventoryId: number, level: number, quantity = 1) =>
  makeItem({
    inventoryId,
    itemId: `iron-refine-stone-lv${level}`,
    refineStoneLevel: level,
    name: `鋼鐵精鍊石 Lv${level}`,
    category: "RefineStone",
    quantity,
  })
const material = (inventoryId: number, itemId: string, name: string, quantity: number) =>
  makeItem({ inventoryId, itemId, name, category: "Material", quantity })
const attackStone = makeItem({
  inventoryId: 40,
  itemId: "attack-stone",
  name: "攻擊魔石",
  category: "Stone",
})
const guardStone = makeItem({
  inventoryId: 41,
  itemId: "guard-stone",
  name: "守護魔石",
  category: "Stone",
})

const recipe: Recipe = {
  id: "iron-ingot",
  resultItemId: "iron-ingot",
  resultName: "鐵錠",
  resultQuantity: 1,
  materials: [
    { itemId: "iron-ore", name: "鐵礦石", quantity: 3 },
    { itemId: "magic-dust", name: "魔法粉塵", quantity: 1 },
  ],
  successRate: 60,
  costPerAttempt: 1000,
}

describe("refine", () => {
  it("previews the next level with the 101 − 10n success rate", () => {
    expect(refinePreview(makeItem({ refineLevel: 0 }))).toBe("+0 → +1（成功率 91%）")
    expect(refinePreview(sword)).toBe("+2 → +3（成功率 71%）")
    expect(refinePreview(makeItem({ refineLevel: 9, refineMax: 10 }))).toBe("+9 → +10（成功率 1%）")
  })

  it("stops at the refine max", () => {
    expect(isAtRefineMax(sword)).toBe(false)
    expect(isAtRefineMax({ ...sword, refineLevel: 4 })).toBe(true)
  })

  it("enables only stones of the target level", () => {
    const inventory = makeInventory([sword, stone(30, 3), stone(31, 2)])
    expect(refineStoneOptions(inventory, sword)).toEqual([
      { stone: stone(30, 3), allowed: true },
      { stone: stone(31, 2), allowed: false },
    ])
  })

  it("narrates success and which stone was lost on failure", () => {
    expect(refineResultText(true, "測試劍", 3, "鋼鐵精鍊石 Lv3", false)).toBe("精鍊成功！測試劍 +3")
    expect(refineResultText(false, "測試劍", 3, "鋼鐵精鍊石 Lv3", false)).toBe(
      "精鍊失敗，鋼鐵精鍊石 Lv3消失了。",
    )
    expect(refineResultText(false, "測試劍", 3, "鋼鐵精鍊石 Lv3", true)).toBe(
      "精鍊失敗，安全石消失了。",
    )
  })

  it("merges two stones of a level into one of the next, up to Lv10", () => {
    expect(mergeablePairs(stone(30, 3, 5))).toBe(2)
    expect(mergeablePairs(stone(30, 3, 1))).toBe(0)
    expect(mergeablePairs(stone(30, 10, 6))).toBe(0)
    const inventory = makeInventory([stone(30, 3, 5), stone(31, 4, 1), stone(32, 10, 2)])
    expect(mergeableStones(inventory)).toEqual([stone(30, 3, 5)])
  })
})

describe("forgeTargets", () => {
  it("lists equipped gear first, then equipment in the bag", () => {
    const worn = makeItem({ inventoryId: 9, name: "精鋼劍" })
    const inventory = makeInventory([sword, helmet, stone(30, 3)], {
      equipment: [
        { slot: "Weapon", quantity: 1, item: worn },
        {
          slot: "Supply",
          quantity: 5,
          item: makeItem({ inventoryId: 10, category: "Consumable" }),
        },
      ],
    })
    expect(forgeTargets(inventory).map((item) => item.inventoryId)).toEqual([9, 1, 2])
  })
})

describe("socket", () => {
  const socketed = makeItem({
    inventoryId: 5,
    holes: 3,
    sockets: [
      { hole: 2, stoneId: "attack-stone", stoneName: "攻擊魔石", rolled: makeItem().refineBonus },
    ],
  })

  it("lays out every hole with its stone", () => {
    expect(holeSlots(socketed)).toEqual([
      { hole: 1, stoneName: null },
      { hole: 2, stoneName: "攻擊魔石" },
      { hole: 3, stoneName: null },
    ])
  })

  it("blocks a second stone of the same kind", () => {
    const inventory = makeInventory([attackStone, guardStone])
    expect(socketStoneOptions(inventory, socketed)).toEqual([
      { stone: attackStone, allowed: false },
      { stone: guardStone, allowed: true },
    ])
  })

  it("drills only below four holes", () => {
    expect(canDrill(socketed)).toBe(true)
    expect(canDrill({ ...socketed, holes: 4 })).toBe(false)
  })

  it("narrates removal and drilling", () => {
    expect(unsocketResultText(true, "攻擊魔石")).toBe("拆除成功，攻擊魔石回到背包。")
    expect(unsocketResultText(false, "攻擊魔石")).toBe("拆除後攻擊魔石消失了。")
    expect(drillResultText(true)).toBe("打洞成功！")
    expect(drillResultText(false)).toBe("打洞失敗。")
  })
})

describe("synthesis", () => {
  it("lists the result, materials and success rate", () => {
    expect(recipeText(recipe)).toBe("鐵錠 ×1 ← 鐵礦石 ×3、魔法粉塵 ×1（60%）")
  })

  it("counts materials across stacks", () => {
    const inventory = makeInventory([
      material(60, "iron-ore", "鐵礦石", 4),
      material(61, "iron-ore", "鐵礦石", 3),
    ])
    expect(countInBag(inventory, "iron-ore")).toBe(7)
  })

  it("allows as many attempts as the scarcest material covers, capped at 99", () => {
    const enough = makeInventory([
      material(60, "iron-ore", "鐵礦石", 7),
      material(62, "magic-dust", "魔法粉塵", 5),
    ])
    expect(maxSynthesisAttempts(recipe, enough)).toBe(2)
    expect(
      maxSynthesisAttempts(recipe, makeInventory([material(60, "iron-ore", "鐵礦石", 9)])),
    ).toBe(0)
    const plenty = makeInventory([
      material(60, "iron-ore", "鐵礦石", 99),
      material(61, "iron-ore", "鐵礦石", 99),
      material(62, "iron-ore", "鐵礦石", 99),
      material(63, "iron-ore", "鐵礦石", 99),
      material(64, "magic-dust", "魔法粉塵", 99),
      material(65, "magic-dust", "魔法粉塵", 99),
    ])
    expect(maxSynthesisAttempts(recipe, plenty)).toBe(99)
  })

  it("adds up a material listed more than once", () => {
    const doubled: Recipe = {
      ...recipe,
      materials: [
        { itemId: "iron-ore", name: "鐵礦石", quantity: 2 },
        { itemId: "iron-ore", name: "鐵礦石", quantity: 2 },
      ],
    }
    expect(
      maxSynthesisAttempts(doubled, makeInventory([material(60, "iron-ore", "鐵礦石", 9)])),
    ).toBe(2)
  })

  it("shows the money ceiling and the result", () => {
    expect(synthesisButtonText(3)).toBe("合成（3,000 金錢上限）")
    expect(synthesisResultText({ attempts: 3, successes: 2, cost: 2000 })).toBe(
      "合成 3 次，成功 2 次，花費 2,000 金錢。",
    )
  })
})
