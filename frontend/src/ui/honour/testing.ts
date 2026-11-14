import { vi } from "vitest"
import type { ApiClient } from "../../api/client"
import type { HonourShopVo } from "../../game/honour"
import { makeHonourShop } from "../../test/fixtures"
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
