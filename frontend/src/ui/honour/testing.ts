import { vi } from "vitest"
import type { ApiClient } from "../../api/client"
import type { HonourExchange, HonourItemDetail, HonourShopVo } from "../../game/honour"
import { makeHonourItem, makeHonourShop } from "../../test/fixtures"
import type { HonourScreenApi } from "./HonourScreen"

// 勳章所測試共用的勳章：大陸 14、王國 6、雨林 3、王城 0、白銀 2（同設計稿示範）
export const medals = [
  { itemId: "item-1304", name: "大陸勳章", owned: 14 },
  { itemId: "item-1305", name: "王國勳章", owned: 6 },
  { itemId: "item-1306", name: "雨林勳章", owned: 3 },
  { itemId: "item-1307", name: "王城勳章", owned: 0 },
  { itemId: "item-2222", name: "白銀勳章", owned: 2 },
]

export const shopWith = (overrides: Partial<HonourShopVo> = {}) =>
  makeHonourShop({ medals, exchanges: [], hiredMercenaryName: null, ...overrides })

export const createHonourApi = (
  shop: HonourShopVo = shopWith(),
): HonourScreenApi & Pick<ApiClient, "advance"> => ({
  getHonourShop: vi.fn().mockResolvedValue(shop),
  exchangeHonour: vi.fn(),
  getMyCharacter: vi.fn().mockResolvedValue(null),
  advance: vi.fn(),
})

type Cost = [itemId: string, quantity: number]

const costOf = (cost: Cost[]): HonourExchange["cost"] =>
  cost.map(([itemId, quantity]) => ({
    itemId,
    name: medals.find((medal) => medal.itemId === itemId)?.name ?? itemId,
    quantity,
    owned: medals.find((medal) => medal.itemId === itemId)?.owned ?? 0,
  }))

// 測試用兌換：道具（分類、數量）與傭兵；affordable 照持有數自己算
export const itemExchange = (
  id: string,
  name: string,
  category: HonourItemDetail["category"],
  cost: Cost[],
  options: { quantity?: number; item?: Partial<HonourItemDetail> } = {},
): HonourExchange => {
  const costs = costOf(cost)
  return {
    id,
    kind: "item",
    rewardId: `item-${id}`,
    rewardName: name,
    quantity: options.quantity ?? 1,
    cost: costs,
    affordable: costs.every((entry) => entry.owned >= entry.quantity),
    item: makeHonourItem({
      category,
      consumable: "None",
      requirements: [],
      elixir: null,
      key: null,
      ...options.item,
    }),
    mercenary: null,
  }
}

export const mercenaryExchange = (id: string, name: string, cost: Cost[]): HonourExchange => {
  const costs = costOf(cost)
  return {
    id,
    kind: "mercenary",
    rewardId: `mercenary-${id}`,
    rewardName: name,
    quantity: 1,
    cost: costs,
    affordable: costs.every((entry) => entry.owned >= entry.quantity),
    item: null,
    mercenary: {
      fights: 900,
      moneyPercent: 70,
      dropPercent: 70,
      experiencePercent: 100,
      deathCost: 2,
    },
  }
}

// 設計稿示範的幾筆：大陸 14 夠換巨石槌、刺心者、破山斧，不夠 C級傭兵（王國 6）
export const demoExchanges = [
  mercenaryExchange("1", "C級傭兵", [
    ["item-1304", 13],
    ["item-1305", 13],
  ]),
  itemExchange("9", "巨石槌", "Weapon", [["item-1304", 2]], {
    item: { requirements: [{ stat: "strength", minimum: 145, met: true }], holes: 2, refineMax: 5 },
  }),
  itemExchange("11", "刺心者", "Weapon", [["item-1304", 9]], {
    item: {
      requirements: [{ stat: "strength", minimum: 435, met: false }],
      holes: 2,
      refineMax: 4,
    },
  }),
  itemExchange("10", "破山斧", "Weapon", [["item-1304", 5]]),
  itemExchange(
    "29",
    "靜止鑰匙",
    "Key",
    [
      ["item-1305", 3],
      ["item-1307", 3],
    ],
    { item: { key: { placeName: "靜止之城", seconds: 86400 } } },
  ),
  itemExchange("133", "90000HP回復劑", "Consumable", [["item-2222", 1]], {
    quantity: 10,
    item: { consumable: "Supply", supplyHealthPoints: 90000, bound: false, holes: 0, refineMax: 0 },
  }),
  itemExchange(
    "142",
    "劣質打孔石",
    "Material",
    [
      ["item-1304", 1],
      ["item-1305", 1],
      ["item-1306", 1],
      ["item-1307", 1],
      ["item-2222", 1],
    ],
    { quantity: 3 },
  ),
]
