import { cleanup, render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { CharacterView, DepotView, InventoryView, Recipe } from "../game/types"
import { emptyBonus, makeCharacter, makeInventory, makeItem, makeShopItem } from "../test/fixtures"
import { BagScreen, type BagApi } from "./BagScreen"

const sword = makeItem({ inventoryId: 1, name: "測試劍" })
const heavySword = makeItem({
  inventoryId: 2,
  name: "巨劍",
  requires: { str: 30, smart: 0, agi: 0, au: 0 },
})
const potion = makeItem({
  inventoryId: 3,
  itemId: "small-potion",
  category: "Consumable",
  consumable: "Supply",
  name: "小藥水",
  quantity: 30,
  supplyHp: 50,
  supplySp: 25,
  price: 25,
  refineMax: 0,
})
const strengthPotion = makeItem({
  inventoryId: 4,
  itemId: "strength-potion",
  category: "Consumable",
  consumable: "StatPotion",
  name: "力量藥水",
  quantity: 2,
})
const inventory: InventoryView = makeInventory([sword, heavySword, potion, strengthPotion], {
  equipment: [
    {
      slot: "Weapon",
      quantity: 1,
      item: makeItem({
        inventoryId: 9,
        name: "精鋼劍",
        refineLevel: 2,
        holes: 2,
        sockets: [
          { hole: 1, stoneId: "ruby", stoneName: "紅寶石", rolled: makeItem().refineBonus },
        ],
      }),
    },
  ],
})

const depot: DepotView = {
  capacity: 20,
  entries: [makeItem({ inventoryId: 50, name: "倉庫劍" })],
}

const createFakeApi = (overrides: Partial<BagApi> = {}): BagApi => ({
  getInventory: vi.fn().mockResolvedValue(inventory),
  equip: vi.fn().mockResolvedValue(inventory),
  unequip: vi.fn().mockResolvedValue(inventory),
  useItem: vi.fn().mockResolvedValue(inventory),
  getShop: vi.fn().mockImplementation((category: string) =>
    Promise.resolve(
      category === "Consumable"
        ? [
            makeShopItem({
              id: "small-potion",
              name: "小藥水",
              category: "Consumable",
              price: 20,
            }),
          ]
        : [makeShopItem({ id: "wooden-sword", name: "木劍", price: 200 })],
    ),
  ),
  buy: vi.fn().mockResolvedValue(inventory),
  sell: vi.fn().mockResolvedValue(inventory),
  getDepot: vi.fn().mockResolvedValue(depot),
  store: vi.fn().mockResolvedValue({ inventory, depot }),
  take: vi.fn().mockResolvedValue({ inventory, depot }),
  deposit: vi.fn(),
  withdraw: vi.fn(),
  getMyCharacter: vi.fn().mockResolvedValue(makeCharacter({ money: 10 })),
  refine: vi.fn().mockResolvedValue({ success: true, inventory }),
  socket: vi.fn().mockResolvedValue({ success: true, inventory }),
  unsocket: vi.fn().mockResolvedValue({ success: true, recovered: true, inventory }),
  drill: vi.fn().mockResolvedValue({ success: false, inventory }),
  mergeStones: vi.fn().mockResolvedValue({ success: true, inventory }),
  getRecipes: vi.fn().mockResolvedValue([]),
  synthesize: vi.fn().mockResolvedValue({ successes: 0, attempts: 1, cost: 0, inventory }),
  ...overrides,
})

const renderBag = (
  api: BagApi = createFakeApi(),
  character: CharacterView = makeCharacter({ money: 1000 }),
) => {
  const onCharacterChanged = vi.fn()
  render(<BagScreen api={api} character={character} onCharacterChanged={onCharacterChanged} />)
  return { api, onCharacterChanged }
}

const openSubPage = async (name: string) => {
  await userEvent.click(await screen.findByRole("tab", { name }))
}

test("BagScreen_ShowsSixSubPages", async () => {
  renderBag()

  const tabs = within(await screen.findByRole("tablist", { name: "背包分頁" })).getAllByRole("tab")
  expect(tabs.map((tab) => tab.textContent)).toEqual([
    "背包",
    "裝備",
    "商店",
    "倉庫",
    "銀行",
    "鍛造",
  ])
  expect(screen.getByRole("tab", { name: "背包" })).toHaveAttribute("aria-selected", "true")
  expect(await screen.findByText("測試劍 +0")).toBeInTheDocument()

  await openSubPage("裝備")
  expect(screen.getByRole("button", { name: /^武器/ })).toBeInTheDocument()
  await openSubPage("商店")
  expect(await screen.findByText("木劍 200 金錢")).toBeInTheDocument()
  await openSubPage("倉庫")
  expect(await screen.findByText("倉庫 1/20")).toBeInTheDocument()
  await openSubPage("銀行")
  expect(screen.getByRole("button", { name: "提領" })).toBeInTheDocument()
  await openSubPage("鍛造")
  const forgeTabs = within(screen.getByRole("tablist", { name: "鍛造項目" })).getAllByRole("tab")
  expect(forgeTabs.map((tab) => tab.textContent)).toEqual(["精鍊", "鑲嵌", "合成"])
})

test("BagScreen_ListsItemsByCategoryWithUsage", async () => {
  renderBag()

  expect(await screen.findByText("武器 2/15")).toBeInTheDocument()
  expect(screen.getByText("巨劍 +0")).toBeInTheDocument()
  expect(screen.queryByText("小藥水 ×30")).not.toBeInTheDocument()

  await userEvent.click(screen.getByRole("tab", { name: "道具" }))

  expect(screen.getByText("道具 2/15")).toBeInTheDocument()
  expect(screen.getByText("小藥水 ×30")).toBeInTheDocument()
  expect(screen.getByText("力量藥水 ×2")).toBeInTheDocument()
})

test("BagScreen_SellButtonShowsFloorPrice", async () => {
  const { api, onCharacterChanged } = renderBag()
  await userEvent.click(await screen.findByRole("tab", { name: "道具" }))
  await userEvent.click(screen.getByRole("button", { name: /小藥水 ×30/ }))

  // floor(25 × 0.5 × 1) = 12，三個是 floor(37.5) = 37
  expect(screen.getByRole("button", { name: "賣出（12）" })).toBeInTheDocument()
  await userEvent.click(screen.getByRole("button", { name: "增加數量" }))
  await userEvent.click(screen.getByRole("button", { name: "增加數量" }))
  await userEvent.click(screen.getByRole("button", { name: "賣出（37）" }))

  expect(api.sell).toHaveBeenCalledWith(3, 3)
  expect(api.getMyCharacter).toHaveBeenCalled()
  expect(onCharacterChanged).toHaveBeenCalledWith(expect.objectContaining({ money: 10 }))
})

test("BagScreen_ItemActionsMatchTheItemKind", async () => {
  const { api } = renderBag()
  await userEvent.click(await screen.findByRole("button", { name: /測試劍 \+0/ }))

  expect(screen.getByRole("button", { name: "存入倉庫" })).toBeInTheDocument()
  expect(screen.queryByRole("button", { name: "使用" })).not.toBeInTheDocument()
  await userEvent.click(screen.getByRole("button", { name: "裝備" }))
  expect(api.equip).toHaveBeenCalledWith(1)

  await userEvent.click(screen.getByRole("tab", { name: "道具" }))
  await userEvent.click(screen.getByRole("button", { name: /力量藥水 ×2/ }))
  expect(screen.queryByRole("button", { name: "裝備" })).not.toBeInTheDocument()
  await userEvent.click(screen.getByRole("button", { name: "使用" }))
  expect(api.useItem).toHaveBeenCalledWith(4)
})

test("BagScreen_BagPageEquipsWholeSupplyStackCappedAt99", async () => {
  const { api } = renderBag()
  await userEvent.click(await screen.findByRole("tab", { name: "道具" }))
  await userEvent.click(screen.getByRole("button", { name: /小藥水 ×30/ }))
  await userEvent.click(screen.getByRole("button", { name: "裝備" }))

  // 與裝備子頁相同：整疊放上補給欄，上限 99
  expect(api.equip).toHaveBeenCalledWith(3, 30)
})

test("BagScreen_ReplacesInventoryWithMutationResponse", async () => {
  const afterSale = makeInventory([sword, heavySword, { ...potion, quantity: 29 }, strengthPotion])
  const afterPurchase = makeInventory([
    sword,
    heavySword,
    { ...potion, quantity: 29 },
    strengthPotion,
    makeItem({ inventoryId: 70, name: "新買的劍" }),
  ])
  const api = createFakeApi({
    sell: vi.fn().mockResolvedValue(afterSale),
    buy: vi.fn().mockResolvedValue(afterPurchase),
  })
  renderBag(api)
  await userEvent.click(await screen.findByRole("tab", { name: "道具" }))
  await userEvent.click(screen.getByRole("button", { name: /小藥水 ×30/ }))
  await userEvent.click(screen.getByRole("button", { name: "賣出（12）" }))

  expect(await screen.findByText("小藥水 ×29")).toBeInTheDocument()
  expect(screen.queryByText("小藥水 ×30")).not.toBeInTheDocument()

  await openSubPage("商店")
  const line = (await screen.findByText("木劍 200 金錢")).closest("li")
  if (line === null) throw new Error("找不到商品列")
  await userEvent.click(within(line).getByRole("button", { name: "購買" }))
  await openSubPage("背包")

  expect(await screen.findByText("新買的劍 +0")).toBeInTheDocument()
  expect(screen.getByText("武器 3/15")).toBeInTheDocument()
  expect(api.getInventory).toHaveBeenCalledTimes(1)
})

test("BagScreen_DepotReplacesBothListsWithMoveResponse", async () => {
  const takenSword = makeItem({ inventoryId: 80, name: "倉庫劍" })
  const api = createFakeApi({
    take: vi.fn().mockResolvedValue({
      inventory: makeInventory([sword, heavySword, potion, strengthPotion, takenSword]),
      depot: { capacity: 20, entries: [] },
    }),
  })
  renderBag(api)
  await openSubPage("倉庫")
  const stored = (await screen.findByText("倉庫劍 +0")).closest("li")
  if (stored === null) throw new Error("找不到倉庫道具")
  await userEvent.click(within(stored).getByRole("button", { name: "取出" }))

  expect(await screen.findByText("倉庫 0/20")).toBeInTheDocument()
  expect(screen.getByText("倉庫是空的")).toBeInTheDocument()
  const bag = screen.getByRole("region", { name: "背包" })
  expect(within(bag).getByText("倉庫劍 +0")).toBeInTheDocument()
  expect(api.getDepot).toHaveBeenCalledTimes(1)
  expect(api.getInventory).toHaveBeenCalledTimes(1)
})

test("BagScreen_BuySendsItemIdAndQuantity", async () => {
  const { api, onCharacterChanged } = renderBag()
  await openSubPage("商店")

  expect(await screen.findByText("木劍 200 金錢")).toBeInTheDocument()
  expect(api.getShop).toHaveBeenLastCalledWith("Weapon", 1)

  await userEvent.click(screen.getByRole("tab", { name: "道具" }))
  const line = (await screen.findByText("小藥水 20 金錢")).closest("li")
  if (line === null) throw new Error("找不到商品列")
  expect(api.getShop).toHaveBeenLastCalledWith("Consumable", 1)
  await userEvent.click(within(line).getByRole("button", { name: "增加數量" }))
  await userEvent.click(within(line).getByRole("button", { name: "增加數量" }))
  expect(within(line).getByText("3")).toBeInTheDocument()
  await userEvent.click(within(line).getByRole("button", { name: "購買" }))

  expect(api.buy).toHaveBeenCalledWith("small-potion", 3)
  expect(onCharacterChanged).toHaveBeenCalled()

  await userEvent.click(screen.getByRole("tab", { name: "Lv 2" }))
  expect(api.getShop).toHaveBeenLastCalledWith("Consumable", 2)
})

test("BagScreen_ShopLimitsEquipmentToOneAndDisablesWhenMoneyShort", async () => {
  renderBag(createFakeApi(), makeCharacter({ money: 100 }))
  await openSubPage("商店")

  const line = (await screen.findByText("木劍 200 金錢")).closest("li")
  if (line === null) throw new Error("找不到商品列")
  expect(within(line).getByRole("button", { name: "增加數量" })).toBeDisabled()
  expect(within(line).getByRole("button", { name: "購買" })).toBeDisabled()
  expect(within(line).getByText("力量 ≥ 10")).toBeInTheDocument()
})

test("BagScreen_EquipCandidatesShowRequirementsAndCrimsonForUnmet", async () => {
  const { api } = renderBag()
  await openSubPage("裝備")

  await userEvent.click(screen.getByRole("button", { name: /^武器/ }))

  const heavy = screen.getByText("巨劍 +0").closest("li")
  const light = screen.getByText("測試劍 +0").closest("li")
  if (heavy === null || light === null) throw new Error("找不到候選裝備")
  // 測試角色裸點力量 11
  expect(within(heavy).getByText("力量 ≥ 30")).toHaveClass("crimson-text")
  expect(within(heavy).getByRole("button", { name: "裝備" })).toBeDisabled()
  await userEvent.click(within(light).getByRole("button", { name: "裝備" }))

  expect(api.equip).toHaveBeenCalledWith(1)
})

test("BagScreen_EquipmentSlotsShowRefineAndHoles", async () => {
  const { api } = renderBag()
  await openSubPage("裝備")

  expect(screen.getByRole("button", { name: /^武器 精鋼劍 \+2 ◆◇$/ })).toBeInTheDocument()
  expect(screen.getByRole("button", { name: /^頭部 （空）$/ })).toBeInTheDocument()

  await userEvent.click(screen.getByRole("button", { name: /^武器/ }))
  await userEvent.click(screen.getByRole("button", { name: "卸下" }))

  expect(api.unequip).toHaveBeenCalledWith("Weapon")
})

test("BagScreen_SupplyEquipSendsQuantity", async () => {
  const { api } = renderBag()
  await openSubPage("裝備")
  await userEvent.click(screen.getByRole("button", { name: /^補給/ }))

  const input = screen.getByRole("spinbutton", { name: "補給數量" })
  await userEvent.clear(input)
  await userEvent.type(input, "20")
  await userEvent.click(screen.getByRole("button", { name: "裝備" }))

  expect(api.equip).toHaveBeenCalledWith(3, 20)
})

test("BagScreen_DepotStoresAndTakes", async () => {
  const { api } = renderBag()
  await openSubPage("倉庫")

  expect(await screen.findByText("倉庫 1/20")).toBeInTheDocument()
  const stored = screen.getByText("倉庫劍 +0").closest("li")
  if (stored === null) throw new Error("找不到倉庫道具")
  await userEvent.click(within(stored).getByRole("button", { name: "取出" }))
  expect(api.take).toHaveBeenCalledWith(50, 1)

  const supply = screen.getByText("小藥水 ×30").closest("li")
  if (supply === null) throw new Error("找不到背包道具")
  await userEvent.click(within(supply).getByRole("button", { name: "增加數量" }))
  await userEvent.click(within(supply).getByRole("button", { name: "存入" }))
  expect(api.store).toHaveBeenCalledWith(3, 2)
})

test("BagScreen_BankLockedBelowLevel15", async () => {
  renderBag(createFakeApi(), makeCharacter({ level: 14, money: 5000 }))
  await openSubPage("銀行")

  expect(screen.getByText("Lv 15 開放")).toBeInTheDocument()
  expect(screen.getByText("存款 0")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "存入" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "提領" })).toBeEnabled()
})

test("BagScreen_BankDepositsAtLevel15", async () => {
  const updated = makeCharacter({ level: 15, money: 4000 })
  const api = createFakeApi({ deposit: vi.fn().mockResolvedValue(updated) })
  const character = makeCharacter({ level: 15, money: 5000, bank: 3000 })
  const { onCharacterChanged } = renderBag(api, character)
  await openSubPage("銀行")

  expect(screen.getByText("存款 3,000")).toBeInTheDocument()
  expect(screen.queryByText("Lv 15 開放")).not.toBeInTheDocument()
  await userEvent.type(screen.getByRole("spinbutton", { name: "存入金額" }), "1000")
  await userEvent.click(screen.getByRole("button", { name: "存入" }))

  expect(api.deposit).toHaveBeenCalledWith(1000)
  expect(onCharacterChanged).toHaveBeenCalledWith(updated)
})

test("BagScreen_ShowsMappedErrorFromApiErrorReasons", async () => {
  const api = createFakeApi({
    buy: vi.fn().mockRejectedValue(new ApiError(400, "無法購買", ["bagFull"])),
    equip: vi.fn().mockRejectedValue(new ApiError(400, "無法裝備", ["statTooLow:str"])),
  })
  renderBag(api)

  await userEvent.click(await screen.findByRole("button", { name: /測試劍 \+0/ }))
  await userEvent.click(screen.getByRole("button", { name: "裝備" }))
  expect(await screen.findByRole("alert")).toHaveTextContent("力量不足")

  await openSubPage("商店")
  const line = (await screen.findByText("木劍 200 金錢")).closest("li")
  if (line === null) throw new Error("找不到商品列")
  await userEvent.click(within(line).getByRole("button", { name: "購買" }))
  expect(await screen.findByRole("alert")).toHaveTextContent("背包已滿")
})

test("BagScreen_ShowsRetry_WhenInventoryFailsToLoad", async () => {
  const api = createFakeApi({
    getInventory: vi.fn().mockRejectedValueOnce(new Error("offline")).mockResolvedValue(inventory),
  })
  renderBag(api)

  await userEvent.click(await screen.findByRole("button", { name: "重試" }))

  expect(await screen.findByText("測試劍 +0")).toBeInTheDocument()
})

const refineStone = (inventoryId: number, level: number, quantity = 1) =>
  makeItem({
    inventoryId,
    itemId: `iron-refine-stone-lv${level}`,
    refineStoneLevel: level,
    name: `鋼鐵精鍊石 Lv${level}`,
    category: "RefineStone",
    quantity,
  })
const bagItem = (
  inventoryId: number,
  itemId: string,
  name: string,
  category: "Stone" | "Material",
  quantity = 1,
) => makeItem({ inventoryId, itemId, name, category, quantity })
const wornSword = makeItem({
  inventoryId: 9,
  name: "精鋼劍",
  refineLevel: 2,
  holes: 2,
  sockets: [{ hole: 1, stoneId: "attack-stone", stoneName: "攻擊魔石", rolled: emptyBonus }],
})
const forgeItems = [
  sword,
  refineStone(30, 3),
  refineStone(31, 2, 3),
  bagItem(40, "attack-stone", "攻擊魔石", "Stone"),
  bagItem(41, "guard-stone", "守護魔石", "Stone"),
  bagItem(50, "safe-refine-stone", "安全精鍊石", "Material"),
  bagItem(51, "stone-remover", "拆石器", "Material"),
  bagItem(52, "hole-driller", "鑽孔器", "Material"),
  bagItem(60, "iron-ore", "鐵礦石", "Material", 7),
  bagItem(61, "magic-dust", "魔法粉塵", "Material", 5),
]
const forgeInventory = makeInventory(forgeItems, {
  equipment: [{ slot: "Weapon", quantity: 1, item: wornSword }],
})
const ingot: Recipe = {
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
const crystal: Recipe = {
  ...ingot,
  id: "crystal",
  resultItemId: "crystal",
  resultName: "晶石",
  materials: [{ itemId: "crystal-shard", name: "水晶碎片", quantity: 2 }],
}

const renderForge = async (
  overrides: Partial<BagApi> = {},
  character: CharacterView = makeCharacter({ money: 5000 }),
) => {
  const rendered = renderBag(
    createFakeApi({
      getInventory: vi.fn().mockResolvedValue(forgeInventory),
      getRecipes: vi.fn().mockResolvedValue([ingot, crystal]),
      ...overrides,
    }),
    character,
  )
  await openSubPage("鍛造")
  return rendered
}

test("BagScreen_ForgeRefineSendsBodyAndShowsSuccess", async () => {
  const refined = makeInventory(forgeItems.slice(1), {
    equipment: [{ slot: "Weapon", quantity: 1, item: { ...wornSword, refineLevel: 3 } }],
  })
  const { api, onCharacterChanged } = await renderForge({
    refine: vi.fn().mockResolvedValue({ success: true, inventory: refined }),
  })

  expect(screen.getByRole("combobox", { name: "選擇裝備" })).toHaveDisplayValue(
    "精鋼劍 +2（裝備中）",
  )
  expect(screen.getByText("+2 → +3（成功率 71%）")).toBeInTheDocument()
  await userEvent.click(screen.getByRole("radio", { name: /鋼鐵精鍊石 Lv3/ }))
  await userEvent.click(screen.getByRole("checkbox", { name: "使用安全石" }))
  await userEvent.click(screen.getByRole("button", { name: "精鍊" }))

  expect(api.refine).toHaveBeenCalledWith(9, 30, true)
  expect(await screen.findByRole("status")).toHaveTextContent("精鍊成功！精鋼劍 +3")
  expect(screen.getByText("+3 → +4（成功率 61%）")).toBeInTheDocument()
  expect(api.getMyCharacter).toHaveBeenCalled()
  expect(onCharacterChanged).toHaveBeenCalled()
})

test("BagScreen_ForgeRefineFailureNamesTheLostStone", async () => {
  const { api } = await renderForge({
    refine: vi.fn().mockResolvedValue({ success: false, inventory: forgeInventory }),
  })

  await userEvent.click(screen.getByRole("radio", { name: /鋼鐵精鍊石 Lv3/ }))
  await userEvent.click(screen.getByRole("button", { name: "精鍊" }))
  expect(api.refine).toHaveBeenCalledWith(9, 30, false)
  expect(await screen.findByRole("status")).toHaveTextContent("精鍊失敗，鋼鐵精鍊石 Lv3消失了。")

  await userEvent.click(screen.getByRole("checkbox", { name: "使用安全石" }))
  await userEvent.click(screen.getByRole("button", { name: "精鍊" }))
  expect(await screen.findByRole("status")).toHaveTextContent("精鍊失敗，安全石消失了。")
})

test("BagScreen_ForgeRefineDisablesMismatchedStonesAndMissingSafeStone", async () => {
  const withoutSafeStone = makeInventory(
    forgeItems.filter((item) => item.itemId !== "safe-refine-stone"),
    { equipment: [{ slot: "Weapon", quantity: 1, item: wornSword }] },
  )
  await renderForge({ getInventory: vi.fn().mockResolvedValue(withoutSafeStone) })

  expect(screen.getByRole("radio", { name: /鋼鐵精鍊石 Lv2.*等級不符/ })).toBeDisabled()
  expect(screen.getByRole("radio", { name: /鋼鐵精鍊石 Lv3/ })).toBeEnabled()
  expect(screen.getByRole("checkbox", { name: "使用安全石" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "精鍊" })).toBeDisabled()
})

test("BagScreen_ForgeRefineStopsAtRefineMax", async () => {
  await renderForge()

  await userEvent.selectOptions(screen.getByRole("combobox", { name: "選擇裝備" }), "測試劍 +0")
  expect(screen.getByText("+0 → +1（成功率 91%）")).toBeInTheDocument()

  const maxed = makeInventory([{ ...sword, refineLevel: 4 }, refineStone(30, 5)])
  cleanup()
  await renderForge({ getInventory: vi.fn().mockResolvedValue(maxed) })
  expect(screen.getByText("已達上限")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "精鍊" })).toBeDisabled()
})

test("BagScreen_ForgeMergesRefineStones", async () => {
  const { api } = await renderForge()

  const merge = screen.getByRole("region", { name: "精鍊石合成" })
  const line = within(merge).getByText("鋼鐵精鍊石 Lv2 ×3").closest("li")
  if (line === null) throw new Error("找不到精鍊石合成列")
  await userEvent.click(within(line).getByRole("button", { name: "合成" }))

  expect(api.mergeStones).toHaveBeenCalledWith(31, 1)
  expect(await screen.findByRole("status")).toHaveTextContent("精鍊石合成成功！")
})

test("BagScreen_ForgeSocketSendsHoleAndStone", async () => {
  const { api } = await renderForge()
  await userEvent.click(screen.getByRole("tab", { name: "鑲嵌" }))

  expect(screen.getByRole("button", { name: "孔 1：攻擊魔石" })).toHaveTextContent("◆")
  await userEvent.click(screen.getByRole("button", { name: "孔 2：空" }))
  expect(screen.getByRole("radio", { name: /攻擊魔石.*已有同種魔石/ })).toBeDisabled()
  await userEvent.click(screen.getByRole("radio", { name: /守護魔石/ }))
  await userEvent.click(screen.getByRole("button", { name: "鑲嵌" }))

  expect(api.socket).toHaveBeenCalledWith(9, 2, 41)
  expect(await screen.findByRole("status")).toHaveTextContent("鑲嵌成功！")
})

test("BagScreen_ForgeUnsocketAndDrillNarrateTheRoll", async () => {
  const { api } = await renderForge({
    unsocket: vi
      .fn()
      .mockResolvedValue({ success: true, recovered: false, inventory: forgeInventory }),
  })
  await userEvent.click(screen.getByRole("tab", { name: "鑲嵌" }))

  await userEvent.click(screen.getByRole("button", { name: "孔 1：攻擊魔石" }))
  await userEvent.click(screen.getByRole("button", { name: "拆除（需要拆石器）" }))
  expect(api.unsocket).toHaveBeenCalledWith(9, 1)
  expect(await screen.findByRole("status")).toHaveTextContent("拆除後攻擊魔石消失了。")

  await userEvent.click(screen.getByRole("button", { name: "打洞（需要鑽孔器，8%）" }))
  expect(api.drill).toHaveBeenCalledWith(9)
  expect(await screen.findByRole("status")).toHaveTextContent("打洞失敗。")
})

test("BagScreen_ForgeDrillDisabledAtFourHoles", async () => {
  const fourHoles = makeInventory(forgeItems, {
    equipment: [{ slot: "Weapon", quantity: 1, item: { ...wornSword, holes: 4 } }],
  })
  await renderForge({ getInventory: vi.fn().mockResolvedValue(fourHoles) })
  await userEvent.click(screen.getByRole("tab", { name: "鑲嵌" }))

  expect(screen.getByRole("button", { name: "打洞（需要鑽孔器，8%）" })).toBeDisabled()
})

test("BagScreen_ForgeSynthesizeSendsRecipeAndAttempts", async () => {
  const { api } = await renderForge({
    synthesize: vi
      .fn()
      .mockResolvedValue({ successes: 1, attempts: 2, cost: 1000, inventory: forgeInventory }),
  })
  await userEvent.click(screen.getByRole("tab", { name: "合成" }))

  const line = (await screen.findByText("鐵錠 ×1 ← 鐵礦石 ×3、魔法粉塵 ×1（60%）")).closest("li")
  if (line === null) throw new Error("找不到配方列")
  await userEvent.click(within(line).getByRole("button", { name: "增加數量" }))
  // 鐵礦石 7 顆只夠 2 次
  expect(within(line).getByRole("button", { name: "增加數量" })).toBeDisabled()
  await userEvent.click(within(line).getByRole("button", { name: "合成（2,000 金錢上限）" }))

  expect(api.synthesize).toHaveBeenCalledWith("iron-ingot", 2)
  expect(await screen.findByRole("status")).toHaveTextContent(
    "合成 2 次，成功 1 次，花費 1,000 金錢。",
  )
  expect(api.getMyCharacter).toHaveBeenCalled()
})

test("BagScreen_ForgeSynthesizeDisabledWithoutMaterials", async () => {
  await renderForge()
  await userEvent.click(screen.getByRole("tab", { name: "合成" }))

  const line = (await screen.findByText("晶石 ×1 ← 水晶碎片 ×2（60%）")).closest("li")
  if (line === null) throw new Error("找不到配方列")
  expect(within(line).getByRole("button", { name: "合成（1,000 金錢上限）" })).toBeDisabled()
})

test("BagScreen_ForgeShowsMappedErrorReasons", async () => {
  await renderForge({
    refine: vi.fn().mockRejectedValue(new ApiError(400, "無法精鍊", ["wrongStoneLevel"])),
  })

  await userEvent.click(screen.getByRole("radio", { name: /鋼鐵精鍊石 Lv3/ }))
  await userEvent.click(screen.getByRole("button", { name: "精鍊" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("精鍊石等級不符")
})

test("BagScreen_ForgeHoleNamesItsStoneWithoutABagCopy", async () => {
  const chaosSword = {
    ...wornSword,
    sockets: [{ hole: 1, stoneId: "chaos-stone", stoneName: "混沌魔石", rolled: emptyBonus }],
  }
  const onlyWorn = makeInventory(forgeItems, {
    equipment: [{ slot: "Weapon", quantity: 1, item: chaosSword }],
  })
  const { api } = await renderForge({
    getInventory: vi.fn().mockResolvedValue(onlyWorn),
    unsocket: vi
      .fn()
      .mockResolvedValue({ success: true, recovered: false, inventory: forgeInventory }),
  })
  await userEvent.click(screen.getByRole("tab", { name: "鑲嵌" }))

  await userEvent.click(screen.getByRole("button", { name: "孔 1：混沌魔石" }))
  expect(screen.getByText("混沌魔石")).toBeInTheDocument()
  await userEvent.click(screen.getByRole("button", { name: "拆除（需要拆石器）" }))

  expect(api.unsocket).toHaveBeenCalledWith(9, 1)
  expect(await screen.findByRole("status")).toHaveTextContent("拆除後混沌魔石消失了。")
})

test("BagScreen_ForgeSynthesizeNeedsMoneyForEveryAttempt", async () => {
  await renderForge({}, makeCharacter({ money: 1500 }))
  await userEvent.click(screen.getByRole("tab", { name: "合成" }))

  const line = (await screen.findByText("鐵錠 ×1 ← 鐵礦石 ×3、魔法粉塵 ×1（60%）")).closest("li")
  if (line === null) throw new Error("找不到配方列")
  expect(within(line).getByRole("button", { name: "合成（1,000 金錢上限）" })).toBeEnabled()
  await userEvent.click(within(line).getByRole("button", { name: "增加數量" }))
  expect(within(line).getByRole("button", { name: "合成（2,000 金錢上限）" })).toBeDisabled()

  cleanup()
  await renderForge({}, makeCharacter({ money: 2000 }))
  await userEvent.click(screen.getByRole("tab", { name: "合成" }))
  const exact = (await screen.findByText("鐵錠 ×1 ← 鐵礦石 ×3、魔法粉塵 ×1（60%）")).closest("li")
  if (exact === null) throw new Error("找不到配方列")
  await userEvent.click(within(exact).getByRole("button", { name: "增加數量" }))
  expect(within(exact).getByRole("button", { name: "合成（2,000 金錢上限）" })).toBeEnabled()
})

test("BagScreen_ForgeToolButtonsNeedTheirTools", async () => {
  const withoutTools = makeInventory(
    forgeItems.filter((item) => item.itemId !== "stone-remover" && item.itemId !== "hole-driller"),
    { equipment: [{ slot: "Weapon", quantity: 1, item: wornSword }] },
  )
  await renderForge({ getInventory: vi.fn().mockResolvedValue(withoutTools) })
  await userEvent.click(screen.getByRole("tab", { name: "鑲嵌" }))
  await userEvent.click(screen.getByRole("button", { name: "孔 1：攻擊魔石" }))

  expect(screen.getByRole("button", { name: "拆除（需要拆石器）" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "打洞（需要鑽孔器，8%）" })).toBeDisabled()
})

test("BagScreen_ForgeSynthesizeUsesTheRecipeCost", async () => {
  await renderForge(
    { getRecipes: vi.fn().mockResolvedValue([{ ...ingot, costPerAttempt: 500 }]) },
    makeCharacter({ money: 1000 }),
  )
  await userEvent.click(screen.getByRole("tab", { name: "合成" }))

  const line = (await screen.findByText("鐵錠 ×1 ← 鐵礦石 ×3、魔法粉塵 ×1（60%）")).closest("li")
  if (line === null) throw new Error("找不到配方列")
  await userEvent.click(within(line).getByRole("button", { name: "增加數量" }))
  expect(within(line).getByRole("button", { name: "合成（1,000 金錢上限）" })).toBeEnabled()
})
