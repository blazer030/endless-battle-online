import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import type { CharacterVo } from "../game/character"
import { InnBookVo, InnVo } from "../game/inn"
import { MercenaryHireVo } from "../game/mercenaries"
import { RevealMilliseconds, PollIntervalMilliseconds } from "../motion/timings"
import { makeCharacter, makeMercenaryOverview } from "../test/fixtures"
import { createCompanionsApi } from "./companions/testing"
import { createHonourApi, shopWith } from "./honour/testing"
import { GameScreen } from "./GameScreen"

// 舊外殼另有測試；這裡只看停在哪個分頁、回城鎮交回什麼角色
// 每個舊外殼交出的「之後才回來的角色」記在這裡，測試在回城鎮後再送
const lateReports: ((character: CharacterVo) => void)[] = []

// 預載只記下被要了哪些圖；每次呼叫各自等測試放行
const preloads: { urls: readonly string[]; finish: () => void }[] = []
vi.mock("./stage/preloadImages", () => ({
  preloadImages: (urls: readonly string[]) =>
    new Promise<void>((resolve) => preloads.push({ urls, finish: resolve })),
}))

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

const character = makeCharacter({ name: "收尾者", level: 6, money: 1000 })

const loft = {
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Experience" as const,
  multiplier: 1.1,
  seconds: 28_800,
  price: 600,
}

const createApi = () =>
  ({
    ...createCompanionsApi(),
    ...createHonourApi(shopWith()),
    getBuffs: vi.fn().mockResolvedValue([]),
    getInn: vi.fn().mockResolvedValue(InnVo.create({ rooms: [loft], lodging: null, restFee: 0 })),
    bookInn: vi.fn(),
    updateSettings: vi.fn(),
    getMyCharacter: vi.fn(),
    advance: vi.fn(),
  }) as unknown as ApiClient

const renderGame = (api = createApi()) => {
  render(<GameScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  return api
}

const segmentOf = () => screen.getByTestId("town-world").dataset.segment
const click = (name: string) => fireEvent.click(screen.getByRole("button", { name }))

beforeEach(() => {
  preloads.length = 0
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

// 城鎮的圖先載完，才在背景載旅館與同伴的圖（慢網路時不跟城鎮搶頻寬）
test("GameScreen_PreloadsInnerPagesAfterTheTown", async () => {
  renderGame()
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

  expect(preloads).toHaveLength(1)
  expect(preloads[0]!.urls.some((url) => url.includes("panorama-"))).toBe(true)

  preloads[0]!.finish()
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

  expect(preloads).toHaveLength(2)
  expect(preloads[1]!.urls.some((url) => url.includes("stall-gate"))).toBe(true)
  expect(preloads[1]!.urls.some((url) => url.includes("medal-case"))).toBe(true)
})

// 規格 inn-screen：Inn opens the inn screen
test("GameScreen_InnOpensTheInnScreen", async () => {
  renderGame()

  click("市集")
  click("旅館")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.getByRole("region", { name: "旅館" })).toBeInTheDocument()
  expect(screen.queryByText(/舊外殼/)).not.toBeInTheDocument()
  expect(screen.queryByTestId("town-world")).not.toBeInTheDocument()
})

// 規格 inn-screen：Back returns to the market——入住後按「市集」，城鎮停在市集、金錢是入住後的
test("GameScreen_BackFromTheInnShowsTheMarketWithTheNewMoney", async () => {
  const api = createApi()
  vi.mocked(api.bookInn).mockResolvedValue(
    InnBookVo.create({
      character: { ...character, money: 400 },
      lodging: {
        roomId: "apprentice-loft",
        startsAt: new Date(),
        endsAt: new Date(Date.now() + 28_800_000),
      },
      cost: 600,
    }),
  )
  renderGame(api)
  click("市集")
  click("旅館")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  fireEvent.click(screen.getByRole("button", { name: /^學徒閣樓/ }))
  fireEvent.click(
    within(screen.getByRole("dialog", { name: "入住" })).getByRole("button", { name: "入住" }),
  )
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })
  click("市集")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.queryByRole("region", { name: "旅館" })).not.toBeInTheDocument()
  expect(segmentOf()).toBe("1")
  expect(screen.getByLabelText("金錢")).toHaveTextContent("400")
})

// 規格 companions-screen：Enter and leave the companions screen——城門口「同伴」展開入口，不呼叫推進
test("GameScreen_CompanionsOpensTheCompanionsScreen", async () => {
  const api = renderGame()

  click("同伴")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.getByRole("button", { name: "獸欄" })).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "傭兵所" })).toBeInTheDocument()
  expect(screen.queryByText(/舊外殼/)).not.toBeInTheDocument()
  expect(screen.queryByTestId("town-world")).not.toBeInTheDocument()
  expect(api.advance).not.toHaveBeenCalled()
})

// 規格 companions-screen：Enter and leave the companions screen——「城門」收回、停在城門口、帶著最新的金錢
test("GameScreen_BackFromCompanionsShowsTheGate", async () => {
  const api = createApi()
  const [grade] = makeMercenaryOverview().catalogue
  vi.mocked(api.getMercenaries).mockResolvedValue(
    makeMercenaryOverview({ catalogue: [{ ...grade!, price: 700 }] }),
  )
  vi.mocked(api.hireMercenary).mockResolvedValue(
    MercenaryHireVo.create({
      mercenaries: makeMercenaryOverview(),
      character: { ...character, money: 300 },
    }),
  )
  renderGame(api)
  click("同伴")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  click("傭兵所")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })
  fireEvent.click(screen.getByRole("button", { name: /^F級傭兵/ }))
  fireEvent.click(
    within(screen.getByRole("dialog", { name: "F級傭兵" })).getByRole("button", { name: "僱用" }),
  )
  await act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })
  click("同伴")
  click("城門")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.queryByRole("button", { name: "傭兵所" })).not.toBeInTheDocument()
  expect(segmentOf()).toBe("0")
  expect(screen.getByLabelText("金錢")).toHaveTextContent("300")
})

// 規格 honour-screen：Enter and leave the honour screen——公會前「勳章所」展開櫃檯，不呼叫推進
test("GameScreen_HonourOpensTheHonourScreen", async () => {
  const api = renderGame()

  click("市集")
  click("公會前")
  click("勳章所")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.getByRole("group", { name: "持有勳章" })).toBeInTheDocument()
  expect(screen.queryByText(/舊外殼/)).not.toBeInTheDocument()
  expect(screen.queryByTestId("town-world")).not.toBeInTheDocument()
  expect(api.advance).not.toHaveBeenCalled()
})

// 規格 honour-screen：「公會前」收回、停在公會前
test("GameScreen_BackFromHonourShowsTheGuildSquare", async () => {
  renderGame()
  click("市集")
  click("公會前")
  click("勳章所")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  click("公會前")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(RevealMilliseconds)
  })

  expect(screen.queryByRole("group", { name: "持有勳章" })).not.toBeInTheDocument()
  expect(segmentOf()).toBe("2")
})

// 規格 Facilities open the old tabbed screen 的對照表
test.each([
  [[], "角色 Lv 6", "character／bag"],
  [[], "背包", "bag／bag"],
  [[], "出發 中央平原", "adventure／bag"],
  [["市集"], "商店街", "bag／shop"],
  [["市集", "公會前"], "鐵舖", "bag／forge"],
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
