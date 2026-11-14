import { describe, expect, it } from "vitest"
import { emptyBonus, makeHonourItem, makeHonourShop } from "../test/fixtures"
import {
  bonusRows,
  categoryLine,
  clerkGreeting,
  clerkLine,
  costText,
  effectText,
  exchangeGroups,
  honourErrorMessage,
  honourRevealOrigin,
  isShort,
  medalCase,
  medalText,
  mercenaryRows,
  quantityBadge,
  rackHeight,
  rackSections,
  requirementRows,
  sealBlock,
  slotLine,
  type HonourExchange,
  type HonourItemDetail,
} from "./honour"

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

// ---- 勳章所新畫面（HonourFit）的純模型 ----

const medal = (itemId: string, quantity: number, owned = 0) => ({
  itemId,
  name: itemId,
  quantity,
  owned,
})

const itemExchange = (
  id: string,
  category: HonourItemDetail["category"],
  cost: HonourExchange["cost"],
  quantity = 1,
): HonourExchange => ({
  id,
  kind: "item",
  rewardId: id,
  rewardName: id,
  quantity,
  cost,
  affordable: true,
  item: makeHonourItem({ category }),
  mercenary: null,
})

const mercenaryExchange = (id: string, cost: HonourExchange["cost"]): HonourExchange => ({
  ...itemExchange(id, "Weapon", cost),
  kind: "mercenary",
  item: null,
  mercenary: {
    fights: 900,
    moneyPercent: 70,
    dropPercent: 70,
    experiencePercent: 100,
    deathCost: 2,
  },
})

describe("honour layout", () => {
  // 規格 honour-screen「Honour layout follows the stage height」的 layout by height 表
  it.each([
    [624, [287, 208], 330, 155, 20, 449, 337],
    [683, [278, 226], 347, 163, 23, 497, 385],
    [720, [271, 237], 357, 168, 25, 527, 415],
    [780, [267, 256], 374, 176, 28, 576, 464],
  ])("H %i", (height, origin, width, caseHeight, bottom, top, rack) => {
    expect(honourRevealOrigin(height)).toEqual({ x: origin[0], y: origin[1] })
    expect(medalCase(height)).toEqual({ width, height: caseHeight, bottom, top })
    expect(rackHeight(height)).toBe(rack)
  })
})

describe("rackSections", () => {
  const silver = "item-2222"
  // 規格 honour-screen「Display rack」的 silver rack order：分區先、組內依白銀數量、同數依內容順序
  it("orders by section, then by this medal's quantity, then by content order", () => {
    const exchanges = [
      itemExchange("90000HP回復劑", "Consumable", [medal(silver, 1)], 10),
      itemExchange("魔法捕捉器", "Consumable", [medal(silver, 2)], 5),
      itemExchange("110000HP回復劑", "Consumable", [medal(silver, 3)], 10),
      itemExchange("1500SP藥水", "Consumable", [medal(silver, 2)], 10),
      itemExchange("劣質打孔石", "Material", [medal("item-1304", 1), medal(silver, 1)], 3),
      itemExchange("大魔石寶箱", "Consumable", [medal(silver, 1)]),
      itemExchange("巨石槌", "Weapon", [medal("item-1304", 2)]),
    ]

    expect(
      rackSections(exchanges, silver).map((section) => [
        section.title,
        section.exchanges.map((exchange) => exchange.id),
      ]),
    ).toEqual([
      ["道具", ["90000HP回復劑", "大魔石寶箱", "魔法捕捉器", "1500SP藥水", "110000HP回復劑"]],
      ["材料", ["劣質打孔石"]],
    ])
  })

  it("lists an exchange paid with several medals on each of their racks", () => {
    const exchanges = [
      mercenaryExchange("C級傭兵", [medal("item-1304", 13), medal("item-1305", 13)]),
      itemExchange("白陽短劍", "Weapon", [medal("item-1304", 8), medal("item-1306", 8)]),
      itemExchange("頭盔", "Head", [medal("item-1306", 3)]),
      itemExchange("鑰匙", "Key", [medal("item-1306", 8)]),
    ]

    expect(rackSections(exchanges, "item-1304").map((section) => section.title)).toEqual([
      "傭兵",
      "武器",
    ])
    expect(rackSections(exchanges, "item-1305").map((section) => section.title)).toEqual(["傭兵"])
    expect(rackSections(exchanges, "item-1306").map((section) => section.title)).toEqual([
      "武器",
      "頭部",
      "鑰匙",
    ])
  })
})

describe("clerk lines", () => {
  // 規格 honour-screen「Lise speaks feedback and errors」的 clerk lines 表（櫃檯的話含換行）
  it.each([
    [{ load: "ready", medals: 5, visit: "first" }, "歡迎來到勳章所！\n今天想兌換些什麼呢？", false],
    [{ load: "ready", medals: 0, visit: "first" }, "啊，您還沒有勳章呢……\n有了再來找我喔！", false],
    [{ load: "ready", medals: 5, visit: "browsed" }, "慢慢挑沒關係，\n陳列板不會跑掉的～", false],
    [{ load: "ready", medals: 0, visit: "exchanged" }, "兌換好了！\n祝您冒險一路順風。", false],
    [
      { load: "loading", medals: 0, visit: "first" },
      "歡迎來到勳章所！\n我翻一下您的帳冊喔……",
      false,
    ],
    [{ load: "failed", medals: 0, visit: "first" }, "哎呀，帳冊一時\n翻不到了……", true],
  ] as const)("counter %o", (state, text, failed) => {
    expect(clerkGreeting(state)).toEqual({ text, failed })
  })

  it.each([
    [{ kind: "exchangedItem" }, "換好了！已經幫您放進背包囉。", false],
    [{ kind: "exchangedMercenary", name: "C級傭兵" }, "C級傭兵的契約簽好囉，祝您順利！", false],
    [
      { kind: "refused", reasons: ["bagFull"], category: "Consumable" },
      "哎呀，道具的格子已經滿了……",
      true,
    ],
    [
      { kind: "refused", reasons: ["bagFull"], category: "Weapon" },
      "哎呀，武器的格子已經滿了……",
      true,
    ],
    [
      { kind: "refused", reasons: ["mercenaryHired"], category: null },
      "您已經有一位傭兵了，一次只能一位喔。",
      true,
    ],
    [
      { kind: "refused", reasons: ["medalsMissing", "mercenaryHired"], category: null },
      "勳章好像還差一點呢……",
      true,
    ],
    [
      { kind: "refused", reasons: ["honourUnknown"], category: null },
      "咦，剛剛沒送出去……再試一次吧？",
      true,
    ],
    [{ kind: "failed" }, "咦，剛剛沒送出去……再試一次吧？", true],
  ] as const)("sheet %o", (event, text, failed) => {
    expect(clerkLine(event)).toEqual({ text, failed })
  })
})

describe("effectText", () => {
  // 規格 honour-screen「Exchange sheet」的 effect text 表
  it.each([
    [{ consumable: "Supply", supplyHealthPoints: 90000 }, "補給欄自動使用：HP 回復 90,000"],
    [{ consumable: "Supply", supplyManaPoints: 1500 }, "補給欄自動使用：SP 回復 1,500"],
    [
      { category: "Key", key: { placeName: "靜止之城", seconds: 86400 } },
      "開啟「靜止之城」24 小時",
    ],
    [
      { consumable: "Elixir", elixir: { kind: "Attack", multiplier: 1.05, seconds: 7200 } },
      "物攻 ×1.05，2 小時",
    ],
    [{ consumable: "Capture", amount: 5 }, "捕捉寵物用（捕捉力 5）"],
    [{ consumable: "StoneBox", amount: 1 }, "打開隨機獲得魔石 ×1"],
    [{ consumable: "RefineBox", amount: 1 }, "打開隨機獲得精鍊石 ×1"],
    [{ consumable: "PetEgg" }, "孵出一隻隨機寵物"],
    [{ category: "Weapon" }, null],
  ] as const)("%o", (overrides, text) => {
    expect(
      effectText(makeHonourItem({ consumable: "None", elixir: null, key: null, ...overrides })),
    ).toBe(text)
  })
})

describe("sheet rows", () => {
  it("colours each requirement by the server's verdict", () => {
    expect(
      requirementRows(
        makeHonourItem({
          requirements: [
            { stat: "strength", minimum: 435, met: false },
            { stat: "charisma", minimum: 20, met: true },
          ],
        }),
      ),
    ).toEqual([
      { text: "力量 435", met: false },
      { text: "魅力 20", met: true },
    ])
  })

  it("names the category with the bound mark and the holes with the refine cap", () => {
    expect(categoryLine(makeHonourItem({ category: "Weapon", bound: true }))).toBe("武器・綁定")
    expect(categoryLine(makeHonourItem({ category: "Consumable", bound: false }))).toBe("道具")
    expect(slotLine(makeHonourItem({ category: "Weapon", holes: 2, refineMax: 4 }))).toBe(
      "孔 2・精鍊上限 +4",
    )
    expect(slotLine(makeHonourItem({ category: "Consumable", holes: 0, refineMax: 0 }))).toBeNull()
  })

  it("lists the non-zero bonuses in the bag's order", () => {
    expect(bonusRows({ ...emptyBonus, strength: 5, attack: 225, faith: 16 })).toEqual([
      { label: "物攻", value: "+225" },
      { label: "力量", value: "+5" },
      { label: "信仰", value: "+16" },
    ])
  })

  it("lists the mercenary's terms", () => {
    expect(
      mercenaryRows({
        fights: 900,
        moneyPercent: 70,
        dropPercent: 70,
        experiencePercent: 100,
        deathCost: 2,
      }),
    ).toEqual([
      { label: "可戰鬥", value: "900 次" },
      { label: "金錢", value: "70%" },
      { label: "寶物", value: "70%" },
      { label: "經驗", value: "100%" },
      { label: "戰敗消耗", value: "2 次" },
    ])
  })

  it("marks quantities above one", () => {
    expect(quantityBadge(10)).toBe("×10")
    expect(quantityBadge(1)).toBeNull()
  })
})

describe("sealBlock", () => {
  const [mercenary, item] = makeHonourShop().exchanges

  it("blocks a mercenary while one is hired, then an unaffordable exchange", () => {
    expect(sealBlock({ ...mercenary, affordable: true }, "葛雷")).toBe("僱用中：葛雷")
    expect(sealBlock({ ...mercenary, affordable: false }, null)).toBe("勳章不足")
    expect(sealBlock({ ...item, affordable: true }, "葛雷")).toBeNull()
    expect(sealBlock({ ...item, affordable: false }, "葛雷")).toBe("勳章不足")
  })
})
