import { expect, test } from "vitest"
import { makePet, makePets, makeWorld } from "../test/fixtures"
import {
  feedCooldownElapsed,
  activationRatio,
  bartonGreeting,
  canHire,
  companionsCounterTop,
  deployGroups,
  feedEffect,
  fruitDisabled,
  hallChip,
  intimacyRatio,
  keeperLine,
  miraGreeting,
  satietyRatio,
  shortPrice,
  stableChip,
  type KeeperEvent,
} from "./companions"
import type { MercenaryCatalogueEntry, MercenaryContract } from "./mercenaries"
import type { PetFood } from "./pets"

const contract = (overrides: Partial<MercenaryContract> = {}): MercenaryContract => ({
  mercenaryId: "mercenary-1",
  name: "葛雷",
  remaining: 640,
  placeId: 4,
  placeName: "迷霧森林",
  fighting: true,
  nextFightAt: null,
  ...overrides,
})

const smallVigor: PetFood = {
  id: "fruit-vigor-small",
  name: "初級活力果實",
  stat: "Attack",
  amount: 2,
  cost: 20,
}

// 規格 companions-screen：lines 表
test.each<[KeeperEvent, "mira" | "barton", string, boolean]>([
  [{ kind: "carried", name: "小風" }, "mira", "小風會跟著你出門囉！", false],
  [{ kind: "stored", name: "小風" }, "mira", "小風回欄裡休息囉。", false],
  [{ kind: "petRenamed", name: "小風" }, "mira", "「小風」，牠很喜歡喔！", false],
  [{ kind: "released", name: "小風" }, "mira", "小風回到野外去了，要保重喔。", false],
  [{ kind: "petRejected", reason: "overOwner" }, "mira", "再吃下去，牠就要比你還強了呢。", true],
  [
    { kind: "petRejected", reason: "notEnoughMoney" },
    "mira",
    "哎呀，這顆果實的錢好像不太夠。",
    true,
  ],
  [{ kind: "petRejected", reason: "nameInvalid" }, "mira", "這名字牠好像記不住呢。", true],
  [{ kind: "petRejected", reason: "full" }, "mira", "牠已經吃得飽飽的了。", true],
  [{ kind: "petRejected", reason: "tooSoon" }, "mira", "慢慢來，讓牠先嚼完這顆。", true],
  [{ kind: "petRejected", reason: "petUnknown" }, "mira", "咦，剛剛沒弄好……再試一次吧？", true],
  [{ kind: "petFailed" }, "mira", "咦，剛剛沒弄好……再試一次吧？", true],
  [{ kind: "hired", name: "F級傭兵" }, "barton", "契約簽好了，F級傭兵歸你使喚。", false],
  [
    { kind: "deployed", name: "葛雷", place: "迷霧森林" },
    "barton",
    "葛雷往迷霧森林出發了。",
    false,
  ],
  [{ kind: "rested", name: "葛雷" }, "barton", "葛雷回來歇口氣了。", false],
  [{ kind: "dismissed", name: "葛雷" }, "barton", "葛雷的契約，我收回來了。", false],
  [{ kind: "mercenaryRenamed", name: "葛雷" }, "barton", "葛雷？行，名冊上改好了。", false],
  [
    { kind: "mercenaryRejected", reason: "notEnoughMoney" },
    "barton",
    "錢不夠，這張契約簽不了。",
    true,
  ],
  [
    { kind: "mercenaryRejected", reason: "mercenaryHired", contractName: "葛雷" },
    "barton",
    "一次只接一份契約，先把葛雷的處理掉。",
    true,
  ],
  [
    { kind: "mercenaryRejected", reason: "honourOnly" },
    "barton",
    "那兩位得去勳章所換，我這兒不賣。",
    true,
  ],
  [
    { kind: "mercenaryRejected", reason: "placeRestricted" },
    "barton",
    "那裡是禁區，我的人不去送死。",
    true,
  ],
  [
    { kind: "mercenaryRejected", reason: "nameInvalid" },
    "barton",
    "這名字名冊上寫不了，換一個。",
    true,
  ],
  [{ kind: "mercenaryFailed" }, "barton", "嘖，文書出了點差錯，再來一次。", true],
  [{ kind: "stableLoadFailed" }, "mira", "咦，欄裡的名簿一時找不到……", true],
  [{ kind: "hallLoadFailed" }, "barton", "嘖，名冊一時翻不到……", true],
])("keeperLine_%#", (event, keeper, text, failed) => {
  expect(keeperLine(event)).toEqual({ keeper, text, failed })
})

// 規格 companions-screen：Entrance with a carried pet and a fighting mercenary
test("Greetings_ForACarriedPetAndAFightingMercenary", () => {
  const pets = [makePet({ id: 1, name: "小風", carried: true }), makePet({ id: 2, name: "夜影" })]

  expect(miraGreeting(pets)).toBe("小風今天也很有精神喔！要來看看牠們嗎？")
  expect(stableChip(makePets(pets))).toBe("寵物 2 / 3・小風 攜帶中")
  expect(bartonGreeting(contract())).toBe("葛雷還在迷霧森林幹活，契約我幫你顧著。")
  expect(hallChip(contract())).toBe("葛雷・戰鬥中")
})

// 上限以伺服器給的 maxPets 為準
test("StableChip_UsesTheServerLimit", () => {
  expect(stableChip(makePets([makePet({ name: "夜影" })], { maxPets: 4 }))).toBe("寵物 1 / 4")
})

test("Greetings_WithoutPetsOrContract", () => {
  expect(miraGreeting([])).toBe("獸欄空蕩蕩的……抓到寵物就帶來給我照顧吧！")
  expect(stableChip(makePets([]))).toBe("寵物 0 / 3")
  expect(miraGreeting([makePet({ name: "夜影" })])).toBe("孩子們都在欄裡等你呢，今天要帶誰出門？")
  expect(bartonGreeting(null)).toBe("想找人手？名冊上的傢伙個個都能打。")
  expect(hallChip(null)).toBe("沒有契約")
  expect(bartonGreeting(contract({ fighting: false }))).toBe("葛雷在後頭磨刀，隨時可以再派出去。")
  expect(hallChip(contract({ fighting: false }))).toBe("葛雷・休息中")
})

// 規格 companions-screen：counter top 表
test.each([
  [624, 231],
  [683, 258],
  [720, 274],
  [780, 301],
])("companionsCounterTop_%i", (height, top) => {
  expect(companionsCounterTop(height)).toBe(top)
})

// 規格 companions-screen：Bars for a pet（飽腹 72、親密 312、出擊率 35）
test("Ratios_CapAtFull", () => {
  expect(satietyRatio(72)).toBeCloseTo(0.72)
  expect(satietyRatio(130)).toBe(1)
  expect(intimacyRatio(312)).toBe(1)
  expect(intimacyRatio(51)).toBeCloseTo(0.2)
  expect(activationRatio(35)).toBe(1)
  expect(activationRatio(7)).toBeCloseTo(0.2)
})

// 規格 companions-screen：prices 表
test.each([
  [500, "500"],
  [100_000, "10萬"],
  [1_000_000, "100萬"],
])("shortPrice_%i", (price, text) => {
  expect(shortPrice(price)).toBe(text)
})

// 規格 companions-screen：Feeding a fruit（親密 88 → 92）
test("FeedEffect_ShowsTheStatAndIntimacyGained", () => {
  const before = makePet({ attack: 30, intimacy: 88 })
  const after = { ...before, attack: 32, intimacy: 92 }

  expect(feedEffect(before, after, smallVigor)).toBe("AT +2　♥ +4")
})

// 規格 companions-screen：Deployable places——依大陸分組、排除禁區、附建議等級
test("DeployGroups_GroupByContinentWithoutRestricted", () => {
  const groups = deployGroups(makeWorld())

  expect(groups.map((group) => group.name)).toEqual(["中央大陸", "魔法王國", "禁區"])
  expect(groups[0]!.places.map((place) => [place.name, place.level])).toEqual([
    ["中央平原", "Lv 1–10"],
    ["黑暗沼澤", "Lv 10–20"],
  ])
  expect(groups.flatMap((group) => group.places).map((place) => place.name)).not.toContain(
    "廢棄洞窟",
  )
})

// 規格 companions-screen：Fruits disabled
test("FruitDisabled_WhenCoolingFullOrPoor", () => {
  const now = new Date("2026-11-14T08:00:00Z")
  const ready = makePet({ satiety: 50, nextFeedAt: new Date(now.getTime() - 1000) })

  expect(fruitDisabled(ready, smallVigor, 100, now)).toBe(false)
  expect(
    fruitDisabled({ ...ready, nextFeedAt: new Date(now.getTime() + 3000) }, smallVigor, 100, now),
  ).toBe(true)
  expect(fruitDisabled({ ...ready, satiety: 100 }, smallVigor, 100, now)).toBe(true)
  expect(fruitDisabled(ready, smallVigor, 19, now)).toBe(true)
})

const entry = (price: number | null): MercenaryCatalogueEntry => ({
  id: "mercenary-1",
  name: "F級傭兵",
  price,
  fights: 850,
  moneyPercent: 30,
  dropPercent: 30,
  experiencePercent: 65,
  deathCost: 5,
})

test("CanHire_NeedsPriceMoneyAndNoContract", () => {
  expect(canHire(entry(500), null, 500)).toBe(true)
  expect(canHire(entry(500), null, 499)).toBe(false)
  expect(canHire(entry(500), contract(), 1000)).toBe(false)
  expect(canHire(entry(null), null, 10_000_000)).toBe(false)
})

// 冷卻金線從已經過去的進度接著縮：餵完 4 秒才看到的線，剩 6 秒
test.each([
  [6_000, 4_000],
  [10_000, 0],
  [0, 10_000],
  [-3_000, 10_000],
  [12_000, 0],
])("FeedCooldownElapsed_FromTheRemainingTime %#", (remaining, elapsed) => {
  const now = new Date("2026-11-14T08:00:00Z")
  expect(feedCooldownElapsed(new Date(now.getTime() + remaining), now)).toBe(elapsed)
})
