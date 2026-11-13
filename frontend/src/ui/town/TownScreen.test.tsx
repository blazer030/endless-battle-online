import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../../api/client"
import { BuffVo } from "../../game/buffs"
import { InnVo } from "../../game/inn"
import { BuffTickMilliseconds, PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { TownScreen, type TownApi } from "./TownScreen"
import roomBadgesPlaceholder from "../../assets/placeholders/room-badges.svg"

const character = makeCharacter({
  name: "收尾者",
  jobId: "job-7",
  jobName: "術士",
  sex: 2,
  level: 6,
  healthPoints: 317,
  manaPoints: 88,
  money: 12345,
  stats: { ...makeCharacter().stats, maxHealthPoints: 600, maxManaPoints: 106 },
})

const createApi = (): TownApi & Pick<ApiClient, "advance"> => ({
  getBuffs: vi.fn().mockResolvedValue([]),
  getInn: vi.fn().mockResolvedValue({ rooms: [], lodging: null, restFee: 0 }),
  advance: vi.fn(),
})

const renderTown = ({
  scale = 1,
  api = createApi(),
  onOpen = vi.fn(),
  initialSegment = 0 as 0 | 1 | 2,
} = {}) => {
  render(
    <StageContext value={{ height: 624, scale }}>
      <TownScreen api={api} character={character} onOpen={onOpen} initialSegment={initialSegment} />
    </StageContext>,
  )
  return { api, onOpen }
}

const world = () => screen.getByTestId("town-world")
const segmentOf = () => world().dataset.segment

const swipe = (target: Element, fromX: number, toX: number, deltaY = 0) => {
  fireEvent.pointerDown(target, { clientX: fromX, clientY: 300 })
  fireEvent.pointerUp(target, { clientX: toX, clientY: 300 + deltaY })
}

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date("2026-11-12T08:00:00Z"))
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 Status shows the entered character
test("TownScreen_ShowsTheGateWithTheEnteredCharacter", () => {
  renderTown()

  const status = screen.getByRole("region", { name: "角色狀態" })
  expect(within(status).getByText("收尾者")).toBeInTheDocument()
  expect(within(status).getByText("術士")).toBeInTheDocument()
  expect(within(status).getByText("Lv 6")).toBeInTheDocument()
  expect(within(status).getByRole("progressbar", { name: "HP" })).toHaveStyle({ width: "53%" })
  expect(within(status).getByRole("progressbar", { name: "SP" })).toHaveStyle({ width: "83%" })
  expect(screen.getByText("12,345")).toBeInTheDocument()
  expect(segmentOf()).toBe("0")
  expect(screen.getByRole("button", { name: "角色 Lv 6" })).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "背包" })).toHaveTextContent(/^背包$/)
  expect(screen.getByRole("button", { name: "同伴" })).toHaveTextContent(/^同伴$/)
})

test("TownScreen_SignMovesToTheMarket", () => {
  renderTown()

  fireEvent.click(screen.getByRole("button", { name: "市集" }))

  expect(segmentOf()).toBe("1")
  // 624 高時市集平移 452
  expect(world()).toHaveStyle({ transform: "translateX(-452px)" })
  expect(screen.getByRole("button", { name: "旅館" })).toBeInTheDocument()
})

// 規格 Swipe moves one segment；位移以舞台 px 計
test("TownScreen_SwipeLeftMovesOneSegment", () => {
  renderTown({ scale: 2 })

  swipe(world(), 300, 240)
  expect(segmentOf()).toBe("0")

  swipe(world(), 300, 200)
  expect(segmentOf()).toBe("1")

  swipe(world(), 300, 200)
  swipe(world(), 300, 200)
  expect(segmentOf()).toBe("2")

  swipe(world(), 200, 300)
  expect(segmentOf()).toBe("1")
})

// 規格 Sign click after a swipe is ignored
test("TownScreen_ClickRightAfterSwipeIsIgnored", () => {
  renderTown()
  fireEvent.click(screen.getByRole("button", { name: "市集" }))
  const toGate = screen.getByRole("button", { name: "城門口" })

  swipe(toGate, 300, 240)
  fireEvent.click(toGate)

  expect(segmentOf()).toBe("2")

  act(() => vi.advanceTimersByTime(300))
  fireEvent.click(screen.getByRole("button", { name: "市集" }))
  expect(segmentOf()).toBe("1")
})

// 規格 Other segments' signs are inactive
test("TownScreen_OtherSegmentsSignsAreInactive", () => {
  const { onOpen } = renderTown()

  expect(screen.queryByRole("button", { name: "旅館" })).not.toBeInTheDocument()
  fireEvent.click(screen.getByText("旅館"))
  expect(onOpen).not.toHaveBeenCalled()

  fireEvent.click(screen.getByRole("button", { name: "市集" }))
  expect(screen.queryByRole("button", { name: "角色 Lv 6" })).not.toBeInTheDocument()
  fireEvent.click(screen.getByText("角色"))
  expect(onOpen).not.toHaveBeenCalled()
})

test("TownScreen_FacilitySignsReportTheirFacility", () => {
  const { onOpen } = renderTown()

  fireEvent.click(screen.getByRole("button", { name: "出發 中央平原" }))
  fireEvent.click(screen.getByRole("button", { name: "市集" }))
  fireEvent.click(screen.getByRole("button", { name: "旅館" }))
  fireEvent.click(screen.getByRole("button", { name: "公會前" }))
  fireEvent.click(screen.getByRole("button", { name: "任務板" }))

  expect(onOpen.mock.calls.map(([facility]) => facility)).toEqual(["adventure", "inn", "missions"])
})

// 從旅館回來停在市集
test("TownScreen_StartsAtTheGivenSegment", () => {
  renderTown({ initialSegment: 1 })

  expect(segmentOf()).toBe("1")
})

// 規格 Portrait key
test("TownScreen_PortraitUsesJobAndSex", () => {
  renderTown()

  expect(screen.getByRole("img", { name: "收尾者（術士）" })).toHaveAttribute(
    "data-asset",
    "portraits/job-7-female",
  )
})

// 規格 Idling in the town
test("TownScreen_MakesNoAdventureRequests", async () => {
  const { api } = renderTown()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 5)
  })

  expect(api.advance).not.toHaveBeenCalled()
})

const at = (seconds: number) => new Date(Date.parse("2026-11-12T08:00:00Z") + seconds * 1000)

const attackElixir = BuffVo.create({
  kind: "Attack",
  multiplier: 1.05,
  startsAt: at(-1200),
  endsAt: at(6000),
  itemId: "item-2257",
  itemName: "神力秘藥LV1",
})
const legacyDefense = BuffVo.create({
  kind: "Defense",
  multiplier: 1.05,
  startsAt: at(-6900),
  endsAt: at(300),
  itemId: null,
  itemName: null,
})
const loft = {
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Experience" as const,
  multiplier: 1.1,
  seconds: 28800,
  price: 100,
}
const innWithLodging = InnVo.create({
  rooms: [loft],
  lodging: { roomId: "apprentice-loft", startsAt: at(-3600), endsAt: at(25200) },
  restFee: 0,
})

const bonusApi = (buffs: BuffVo[] = [attackElixir, legacyDefense]) => {
  const api = createApi()
  vi.mocked(api.getBuffs).mockResolvedValue(buffs)
  vi.mocked(api.getInn).mockResolvedValue(innWithLodging)
  return api
}

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const badges = () =>
  within(screen.getByRole("list", { name: "加成" }))
    .getAllByRole("button")
    .map((badge) => badge.getAttribute("aria-label"))

test("TownScreen_BonusRowShowsLodgingThenElixirs", async () => {
  renderTown({ api: bonusApi() })
  await flush()

  expect(badges()).toEqual(["學徒閣樓，剩 7:00", "神力秘藥LV1，剩 1:40", "物防 ×1.05，剩 5 分"])
})

const lodgingBadge = () =>
  screen.getByRole("button", { name: /^鬥士通鋪|^後花園/ }).querySelector("img")!

// 規格 town-screen：Lodging badge for a booked room
test("TownScreen_LodgingBadgeUsesTheRoomBadgeImage", async () => {
  const api = bonusApi([])
  const bunk = { ...loft, id: "fighters-bunk", name: "鬥士通鋪", kind: "Attack" as const }
  vi.mocked(api.getInn).mockResolvedValue(
    InnVo.create({
      ...innWithLodging,
      rooms: [bunk],
      lodging: { ...innWithLodging.lodging!, roomId: "fighters-bunk" },
    }),
  )
  renderTown({ api })
  await flush()

  expect(lodgingBadge()).toHaveAttribute("data-asset", "room-badges/fighters-bunk")
  expect(lodgingBadge().getAttribute("src")).toContain("room-badges/fighters-bunk.")
})

// 規格 town-screen：Lodging badge without an image——清單沒有這間的徽章時用房型徽章佔位圖
test("TownScreen_LodgingBadgeFallsBackToThePlaceholder", async () => {
  const api = bonusApi([])
  const garden = { ...loft, id: "garden-room", name: "後花園" }
  vi.mocked(api.getInn).mockResolvedValue(
    InnVo.create({
      ...innWithLodging,
      rooms: [garden],
      lodging: { ...innWithLodging.lodging!, roomId: "garden-room" },
    }),
  )
  renderTown({ api })
  await flush()

  expect(lodgingBadge()).toHaveAttribute("src", roomBadgesPlaceholder)
})

test("TownScreen_BonusListOpensAndClosesOnOutsideClick", async () => {
  renderTown({ api: bonusApi() })
  await flush()

  fireEvent.click(screen.getByRole("button", { name: "神力秘藥LV1，剩 1:40" }))
  const list = screen.getByRole("dialog", { name: "生效中的加成" })
  expect(within(list).getByText("住宿 1・密藥 2")).toBeInTheDocument()
  expect(within(list).getByText("神力秘藥LV1")).toBeInTheDocument()
  expect(within(list).getByText("物攻 ×1.05")).toBeInTheDocument()
  // 舊資料：名稱就是「種類 ×倍率」，沒有第二行
  expect(within(list).getAllByText("物防 ×1.05")).toHaveLength(1)

  fireEvent.click(screen.getByRole("button", { name: "關閉" }))
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
})

test("TownScreen_ChangingSegmentClosesTheBonusList", async () => {
  renderTown({ api: bonusApi() })
  await flush()

  fireEvent.click(screen.getByRole("button", { name: "神力秘藥LV1，剩 1:40" }))
  fireEvent.click(screen.getByRole("button", { name: "市集" }))

  expect(screen.queryByRole("dialog")).not.toBeInTheDocument()
})

test("TownScreen_EndingSoonBadgeIsMarked", async () => {
  renderTown({ api: bonusApi() })
  await flush()

  expect(screen.getByRole("button", { name: "物防 ×1.05，剩 5 分" })).toHaveAttribute(
    "data-ending-soon",
    "true",
  )
  expect(screen.getByRole("button", { name: "神力秘藥LV1，剩 1:40" })).toHaveAttribute(
    "data-ending-soon",
    "false",
  )
})

// 規格 A bonus runs out in the town
test("TownScreen_ExpiredBonusTriggersOneRefresh", async () => {
  const api = bonusApi()
  renderTown({ api })
  await flush()
  vi.mocked(api.getBuffs).mockResolvedValue([attackElixir])

  await act(async () => {
    await vi.advanceTimersByTimeAsync(BuffTickMilliseconds * 11)
  })

  expect(api.getBuffs).toHaveBeenCalledTimes(2)
  expect(api.getInn).toHaveBeenCalledTimes(2)
  expect(screen.queryByRole("button", { name: /物防/ })).not.toBeInTheDocument()
})

test("TownScreen_BonusRowHiddenWhenLoadingFails", async () => {
  const api = bonusApi()
  vi.mocked(api.getBuffs).mockRejectedValue(new Error("boom"))
  renderTown({ api })
  await flush()

  expect(screen.queryByRole("list", { name: "加成" })).not.toBeInTheDocument()
  expect(screen.queryByRole("alert")).not.toBeInTheDocument()
})
