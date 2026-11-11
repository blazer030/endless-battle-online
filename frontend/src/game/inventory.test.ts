import { describe, expect, it } from "vitest"
import { makeCharacter, makeInventory, makeItem } from "../test/fixtures"
import type { InventoryItem } from "./types"
import {
  bagSlotsOf,
  BankMinimumLevel,
  candidatesFor,
  categoryLabel,
  equipmentSlots,
  errorMessage,
  holeMarks,
  isBankLocked,
  itemCategories,
  itemTitle,
  maxBuyQuantity,
  meetsRequirements,
  reasonText,
  requirementRows,
  sellPrice,
  shopCategories,
  supplyEquipQuantity,
  usageText,
  isUsable,
  itemUseResultLines,
} from "./inventory"

describe("shopCategories", () => {
  it("leaves out medals and keys, which no shop sells", () => {
    expect(shopCategories.map(({ id }) => id)).toEqual(
      itemCategories.map(({ id }) => id).filter((id) => id !== "Medal" && id !== "Key"),
    )
  })
})

describe("itemCategories", () => {
  it("lists the twelve categories in bag order with their labels", () => {
    expect(itemCategories.map(({ id, label }) => `${id}:${label}`)).toEqual([
      "Weapon:武器",
      "Head:頭部",
      "Body:身體",
      "Hand:手部",
      "Foot:腳部",
      "Accessory:飾品",
      "Consumable:道具",
      "Stone:魔石",
      "RefineStone:精鍊石",
      "Material:材料",
      "Medal:勳章",
      "Key:鑰匙",
    ])
  })

  it("labels a category", () => {
    expect(categoryLabel("RefineStone")).toBe("精鍊石")
  })
})

describe("equipmentSlots", () => {
  it("lists six equipment slots and the supply slot", () => {
    expect(equipmentSlots.map(({ id, label }) => `${id}:${label}`)).toEqual([
      "Weapon:武器",
      "Head:頭部",
      "Body:身體",
      "Hand:手部",
      "Foot:腳部",
      "Accessory:飾品",
      "Supply:補給",
    ])
  })
})

describe("bagSlotsOf", () => {
  it("returns the items and capacity of a category", () => {
    const potion = makeItem({ inventoryId: 2, category: "Consumable", name: "小藥水" })
    const inventory = makeInventory([makeItem(), potion])

    const slots = bagSlotsOf(inventory, "Consumable")

    expect(slots.items).toEqual([potion])
    expect(slots.capacity).toBe(15)
    expect(usageText(slots)).toBe("1/15")
  })

  it("treats a category missing from the response as empty", () => {
    const slots = bagSlotsOf(makeInventory([], { bag: [] }), "Material")

    expect(slots).toEqual({ category: "Material", capacity: 0, items: [] })
    expect(usageText(slots)).toBe("0/0")
  })
})

describe("itemTitle", () => {
  it("shows the quantity of a stackable item", () => {
    expect(itemTitle(makeItem({ category: "Consumable", name: "小藥水", quantity: 12 }))).toBe(
      "小藥水 ×12",
    )
  })

  it("shows the refine level of equipment", () => {
    expect(itemTitle(makeItem({ name: "鐵劍", refineLevel: 3 }))).toBe("鐵劍 +3")
    expect(itemTitle(makeItem({ name: "鐵劍" }))).toBe("鐵劍 +0")
  })
})

describe("holeMarks", () => {
  it("draws filled holes before empty ones", () => {
    const item = makeItem({
      holes: 3,
      sockets: [{ hole: 1, stoneId: "ruby", stoneName: "紅寶石", rolled: makeItem().refineBonus }],
    })
    expect(holeMarks(item)).toBe("◆◇◇")
  })

  it("is empty without holes", () => {
    expect(holeMarks(makeItem())).toBe("")
  })
})

describe("candidatesFor", () => {
  const sword = makeItem({ inventoryId: 1 })
  const helmet = makeItem({ inventoryId: 2, category: "Head", name: "皮帽" })
  const supply = makeItem({
    inventoryId: 3,
    category: "Consumable",
    consumable: "Supply",
    name: "小藥水",
  })
  const statPotion = makeItem({
    inventoryId: 4,
    category: "Consumable",
    consumable: "StatPotion",
    name: "力量藥水",
  })
  const net = makeItem({
    inventoryId: 5,
    category: "Consumable",
    consumable: "Capture",
    name: "捕獸網",
    amount: 1,
  })
  const inventory = makeInventory([sword, helmet, supply, statPotion, net])

  it("lists bag items of the slot's category", () => {
    expect(candidatesFor(inventory, "Head")).toEqual([helmet])
  })

  it("lists supplies and capture items for the supply slot", () => {
    expect(candidatesFor(inventory, "Supply")).toEqual([supply, net])
  })
})

describe("requirementRows", () => {
  const character = makeCharacter({
    jobId: "warrior",
    baseStats: {
      strength: 20,
      intelligence: 5,
      life: 10,
      vitality: 10,
      agility: 12,
      charisma: 10,
      faith: 10,
    },
    // 有效數值含裝備加成，需求只比對裸點
    stats: { ...makeCharacter().stats, intelligence: 50 },
  })

  it("compares bare stats with each nonzero requirement", () => {
    const item = makeItem({ requires: { strength: 20, intelligence: 18, agility: 0, charisma: 0 } })

    expect(requirementRows(item, character)).toEqual([
      { text: "力量 ≥ 20", met: true },
      { text: "智力 ≥ 18", met: false },
    ])
  })

  it("adds the job requirement", () => {
    const jobNames = [{ id: "mage", name: "法師" }]
    const item = makeItem({ jobId: "mage" })

    expect(requirementRows(item, character, jobNames)).toEqual([{ text: "職業：法師", met: false }])
    expect(requirementRows(item, { ...character, jobId: "mage" }, jobNames)).toEqual([
      { text: "職業：法師", met: true },
    ])
  })

  it("falls back to the job id without names", () => {
    expect(requirementRows(makeItem({ jobId: "mage" }), character)).toEqual([
      { text: "職業：mage", met: false },
    ])
  })

  it("meetsRequirements only when every row is met", () => {
    expect(meetsRequirements(makeItem(), character)).toBe(true)
    expect(
      meetsRequirements(
        makeItem({ requires: { strength: 21, intelligence: 0, agility: 0, charisma: 0 } }),
        character,
      ),
    ).toBe(false)
    expect(meetsRequirements(makeItem({ jobId: "mage" }), character)).toBe(false)
  })
})

describe("sellPrice", () => {
  it.each([
    [200, 1, 100],
    [25, 1, 12],
    [25, 3, 37],
    [1, 1, 0],
  ])("floors price %i × 0.5 × %i to %i", (price, quantity, expected) => {
    expect(sellPrice(price, quantity)).toBe(expected)
  })
})

describe("supplyEquipQuantity", () => {
  it("puts the whole stack on the supply slot, capped at 99", () => {
    expect(supplyEquipQuantity(makeItem({ quantity: 30 }))).toBe(30)
    expect(supplyEquipQuantity(makeItem({ quantity: 99 }))).toBe(99)
    expect(supplyEquipQuantity(makeItem({ quantity: 120 }))).toBe(99)
  })
})

describe("maxBuyQuantity", () => {
  it("allows one piece of equipment and 99 of anything else", () => {
    expect(maxBuyQuantity("Weapon")).toBe(1)
    expect(maxBuyQuantity("Accessory")).toBe(1)
    expect(maxBuyQuantity("Consumable")).toBe(99)
    expect(maxBuyQuantity("Material")).toBe(99)
  })
})

describe("reasonText", () => {
  it.each([
    ["alreadyEquipped", "這件道具已經裝備中"],
    ["bagFull", "背包已滿"],
    ["depotFull", "倉庫已滿"],
    ["equipped", "請先卸下這件裝備"],
    ["invalidAmount", "金額不正確"],
    ["invalidLevel", "商店等級不正確"],
    ["invalidQuantity", "數量不正確"],
    ["itemNotFound", "找不到這件道具"],
    ["jobMismatch", "職業不符"],
    ["levelTooLow", "Lv 15 才能存款"],
    ["noEffect", "使用後沒有效果"],
    ["notEnoughBank", "存款不足"],
    ["notEnoughMoney", "金錢不足"],
    ["notEquipment", "這不是裝備"],
    ["notForSale", "商店沒有販售這個品項"],
    ["notUsable", "這個道具不能直接使用"],
    ["slotEmpty", "這一欄沒有裝備"],
    ["stackLimit", "已達堆疊上限"],
    ["statTooLow:strength", "力量不足"],
    ["statTooLow:intelligence", "智力不足"],
    ["statTooLow:agility", "敏捷不足"],
    ["statTooLow:charisma", "魅力不足"],
    ["statTooLow:life", "生命不足"],
    ["statTooLow:vitality", "體質不足"],
    ["statTooLow:faith", "信仰不足"],
    ["wrongStoneLevel", "精鍊石等級不符"],
    ["atRefineMax", "已達精鍊上限"],
    ["notRefineStone", "這不是精鍊石"],
    ["holeOutOfRange", "沒有這個孔位"],
    ["holeOccupied", "這個孔已經鑲嵌了魔石"],
    ["duplicateStone", "同一件裝備不能鑲嵌同種魔石"],
    ["notStone", "這不是魔石"],
    ["holeEmpty", "這個孔沒有魔石"],
    ["maxHoles", "孔數已達上限"],
    ["missingTool:item-1378", "缺少拆石器"],
    ["missingTool:item-3361", "缺少鑽孔器"],
    ["missingTool:safe-refine-stone", "缺少安全精鍊石"],
    ["missingTool:mystery-tool", "缺少需要的道具"],
    ["cannotMerge", "這種精鍊石無法再合成"],
    ["missingMaterials", "材料不足"],
    ["missionIncomplete", "需先完成前置任務"],
  ])("translates %s", (code, text) => {
    expect(reasonText(code)).toBe(text)
  })

  it("keeps an unknown code visible", () => {
    expect(reasonText("somethingNew")).toBe("無法完成（somethingNew）")
  })
})

describe("errorMessage", () => {
  it("joins the translated reasons", () => {
    expect(errorMessage("無法裝備", ["jobMismatch", "statTooLow:strength"])).toBe(
      "職業不符、力量不足",
    )
  })

  it("uses the detail when there are no reasons", () => {
    expect(errorMessage("缺少數量", [])).toBe("缺少數量")
  })
})

describe("bank", () => {
  it("locks deposits below Lv 15", () => {
    expect(BankMinimumLevel).toBe(15)
    expect(isBankLocked(14)).toBe(true)
    expect(isBankLocked(15)).toBe(false)
  })
})

describe("isUsable", () => {
  it.each([
    "StatPotion",
    "BagExpansion",
    "DepotExpansion",
    "StoneBox",
    "RefineBox",
    "PetEgg",
    "ProficiencyBook",
    "ExperienceCapsule",
    "Elixir",
  ] as const)("%s 可以從背包使用", (consumable) => {
    expect(isUsable(makeItem({ category: "Consumable", consumable }))).toBe(true)
  })

  it.each(["Supply", "Capture", "None"] as const)("%s 不能從背包使用", (consumable) => {
    expect(isUsable(makeItem({ category: "Consumable", consumable }))).toBe(false)
  })
})

describe("itemUseResultLines", () => {
  const nothing = { obtained: [], petName: null, skillPoints: 0, levelsGained: 0, buff: null }
  const now = new Date("2026-10-23T03:00:00Z")
  const used = (consumable: InventoryItem["consumable"], amount = 1) =>
    makeItem({ category: "Consumable", consumable, amount })

  it("寶箱列出抽到的道具，以空白分隔", () => {
    expect(
      itemUseResultLines(
        used("StoneBox", 2),
        { ...nothing, obtained: ["攻擊魔石", "疾風魔石"] },
        now,
      ),
    ).toEqual(["恭喜抽到 攻擊魔石 疾風魔石"])
    expect(
      itemUseResultLines(used("RefineBox", 3), { ...nothing, obtained: ["秘法精鍊石 Lv3"] }, now),
    ).toEqual(["恭喜抽到 秘法精鍊石 Lv3"])
  })

  it("寵物蛋、心得書與經驗膠囊", () => {
    expect(
      itemUseResultLines(used("PetEgg", 280), { ...nothing, petName: "流浪絨球" }, now),
    ).toEqual(["獲得寵物 流浪絨球"])
    expect(
      itemUseResultLines(used("ProficiencyBook", 1500), { ...nothing, skillPoints: 1500 }, now),
    ).toEqual(["技能點 +1,500"])
    expect(itemUseResultLines(used("ExperienceCapsule", 20000), nothing, now)).toEqual([
      "經驗值 +20,000",
    ])
    expect(
      itemUseResultLines(used("ExperienceCapsule", 20000), { ...nothing, levelsGained: 2 }, now),
    ).toEqual(["經驗值 +20,000，升級了！"])
  })

  it("其他道具沒有結果行", () => {
    expect(itemUseResultLines(used("StatPotion"), nothing, now)).toEqual([])
  })

  it("密藥列出效果、倍率與持續分鐘", () => {
    const elixir = makeItem({ category: "Consumable", consumable: "Elixir", name: "經驗密藥" })
    const buff = { kind: "Exp", multiplier: 1.5, endsAt: "2026-10-23T04:00:00Z" } as const
    expect(itemUseResultLines(elixir, { ...nothing, buff }, now)).toEqual([
      "經驗密藥生效：經驗 ×1.5，持續 60 分鐘",
    ])
  })

  it("裝置時鐘比伺服器慢幾秒時，一小時的密藥仍顯示 60 分鐘", () => {
    const elixir = makeItem({ category: "Consumable", consumable: "Elixir", name: "經驗密藥" })
    const buff = { kind: "Exp", multiplier: 1.5, endsAt: "2026-10-23T04:00:00Z" } as const
    const slowClock = new Date("2026-10-23T02:59:58Z")
    expect(itemUseResultLines(elixir, { ...nothing, buff }, slowClock)).toEqual([
      "經驗密藥生效：經驗 ×1.5，持續 60 分鐘",
    ])
  })

  it("牧場已滿的錯誤文案", () => {
    expect(reasonText("petsFull")).toBe("牧場已滿，最多只能擁有3隻寵物")
  })
})
