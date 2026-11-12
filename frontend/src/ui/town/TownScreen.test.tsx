import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../../api/client"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { TownScreen, type TownApi } from "./TownScreen"

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
  getInn: vi.fn().mockResolvedValue({ rooms: [], lodging: null }),
  advance: vi.fn(),
})

const renderTown = ({ scale = 1, api = createApi(), onOpen = vi.fn() } = {}) => {
  render(
    <StageContext value={{ height: 624, scale }}>
      <TownScreen api={api} character={character} onOpen={onOpen} />
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
