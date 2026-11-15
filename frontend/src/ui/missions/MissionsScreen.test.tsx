import { act, cleanup, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { PollIntervalMilliseconds } from "../../motion/timings"
import { makeCharacter, makeMissionBoard } from "../../test/fixtures"
import type { MissionBoardVo } from "../../game/missions"
import { StageContext } from "../stage/stageContext"
import { MissionsScreen } from "./MissionsScreen"
import { createMissionsApi, daily, lv150Board, regular } from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderMissions = async (
  api = createMissionsApi(lv150Board()),
  options: { level?: number; onBack?: () => void; onCharacterChange?: () => void } = {},
) => {
  const onBack = options.onBack ?? vi.fn()
  const onCharacterChange = options.onCharacterChange ?? vi.fn()
  render(
    <StageContext value={{ height: 624, scale: 1 }}>
      <MissionsScreen
        api={api}
        character={makeCharacter({ level: options.level ?? 150 })}
        onCharacterChange={onCharacterChange}
        onBack={onBack}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack, onCharacterChange }
}

const board = () => screen.getByRole("region", { name: "佈告欄" })
const pageButton = (name: "每日委託" | "一般委託") =>
  within(screen.getByRole("group", { name: "佈告欄頁" })).getByRole("button", {
    name: new RegExp(`^${name}`),
  })
const currentPage = () =>
  within(screen.getByRole("group", { name: "佈告欄頁" }))
    .getAllByRole("button")
    .find((button) => button.getAttribute("aria-pressed") === "true")
    ?.textContent?.replace(/\d+/g, "")
const slips = () => within(board()).queryAllByTestId("board-slip")
const swipe = (dx: number) => {
  fireEvent.pointerDown(board(), { clientX: 200 })
  fireEvent.pointerUp(board(), { clientX: 200 + dx })
}

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
})

afterEach(() => {
  cleanup()
  vi.useRealTimers()
})

// 規格 missions-screen：Opening page and counts（Lv 150）
test("OpensOnTheDailyPageWhenItHasSlips", async () => {
  await renderMissions()

  expect(currentPage()).toBe("每日委託")
  expect(slips()).toHaveLength(4)
})

// 規格 Opening page and counts（Lv 20，每日未開放）
test("OpensOnTheRegularPageWithoutDailies", async () => {
  await renderMissions(
    createMissionsApi(makeMissionBoard({ available: [regular("mission-1", 20)] })),
    {
      level: 20,
    },
  )

  expect(currentPage()).toBe("一般委託")
  expect(slips()).toHaveLength(1)
})

test("SwipeChangesPageWithoutOpeningASlip", async () => {
  await renderMissions()

  swipe(-60)
  fireEvent.click(slips()[0])

  expect(currentPage()).toBe("一般委託")
  expect(screen.queryByRole("dialog")).toBeNull()
  swipe(-60)
  expect(currentPage()).toBe("一般委託")
  swipe(60)
  expect(currentPage()).toBe("每日委託")
  swipe(30)
  expect(currentPage()).toBe("每日委託")
})

test("PaperBehindSwitchesPages", async () => {
  await renderMissions()

  fireEvent.click(pageButton("一般委託"))

  expect(currentPage()).toBe("一般委託")
  expect(slips()).toHaveLength(21)
})

test("PaperBehindShowsTheCount", async () => {
  await renderMissions()

  expect(pageButton("一般委託")).toHaveTextContent("21")
  expect(pageButton("每日委託")).not.toHaveTextContent(/\d/)
  fireEvent.click(pageButton("一般委託"))
  expect(pageButton("每日委託")).toHaveTextContent("3")
  expect(pageButton("一般委託")).not.toHaveTextContent(/\d/)
})

// 規格 Completed daily stays in place
test("CompletedDailyStaysFadedInPlace", async () => {
  await renderMissions()

  const titles = slips().map((slip) => within(slip).getByTestId("slip-title").textContent)
  expect(titles).toEqual([
    "委託mission-222",
    "委託mission-225",
    "委託mission-226",
    "委託mission-227",
  ])
  expect(slips()[1]).toHaveAttribute("data-done", "true")
  expect(within(slips()[1]).getByText("完")).toBeInTheDocument()
  expect(slips()[0]).toHaveAttribute("data-done", "false")
  expect(within(slips()[0]).getByText("討伐")).toBeInTheDocument()
  expect(within(slips()[0]).getByText("Lv 50")).toBeInTheDocument()
  expect(screen.getByText(/小時後重置$/)).toBeInTheDocument()
})

test("RegularPageListsEveryAvailable", async () => {
  await renderMissions()
  fireEvent.click(pageButton("一般委託"))

  expect(slips()).toHaveLength(21)
  expect(within(board()).getByTestId("board-notes")).toHaveAttribute("data-scroll", "true")
  expect(screen.queryByText(/小時後重置$/)).toBeNull()
})

// 規格 Completed pile
test("PileOpensTheCompletedList", async () => {
  await renderMissions()
  fireEvent.click(pageButton("一般委託"))

  fireEvent.click(screen.getByRole("button", { name: "完成的委託 2 件" }))
  const list = screen.getByRole("dialog", { name: "完成的委託" })
  const entries = within(list).getAllByRole("button", { name: /^委託/ })
  expect(entries.map((entry) => entry.getAttribute("aria-label"))).toEqual([
    "委託mission-90",
    "委託mission-80",
  ])

  fireEvent.click(entries[0])
  const sheet = screen.getByRole("dialog", { name: "委託mission-90" })
  expect(within(sheet).getByText("完")).toBeInTheDocument()
  fireEvent.click(within(sheet).getByRole("button", { name: "關閉" }))
  expect(screen.getByRole("dialog", { name: "完成的委託" })).toBeInTheDocument()
})

test("LoadingShowsBlankSlips", async () => {
  const api = createMissionsApi()
  vi.mocked(api.getMissions).mockReturnValue(new Promise<MissionBoardVo>(() => undefined))
  await renderMissions(api)

  const blanks = within(board()).getAllByTestId("board-blank")
  expect(blanks).toHaveLength(6)
  expect(slips()).toHaveLength(0)
  expect(pageButton("一般委託")).not.toHaveTextContent(/\d/)
  expect(screen.queryByText(/小時後重置$/)).toBeNull()
  expect(screen.getByTestId("hand-count")).toHaveTextContent("")
})

test("RetryAfterLoadFailure", async () => {
  const api = createMissionsApi(lv150Board())
  vi.mocked(api.getMissions).mockRejectedValueOnce(new Error("offline"))
  await renderMissions(api)

  expect(within(board()).getByText("哎呀，委託單一時找不到了……")).toBeInTheDocument()
  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(slips()).toHaveLength(4)
  expect(currentPage()).toBe("每日委託")
})

// 規格 Daily missions not open yet
test("DailyPageShowsFirstDailyLevelBeforeOpening", async () => {
  await renderMissions(
    createMissionsApi(
      makeMissionBoard({ available: [regular("mission-1", 20)], firstDailyLevel: 50 }),
    ),
    {
      level: 20,
    },
  )
  fireEvent.click(pageButton("每日委託"))

  expect(slips()).toHaveLength(0)
  expect(within(board()).getByText("Lv 50")).toBeInTheDocument()
})

test("BackReturnsToTheGuildSquare", async () => {
  const { onBack } = await renderMissions()

  fireEvent.click(screen.getByRole("button", { name: "公會前" }))

  expect(onBack).toHaveBeenCalledOnce()
})

test("MakesNoAdventureRequests", async () => {
  const { api } = await renderMissions()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 3)
  })

  expect(api.advance).not.toHaveBeenCalled()
  expect(api.getMissions).toHaveBeenCalledOnce()
  void daily
})
