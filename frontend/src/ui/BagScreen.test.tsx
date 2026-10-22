import { render, screen, within } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { CharacterView, DepotView, InventoryView } from "../game/types"
import { makeCharacter, makeInventory, makeItem, makeShopItem } from "../test/fixtures"
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
        sockets: [{ hole: 1, stoneId: "ruby", rolled: makeItem().refineBonus }],
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
  expect(screen.getByText("鍛造功能即將開放")).toBeInTheDocument()
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
