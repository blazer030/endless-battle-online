import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import type { CharacterVo } from "../game/character"
import { PollIntervalMilliseconds } from "../motion/timings"
import { makeCharacter } from "../test/fixtures"
import { GameScreen } from "./GameScreen"

// 舊外殼另有測試；這裡只看停在哪個分頁、回城鎮交回什麼角色
// 每個舊外殼交出的「之後才回來的角色」記在這裡，測試在回城鎮後再送
const lateReports: ((character: CharacterVo) => void)[] = []

vi.mock("./AdventureScreen", () => ({
  AdventureScreen: ({
    initialCharacter,
    initialTab,
    initialBagPage,
    onReturnToTown,
    onConfirmedCharacter,
  }: {
    initialCharacter: CharacterVo
    initialTab: string
    initialBagPage: string
    onReturnToTown: (character: CharacterVo) => void
    onConfirmedCharacter: (character: CharacterVo) => void
  }) => {
    lateReports.push(onConfirmedCharacter)
    return (
      <>
        <p>{`舊外殼：${initialTab}／${initialBagPage}／Lv ${initialCharacter.level}`}</p>
        <button type="button" onClick={() => onReturnToTown({ ...initialCharacter, level: 7 })}>
          回城鎮
        </button>
      </>
    )
  },
}))

const character = makeCharacter({ name: "收尾者", level: 6 })

const createApi = () =>
  ({
    getBuffs: vi.fn().mockResolvedValue([]),
    getInn: vi.fn().mockResolvedValue({ rooms: [], lodging: null }),
    advance: vi.fn(),
  }) as unknown as ApiClient

const renderGame = (api = createApi()) => {
  render(<GameScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  return api
}

const segmentOf = () => screen.getByTestId("town-world").dataset.segment
const click = (name: string) => fireEvent.click(screen.getByRole("button", { name }))

beforeEach(() => {
  lateReports.length = 0
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 Opening the game with a character／Idling in the town
test("GameScreen_StartsAtTheGateWithoutAdventureRequests", async () => {
  const api = renderGame()

  await act(async () => {
    await vi.advanceTimersByTimeAsync(PollIntervalMilliseconds * 3)
  })

  expect(segmentOf()).toBe("0")
  expect(within(screen.getByRole("region", { name: "角色狀態" })).getByText("收尾者")).toBeVisible()
  expect(api.advance).not.toHaveBeenCalled()
})

// 規格 Inn opens the bag tab at the inn page
test("GameScreen_InnOpensTheBagTabAtTheInnPage", () => {
  renderGame()

  click("市集")
  click("旅館")

  expect(screen.getByText("舊外殼：bag／inn／Lv 6")).toBeInTheDocument()
})

// 規格 Facilities open the old tabbed screen 的對照表
test.each([
  [[], "角色 Lv 6", "character／bag"],
  [[], "背包", "bag／bag"],
  [[], "同伴", "companions／bag"],
  [[], "出發 中央平原", "adventure／bag"],
  [["市集"], "旅館", "bag／inn"],
  [["市集"], "商店街", "bag／shop"],
  [["市集", "公會前"], "鐵舖", "bag／forge"],
  [["市集", "公會前"], "勳章所", "bag／honour"],
  [["市集", "公會前"], "任務板", "missions／bag"],
])("GameScreen_EachFacilityOpensItsTab %#", (path, facility, expected) => {
  renderGame()

  for (const sign of path) click(sign)
  click(facility)

  expect(screen.getByText(`舊外殼：${expected}／Lv 6`)).toBeInTheDocument()
})

// 規格 Return to the town
test("GameScreen_ReturnToTownShowsTheGateWithTheLatestCharacter", () => {
  renderGame()
  click("市集")
  click("商店街")

  click("回城鎮")

  expect(segmentOf()).toBe("0")
  expect(screen.getByRole("button", { name: "角色 Lv 7" })).toBeInTheDocument()
})

// 規格 A response arriving after returning to the town
test("GameScreen_LateResponseAfterReturnUpdatesTheTown", () => {
  renderGame()
  click("出發 中央平原")
  const report = lateReports[lateReports.length - 1]
  click("回城鎮")

  act(() => report({ ...character, level: 19 }))

  expect(screen.getByRole("button", { name: "角色 Lv 19" })).toBeInTheDocument()
})

// 比較早打開的舊外殼回應較晚到時，不蓋掉之後那次的角色
test("GameScreen_IgnoresResponsesFromAnEarlierShell", () => {
  renderGame()
  click("出發 中央平原")
  const earlier = lateReports[lateReports.length - 1]
  click("回城鎮")
  click("背包")
  click("回城鎮")

  act(() => earlier({ ...character, level: 3 }))

  expect(screen.getByRole("button", { name: "角色 Lv 7" })).toBeInTheDocument()
})
