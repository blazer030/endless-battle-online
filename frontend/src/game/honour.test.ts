import { describe, expect, it } from "vitest"
import { makeHonourShop } from "../test/fixtures"
import { costText, exchangeGroups, honourErrorMessage, isShort, medalText } from "./honour"

const shop = makeHonourShop()

describe("medalText", () => {
  it("shows the medal and how many are held", () => {
    expect(shop.medals.map(medalText)).toEqual([
      "白銀勳章 ×0",
      "大陸勳章 ×2",
      "王國勳章 ×0",
      "雨林勳章 ×0",
      "王城勳章 ×0",
    ])
  })
})

describe("costText", () => {
  it("names each medal and quantity and flags the short ones", () => {
    const [mercenary] = shop.exchanges
    expect(mercenary.cost.map(costText)).toEqual(["大陸勳章 ×3", "王國勳章 ×3"])
    expect(mercenary.cost.map(isShort)).toEqual([true, true])
    expect(isShort({ itemId: "a", name: "a", quantity: 2, owned: 2 })).toBe(false)
  })
})

describe("exchangeGroups", () => {
  it("splits mercenaries from items", () => {
    expect(
      exchangeGroups(shop.exchanges).map(({ title, exchanges }) => [
        title,
        exchanges.map((exchange) => exchange.rewardName),
      ]),
    ).toEqual([
      ["傭兵", ["B級傭兵"]],
      ["道具", ["攻擊魔石"]],
    ])
  })
})

describe("honourErrorMessage", () => {
  it("maps every honour code", () => {
    expect(
      honourErrorMessage(["honourUnknown", "medalsMissing", "mercenaryHired", "bagFull"], "x"),
    ).toBe("沒有這個兌換項目、勳章不足、只能僱用一位傭兵、背包已滿")
  })

  it("falls back to the detail for unknown or missing codes", () => {
    expect(honourErrorMessage(["somethingNew"], "無法兌換")).toBe("無法兌換")
    expect(honourErrorMessage([], "無法兌換")).toBe("無法兌換")
  })
})
