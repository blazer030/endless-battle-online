import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError } from "../../api/client"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import mercenaryHelmet from "../../assets/honour/mercenary-helmet.webp"
import keyPlaceholder from "../../assets/placeholders/items/key.svg"
import materialPlaceholder from "../../assets/placeholders/items/material.svg"
import weaponPlaceholder from "../../assets/placeholders/items/weapon.svg"
import { HonourScreen } from "./HonourScreen"
import {
  createHonourApi,
  demoExchanges,
  itemExchange,
  medals,
  mercenaryExchange,
  shopWith,
} from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderHonour = async (api = createHonourApi(), onBack = vi.fn()) => {
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <HonourScreen
        api={api}
        character={makeCharacter()}
        onCharacterChange={vi.fn()}
        onBack={onBack}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack }
}

const medalCase = () => screen.getByRole("group", { name: "持有勳章" })
const medalButton = (name: string) =>
  within(medalCase()).getByRole("button", { name: new RegExp(`^${name}`) })
const clerk = () => screen.getByTestId("clerk-line")

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 honour-screen：Counter with medals and a contract
test("CounterShowsMedalCountsAndGreeting", async () => {
  await renderHonour(createHonourApi(shopWith({ hiredMercenaryName: "葛雷" })))

  expect(
    within(medalCase())
      .getAllByRole("button")
      .map((button) => button.textContent),
  ).toEqual(["14", "6", "3", "0", "2"])
  expect(screen.getByText("僱用中").parentElement).toHaveTextContent("僱用中葛雷")
  expect(clerk().textContent).toBe("歡迎來到勳章所！\n今天想兌換些什麼呢？")
})

// 規格 honour-screen：Counter without medals
test("CounterWithoutMedals", async () => {
  await renderHonour(
    createHonourApi(shopWith({ medals: medals.map((medal) => ({ ...medal, owned: 0 })) })),
  )

  expect(clerk().textContent).toBe("啊，您還沒有勳章呢……\n有了再來找我喔！")
  expect(medalButton("大陸勳章")).toBeEnabled()
  expect(screen.queryByText("僱用中")).not.toBeInTheDocument()
})

// 規格 honour-screen：Back from the rack and the counter
test("MedalLowersTheRackAndBackReturns", async () => {
  const { onBack } = await renderHonour()

  fireEvent.click(medalButton("王國勳章"))
  expect(screen.getByRole("region", { name: "王國勳章陳列板" })).toBeVisible()
  expect(screen.queryByTestId("clerk-line")).not.toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  expect(screen.queryByRole("region", { name: /陳列板$/ })).not.toBeInTheDocument()
  expect(clerk().textContent).toBe("慢慢挑沒關係，\n陳列板不會跑掉的～")

  fireEvent.click(screen.getByRole("button", { name: "公會前" }))
  expect(onBack).toHaveBeenCalledOnce()
})

// 規格 honour-screen：Honour loading and load failure
test("LoadingHidesCountsAndBlocksMedals", async () => {
  const api = createHonourApi()
  api.getHonourShop = vi.fn().mockReturnValue(new Promise(() => undefined))
  await renderHonour(api)

  expect(clerk().textContent).toBe("歡迎來到勳章所！\n我翻一下您的帳冊喔……")
  const buttons = within(medalCase()).getAllByRole("button")
  expect(buttons).toHaveLength(5)
  buttons.forEach((button) => {
    expect(button).toBeDisabled()
    expect(button.textContent?.trim()).toBe("")
  })
})

// 規格 honour-screen：Retry after a load failure
test("RetryAfterLoadFailure", async () => {
  const api = createHonourApi()
  api.getHonourShop = vi
    .fn()
    .mockRejectedValueOnce(new Error("offline"))
    .mockResolvedValueOnce(shopWith())
  await renderHonour(api)

  expect(clerk().textContent).toBe("哎呀，帳冊一時\n翻不到了……")
  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(screen.queryByRole("button", { name: "再試一次" })).not.toBeInTheDocument()
  expect(medalButton("大陸勳章")).toHaveTextContent("14")
  expect(clerk().textContent).toBe("歡迎來到勳章所！\n今天想兌換些什麼呢？")
})

// 規格 honour-screen：No adventure requests
test("MakesNoAdventureRequests", async () => {
  const { api } = await renderHonour()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 3)
  })

  expect(api.advance).not.toHaveBeenCalled()
})

const openRack = async (
  name: string,
  exchanges = demoExchanges,
  hiredMercenaryName: string | null = null,
) => {
  await renderHonour(createHonourApi(shopWith({ exchanges, hiredMercenaryName })))
  fireEvent.click(medalButton(name))
  return screen.getByRole("region", { name: `${name}陳列板` })
}

const tagNames = (rack: HTMLElement) =>
  within(rack)
    .getAllByRole("button")
    .map((tag) => tag.getAttribute("aria-label"))

// 規格 honour-screen：Display rack 的 silver rack order
test("RackListsTagsBySectionAndQuantity", async () => {
  const silver: [string, number][] = [["item-2222", 1]]
  const rack = await openRack("白銀勳章", [
    itemExchange("134", "110000HP回復劑", "Consumable", [["item-2222", 3]], { quantity: 10 }),
    itemExchange("133", "90000HP回復劑", "Consumable", silver, { quantity: 10 }),
    itemExchange("142", "劣質打孔石", "Material", silver, { quantity: 3 }),
    itemExchange("135", "魔法捕捉器", "Consumable", [["item-2222", 2]], { quantity: 5 }),
    itemExchange("large-stone-box", "大魔石寶箱", "Consumable", silver),
  ])

  expect(
    within(rack)
      .getAllByRole("heading", { level: 4 })
      .map((heading) => heading.textContent),
  ).toEqual(["道具", "材料"])
  expect(tagNames(rack)).toEqual([
    "90000HP回復劑 ×10",
    "大魔石寶箱",
    "魔法捕捉器 ×5",
    "110000HP回復劑 ×10",
    "劣質打孔石 ×3",
  ])
})

test("ExchangeWithSeveralMedalsAppearsOnEachRack", async () => {
  const rack = await openRack("王國勳章")

  expect(tagNames(rack)).toEqual(["C級傭兵", "靜止鑰匙", "劣質打孔石 ×3"])
  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  fireEvent.click(medalButton("王城勳章"))
  expect(tagNames(screen.getByRole("region", { name: "王城勳章陳列板" }))).toEqual([
    "靜止鑰匙",
    "劣質打孔石 ×3",
  ])
})

test("UnaffordableAndHiredTagsAreDimmedButOpen", async () => {
  const rack = await openRack("大陸勳章", demoExchanges, "葛雷")
  const tag = (name: string) => within(rack).getByRole("button", { name: new RegExp(`^${name}`) })

  expect(tag("C級傭兵")).toHaveAttribute("data-dimmed", "true")
  expect(tag("巨石槌")).toHaveAttribute("data-dimmed", "false")
  expect(tag("劣質打孔石")).toHaveAttribute("data-dimmed", "true")
  expect(tag("C級傭兵")).toBeEnabled()
  // 不夠的數量標紅
  expect(
    within(tag("C級傭兵")).getByText("×13", { selector: ".honour-tag__short" }),
  ).toBeInTheDocument()
})

test("TagShowsQuantityBadge", async () => {
  const rack = await openRack("白銀勳章")
  const potion = within(rack).getByRole("button", { name: /^90000HP回復劑/ })

  expect(within(potion).getByText("×10")).toBeInTheDocument()
  expect(
    within(within(rack).getByRole("button", { name: /^劣質打孔石/ })).getByText("×3"),
  ).toBeInTheDocument()
})

// 規格 honour-screen：Item without a content image 與 pictures by reward
test("ItemWithoutImageUsesCategoryPlaceholder", async () => {
  const rack = await openRack("大陸勳章")
  const picture = (name: string) =>
    within(rack)
      .getByRole("button", { name: new RegExp(`^${name}`) })
      .querySelector("img")

  expect(picture("巨石槌")?.getAttribute("src")).toBe(weaponPlaceholder)
  expect(picture("劣質打孔石")?.getAttribute("src")).toBe(materialPlaceholder)
  expect(picture("C級傭兵")?.getAttribute("src")).toBe(mercenaryHelmet)
  const costMedal = within(rack)
    .getByRole("button", { name: /^巨石槌/ })
    .querySelector('[data-asset="items/item-1304"]')
  expect(costMedal).not.toBeNull()
  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  fireEvent.click(medalButton("王國勳章"))
  expect(
    within(screen.getByRole("region", { name: "王國勳章陳列板" }))
      .getByRole("button", { name: "靜止鑰匙" })
      .querySelector("img")
      ?.getAttribute("src"),
  ).toBe(keyPlaceholder)
})

const openSheet = async (
  medal: string,
  tag: string,
  api = createHonourApi(shopWith({ exchanges: demoExchanges })),
) => {
  await renderHonour(api)
  fireEvent.click(medalButton(medal))
  fireEvent.click(
    within(screen.getByRole("region", { name: `${medal}陳列板` })).getByRole("button", {
      name: new RegExp(`^${tag}`),
    }),
  )
  return { api, sheet: screen.getByRole("dialog", { name: tag }) }
}

const seal = (sheet: HTMLElement) => within(sheet).getByRole("button", { name: "兌換" })
const said = (sheet: HTMLElement) => within(sheet).queryByTestId("clerk-say")

// 規格 honour-screen：Equipment with an unmet requirement
test("SheetShowsEquipmentDetailsAndRequirementColours", async () => {
  const { sheet } = await openSheet("大陸勳章", "刺心者")

  expect(within(sheet).getByText("武器・綁定")).toBeInTheDocument()
  const requirement = within(sheet).getByTestId("requirement-strength")
  expect(requirement).toHaveTextContent("力量435")
  expect(requirement).toHaveAttribute("data-met", "false")
  expect(within(sheet).getByText("物攻")).toBeInTheDocument()
  expect(within(sheet).getByText("孔 2・精鍊上限 +4")).toBeInTheDocument()
  expect(within(sheet).getByTestId("pay-item-1304")).toHaveTextContent("持有 14／需要 9")
  expect(seal(sheet)).toBeEnabled()
})

// 規格 honour-screen：Effects of consumables and keys
test("SheetShowsEffectsForConsumablesAndKeys", async () => {
  const { sheet } = await openSheet("白銀勳章", "90000HP回復劑")
  expect(within(sheet).getByText("補給欄自動使用：HP 回復 90,000")).toBeInTheDocument()
  expect(within(sheet).getByText("×10")).toBeInTheDocument()
  expect(within(sheet).getByText("道具")).toBeInTheDocument()

  fireEvent.click(within(sheet).getByRole("button", { name: "關閉" }))
  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  fireEvent.click(medalButton("王國勳章"))
  fireEvent.click(
    within(screen.getByRole("region", { name: "王國勳章陳列板" })).getByRole("button", {
      name: "靜止鑰匙",
    }),
  )
  expect(
    within(screen.getByRole("dialog", { name: "靜止鑰匙" })).getByText("開啟「靜止之城」24 小時"),
  ).toBeInTheDocument()
})

test("SheetShowsMercenaryContract", async () => {
  const { sheet } = await openSheet("大陸勳章", "C級傭兵")

  expect(within(sheet).getByText("傭兵")).toBeInTheDocument()
  expect(within(sheet).getByText("900 次")).toBeInTheDocument()
  expect(within(sheet).getByText("戰敗消耗")).toBeInTheDocument()
  expect(within(sheet).getByTestId("pay-item-1305")).toHaveTextContent("持有 6／需要 13")
  expect(within(sheet).getByTestId("pay-item-1305").querySelector("[data-short]")).toHaveAttribute(
    "data-short",
    "true",
  )
})

test("SealDisabledWhenShortOrHired", async () => {
  const short = await openSheet("大陸勳章", "C級傭兵")
  expect(seal(short.sheet)).toBeDisabled()
  expect(within(short.sheet).getByText("勳章不足")).toBeInTheDocument()
  cleanup()

  const rich = [mercenaryExchange("1", "C級傭兵", [["item-1304", 13]]), ...demoExchanges.slice(1)]
  const hired = await openSheet(
    "大陸勳章",
    "C級傭兵",
    createHonourApi(shopWith({ exchanges: rich, hiredMercenaryName: "葛雷" })),
  )
  expect(seal(hired.sheet)).toBeDisabled()
  expect(within(hired.sheet).getByText("僱用中：葛雷")).toBeInTheDocument()
})

test("SendingBlocksTheSheet", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockReturnValue(new Promise(() => undefined))
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)

  fireEvent.click(seal(sheet))
  await flush()

  expect(seal(sheet)).toHaveAttribute("aria-busy", "true")
  expect(seal(sheet)).toBeDisabled()
  expect(within(sheet).getByRole("button", { name: "關閉" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "櫃檯" })).toBeDisabled()
  within(medalCase())
    .getAllByRole("button")
    .forEach((medal) => expect(medal).toBeDisabled())
  expect(api.exchangeHonour).toHaveBeenCalledExactlyOnceWith("9")
})

// 修正：送出中按「櫃檯」換到別張吊牌，先前那筆的結果不能說在別張羊皮紙裡
test("LateResultIsNotSpokenInAnotherSheet", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  let finish: (shop: ReturnType<typeof shopWith>) => void = () => undefined
  vi.mocked(api.exchangeHonour).mockReturnValue(
    new Promise((resolve) => {
      finish = resolve
    }),
  )
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)
  fireEvent.click(seal(sheet))
  await flush()

  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))
  fireEvent.click(medalButton("大陸勳章"))
  fireEvent.click(screen.getByRole("button", { name: /^破山斧/ }))
  finish(shopWith({ exchanges: demoExchanges }))
  await flush()

  expect(screen.queryByRole("dialog", { name: "破山斧" })).not.toBeInTheDocument()
  expect(said(screen.getByRole("dialog", { name: "巨石槌" }))).toHaveTextContent(
    "換好了！已經幫您放進背包囉。",
  )
})

// 規格 honour-screen：Exchanging twice in a row
test("ExchangingTwiceInARow", async () => {
  const potion = (owned: number) =>
    shopWith({
      medals: medals.map((medal) => (medal.itemId === "item-2222" ? { ...medal, owned } : medal)),
      exchanges: [
        {
          ...demoExchanges[5]!,
          cost: [{ itemId: "item-2222", name: "白銀勳章", quantity: 1, owned }],
          affordable: owned >= 1,
        },
      ],
    })
  const api = createHonourApi(potion(2))
  const latest = makeCharacter({ money: 77 })
  vi.mocked(api.getMyCharacter).mockResolvedValue(latest)
  vi.mocked(api.exchangeHonour).mockResolvedValueOnce(potion(1)).mockResolvedValueOnce(potion(0))
  const onCharacterChange = vi.fn()
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <HonourScreen
        api={api}
        character={makeCharacter()}
        onCharacterChange={onCharacterChange}
        onBack={vi.fn()}
      />
    </StageContext>,
  )
  await flush()
  fireEvent.click(medalButton("白銀勳章"))
  fireEvent.click(screen.getByRole("button", { name: /^90000HP回復劑/ }))
  const sheet = screen.getByRole("dialog", { name: "90000HP回復劑" })

  fireEvent.click(seal(sheet))
  await flush()
  expect(within(sheet).getByTestId("pay-item-2222")).toHaveTextContent("持有 1／需要 1")
  expect(said(sheet)).toHaveTextContent("換好了！已經幫您放進背包囉。")
  expect(seal(sheet)).toBeEnabled()
  expect(onCharacterChange).toHaveBeenCalledWith(latest)

  fireEvent.click(seal(sheet))
  await flush()
  expect(within(sheet).getByTestId("pay-item-2222")).toHaveTextContent("持有 0／需要 1")
  expect(seal(sheet)).toBeDisabled()
  expect(medalButton("白銀勳章")).toHaveTextContent("0")
})

test("MercenaryExchangeNamesTheContract", async () => {
  const rich = [mercenaryExchange("1", "C級傭兵", [["item-1304", 13]])]
  const api = createHonourApi(shopWith({ exchanges: rich }))
  vi.mocked(api.exchangeHonour).mockResolvedValue(
    shopWith({ exchanges: rich, hiredMercenaryName: "C級傭兵" }),
  )
  const { sheet } = await openSheet("大陸勳章", "C級傭兵", api)

  fireEvent.click(seal(sheet))
  await flush()

  expect(said(sheet)).toHaveTextContent("C級傭兵的契約簽好囉，祝您順利！")
  expect(seal(sheet)).toBeDisabled()
  expect(screen.getByText("僱用中").parentElement).toHaveTextContent("僱用中C級傭兵")
})

// 規格 honour-screen：Bag full names the category
test("BagFullNamesTheCategory", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockRejectedValue(new ApiError(400, "無法兌換", ["bagFull"]))
  const { sheet } = await openSheet("大陸勳章", "破山斧", api)

  fireEvent.click(seal(sheet))
  await flush()

  expect(said(sheet)).toHaveTextContent("哎呀，武器的格子已經滿了……")
  expect(said(sheet)).toHaveAttribute("data-failed", "true")
})

test("RefusalReloadsTheListing", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockRejectedValue(new ApiError(400, "無法兌換", ["medalsMissing"]))
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)

  fireEvent.click(seal(sheet))
  await flush()

  expect(said(sheet)).toHaveTextContent("勳章好像還差一點呢……")
  expect(api.getHonourShop).toHaveBeenCalledTimes(2)
  expect(screen.getByRole("dialog", { name: "巨石槌" })).toBeInTheDocument()
})

test("NetworkFailureIsSpoken", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockRejectedValue(new Error("offline"))
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)

  fireEvent.click(seal(sheet))
  await flush()

  expect(said(sheet)).toHaveTextContent("咦，剛剛沒送出去……再試一次吧？")
})

test("NextActionClearsTheLine", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockRejectedValue(new Error("offline"))
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)
  fireEvent.click(seal(sheet))
  await flush()
  expect(said(sheet)).not.toBeNull()

  fireEvent.click(within(sheet).getByRole("button", { name: "關閉" }))
  fireEvent.click(screen.getByRole("button", { name: /^巨石槌/ }))

  expect(said(screen.getByRole("dialog", { name: "巨石槌" }))).toBeNull()
})

// 規格 honour-screen：換過再回櫃檯，莉瑟道謝
test("BackAfterExchangingThanks", async () => {
  const api = createHonourApi(shopWith({ exchanges: demoExchanges }))
  vi.mocked(api.exchangeHonour).mockResolvedValue(shopWith({ exchanges: demoExchanges }))
  const { sheet } = await openSheet("大陸勳章", "巨石槌", api)

  fireEvent.click(seal(sheet))
  await flush()
  fireEvent.click(within(sheet).getByRole("button", { name: "關閉" }))
  fireEvent.click(screen.getByRole("button", { name: "櫃檯" }))

  expect(clerk().textContent).toBe("兌換好了！\n祝您冒險一路順風。")
})
