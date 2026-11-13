import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { useState } from "react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import {
  MercenaryHireVo,
  type MercenaryCatalogueEntry,
  type MercenaryContract,
} from "../../game/mercenaries"
import { makeCharacter, makeMercenaryOverview, makePets } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { HallPage } from "./HallPage"
import { createCompanionsApi, fighting } from "./testing"

const grade = (
  id: string,
  name: string,
  price: number | null,
  fights: number,
  share: [number, number, number],
  deathCost: number,
): MercenaryCatalogueEntry => ({
  id,
  name,
  price,
  fights,
  moneyPercent: share[0],
  dropPercent: share[1],
  experiencePercent: share[2],
  deathCost,
})

// 正式內容的五級（mercenaries.json）
const catalogue = [
  grade("mercenary-1", "F級傭兵", 500, 850, [30, 30, 65], 5),
  grade("mercenary-2", "E級傭兵", 100_000, 900, [40, 40, 75], 4),
  grade("mercenary-3", "D級傭兵", 1_000_000, 950, [50, 50, 85], 3),
  grade("mercenary-4", "C級傭兵", null, 900, [70, 70, 100], 2),
  grade("mercenary-5", "B級傭兵", null, 900, [80, 80, 100], 1),
]

const contract = (overrides: Partial<MercenaryContract> = {}): MercenaryContract => ({
  ...fighting,
  mercenaryId: "mercenary-1",
  remaining: 640,
  ...overrides,
})

const overview = (current: MercenaryContract | null) =>
  makeMercenaryOverview({ catalogue, contract: current })

const Harness = ({
  api,
  initial,
}: {
  api: ReturnType<typeof createCompanionsApi>
  initial: CharacterVo
}) => {
  const [current, setCurrent] = useState(initial)
  return (
    <StageContext value={{ height: 624, scale: 1 }}>
      <HallPage api={api} character={current} onCharacterChange={setCurrent} onBack={vi.fn()} />
    </StageContext>
  )
}

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderHall = async ({
  current = null as MercenaryContract | null,
  money = 1000,
  api = createCompanionsApi(makePets([]), overview(current)),
} = {}) => {
  render(<Harness api={api} initial={makeCharacter({ money })} />)
  await flush()
  return api
}

const note = (name: string) => screen.getByRole("button", { name: new RegExp(`^${name}`) })
const sheet = () => screen.getByRole("dialog", { name: /級傭兵|冒險地/ })
const say = () => screen.getAllByRole("status").at(-1)!

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 companions-screen：prices 表與勳章
test("RosterShowsPricesAndMedals", async () => {
  await renderHall({ money: 50_000 })

  const roster = screen.getByRole("list", { name: "傭兵名冊" })
  expect(within(roster).getAllByRole("button")).toHaveLength(5)
  expect(note("F級傭兵")).toHaveTextContent("500")
  expect(note("E級傭兵")).toHaveTextContent("10萬")
  expect(note("D級傭兵")).toHaveTextContent("100萬")
  expect(note("E級傭兵").querySelector("[data-short='true']")).not.toBeNull()
  const medals = note("C級傭兵").querySelectorAll("img[data-asset]")
  expect([...medals].map((image) => image.getAttribute("data-asset"))).toEqual([
    "items/item-1304",
    "items/item-1305",
  ])
})

// 規格 companions-screen：Mercenary hall——錢不夠、已有契約時印章停用
test("HireSheetDisablesWithoutMoneyOrWithContract", async () => {
  await renderHall({ money: 400 })

  fireEvent.click(note("F級傭兵"))
  expect(within(sheet()).getByRole("button", { name: "僱用" })).toBeDisabled()
  expect(within(sheet()).getByText("可戰鬥 850 次")).toBeInTheDocument()
})

test("HireSheetWithContractExplainsAndDisables", async () => {
  await renderHall({ current: contract(), money: 1_000_000 })

  fireEvent.click(note("E級傭兵"))
  expect(within(sheet()).getByRole("button", { name: "僱用" })).toBeDisabled()
  expect(sheet()).toHaveTextContent("已經和葛雷簽約了，一次只能僱用一位")
})

test("HonourOnlyHasNoSeal", async () => {
  await renderHall({ money: 10_000_000 })

  fireEvent.click(note("C級傭兵"))
  expect(within(sheet()).queryByRole("button", { name: "僱用" })).not.toBeInTheDocument()
  expect(sheet()).toHaveTextContent("只能在勳章所兌換")
})

// 規格 companions-screen：lines 表的僱用
test("HiringIsSpokenInTheHeader", async () => {
  const api = await renderHall()
  vi.mocked(api.hireMercenary).mockResolvedValue(
    MercenaryHireVo.create({
      mercenaries: overview(
        contract({
          name: "F級傭兵",
          remaining: 850,
          fighting: false,
          placeId: null,
          placeName: null,
        }),
      ),
      character: makeCharacter({ money: 500 }),
    }),
  )

  fireEvent.click(note("F級傭兵"))
  fireEvent.click(within(sheet()).getByRole("button", { name: "僱用" }))
  await flush()

  expect(api.hireMercenary).toHaveBeenCalledWith("mercenary-1")
  expect(screen.queryByRole("dialog", { name: /級傭兵/ })).not.toBeInTheDocument()
  expect(say()).toHaveTextContent("契約簽好了，F級傭兵歸你使喚。")
  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("500")
})

// 規格 companions-screen：Keepers speak——羊皮紙打開時在羊皮紙裡說
test("SecondHireRejectionIsSpokenInTheSheet", async () => {
  const api = await renderHall({ money: 1000 })
  vi.mocked(api.hireMercenary).mockRejectedValue(new ApiError(400, "不行", ["mercenaryHired"]))

  fireEvent.click(note("F級傭兵"))
  fireEvent.click(within(sheet()).getByRole("button", { name: "僱用" }))
  await flush()

  expect(within(sheet()).getByRole("status")).toHaveTextContent("一次只接一份契約")
  expect(within(sheet()).getByRole("status")).toHaveClass("keeper-say--failed")
})

// 規格 companions-screen：Mercenary hall——剩餘次數條的總數是名冊次數
test("ContractShowsRemainingOutOfCatalogueFights", async () => {
  await renderHall({ current: contract({ remaining: 425 }) })

  const card = screen.getByRole("region", { name: "契約" })
  expect(card).toHaveTextContent("葛雷")
  expect(card).toHaveTextContent("戰鬥中")
  expect(card).toHaveTextContent("425")
  expect(card).toHaveTextContent("/ 850")
  expect(within(card).getByRole("progressbar", { name: "剩餘次數" })).toHaveStyle({ width: "50%" })
  expect(card).toHaveTextContent("迷霧森林")
  expect(card).toHaveTextContent("−5")
  expect(note("F級傭兵")).toHaveTextContent("僱用中")
})

// 規格 companions-screen：Deployable places
test("DeploySheetGroupsPlacesWithoutRestricted", async () => {
  const api = await renderHall({
    current: contract({ fighting: false, placeId: null, placeName: null }),
  })

  fireEvent.click(screen.getByRole("button", { name: "派遣" }))
  await flush()

  expect(api.getWorld).toHaveBeenCalled()
  const places = within(sheet()).getByRole("group", { name: "冒險地" })
  expect(within(places).getByText("中央大陸")).toBeInTheDocument()
  expect(within(places).getByRole("button", { name: /^中央平原/ })).toHaveTextContent("Lv 1–10")
  expect(within(places).queryByRole("button", { name: /^廢棄洞窟/ })).not.toBeInTheDocument()
})

// 規格 companions-screen：lines 表的派遣與召回
test("DeployAndRestAreSpoken", async () => {
  const resting = contract({ fighting: false, placeId: null, placeName: null })
  const api = await renderHall({ current: resting })
  vi.mocked(api.deployMercenary).mockResolvedValue(
    overview(contract({ placeId: 1, placeName: "中央平原" })),
  )

  fireEvent.click(screen.getByRole("button", { name: "派遣" }))
  await flush()
  fireEvent.click(within(sheet()).getByRole("button", { name: /^中央平原/ }))
  fireEvent.click(within(sheet()).getByRole("button", { name: "出發" }))
  await flush()

  expect(api.deployMercenary).toHaveBeenCalledWith(1)
  expect(say()).toHaveTextContent("葛雷往中央平原出發了。")

  vi.mocked(api.restMercenary).mockResolvedValue(overview(resting))
  fireEvent.click(screen.getByRole("button", { name: "召回休息" }))
  await flush()
  expect(say()).toHaveTextContent("葛雷回來歇口氣了。")
})

test("RestrictedPlaceRejectionIsSpoken", async () => {
  const api = await renderHall({ current: contract({ fighting: false }) })
  vi.mocked(api.deployMercenary).mockRejectedValue(new ApiError(400, "禁區", ["placeRestricted"]))

  fireEvent.click(screen.getByRole("button", { name: "派遣" }))
  await flush()
  fireEvent.click(within(sheet()).getByRole("button", { name: /^中央平原/ }))
  fireEvent.click(within(sheet()).getByRole("button", { name: "出發" }))
  await flush()

  expect(within(sheet()).getByRole("status")).toHaveTextContent("那裡是禁區，我的人不去送死。")
})

// 規格 companions-screen：lines 表的解約
test("DismissNeedsConfirmation", async () => {
  const api = await renderHall({ current: contract() })
  vi.mocked(api.dismissMercenary).mockResolvedValue(overview(null))

  fireEvent.click(screen.getByRole("button", { name: "解約" }))
  expect(api.dismissMercenary).not.toHaveBeenCalled()
  fireEvent.click(screen.getByRole("button", { name: "確定解約" }))
  await flush()

  expect(api.dismissMercenary).toHaveBeenCalled()
  expect(say()).toHaveTextContent("葛雷的契約，我收回來了。")
  expect(screen.queryByRole("region", { name: "契約" })).not.toBeInTheDocument()
})

// 規格 companions-screen：傭兵改名 n / 8
test("RenameMercenaryCountsToEight", async () => {
  const api = await renderHall({ current: contract() })
  vi.mocked(api.renameMercenary).mockResolvedValue(overview(contract({ name: "老葛" })))

  fireEvent.click(screen.getByRole("button", { name: "改名" }))
  const input = screen.getByRole("textbox", { name: "傭兵名字" })
  expect(input).toHaveAttribute("maxLength", "8")
  fireEvent.change(input, { target: { value: "老葛" } })
  fireEvent.click(screen.getByRole("button", { name: "確定" }))
  await flush()

  expect(api.renameMercenary).toHaveBeenCalledWith("老葛")
  expect(say()).toHaveTextContent("老葛？行，名冊上改好了。")
})

// 規格 companions-screen：Companions load failure（傭兵所）
test("HallLoadFailureShowsOnlyTheRetrySeal", async () => {
  const api = createCompanionsApi(makePets([]), overview(contract()))
  vi.mocked(api.getMercenaries).mockRejectedValueOnce(new TypeError("Failed to fetch"))
  await renderHall({ api })

  expect(say()).toHaveTextContent("嘖，名冊一時翻不到……")
  expect(screen.queryByRole("list", { name: "傭兵名冊" })).not.toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(screen.getByRole("list", { name: "傭兵名冊" })).toBeInTheDocument()
  expect(screen.getByRole("region", { name: "契約" })).toBeInTheDocument()
})
