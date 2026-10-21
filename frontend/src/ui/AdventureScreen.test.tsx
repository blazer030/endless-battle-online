import { act, fireEvent, render, screen, within } from "@testing-library/react"
import type { Ref } from "react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import type { AdvanceResponse, CharacterView, ContentNames } from "../game/types"
import { LevelUpHintMilliseconds } from "../motion/timings"
import type { StageHandle } from "../render/StageRenderer"
import { makeCharacter, makeContent, makeSummary } from "../test/fixtures"
import { AdventureScreen } from "./AdventureScreen"

// jsdom 沒有 WebGL，舞台換成假元件；handle 要等測試呼叫 attach 才接上，模擬舞台晚到
const stage = vi.hoisted(() => ({
  handle: {
    showEncounter: vi.fn<StageHandle["showEncounter"]>(),
    updateHealth: vi.fn<StageHandle["updateHealth"]>(),
    play: vi.fn<StageHandle["play"]>(),
  },
  attach: () => {},
  fail: false,
}))

vi.mock("../render/PixiStage", async () => {
  const { useEffect, useImperativeHandle, useState } = await import("react")
  const AttachedStage = ({ ref }: { ref?: Ref<StageHandle> }) => {
    useImperativeHandle(ref, () => stage.handle, [])
    return <div data-testid="stage" />
  }
  return {
    PixiStage: ({ ref }: { ref?: Ref<StageHandle> }) => {
      if (stage.fail) throw new Error("chunk load failed")
      const [attached, setAttached] = useState(false)
      useEffect(() => {
        stage.attach = () => setAttached(true)
      }, [])
      return attached ? <AttachedStage ref={ref} /> : <div data-testid="stage" />
    },
  }
})

const character: CharacterView = makeCharacter({ name: "青衣" })

const content: ContentNames = makeContent({
  monsters: [{ id: "test-wolf", name: "測試寒狼" }],
  stages: [{ id: "test-stage-1", name: "測試關卡・一" }],
})

const response: AdvanceResponse = {
  events: [
    {
      type: "encounter",
      atSeconds: 0,
      monsterId: "test-wolf",
      monsterHp: 60,
      monsterHpMax: 60,
      playerHp: 100,
      playerHpMax: 120,
      playerSp: 10,
      playerSpMax: 10,
      staminaDepleted: false,
    },
    { type: "reward", atSeconds: 0, exp: 8, money: 3, jobExp: 2 },
  ],
  summary: makeSummary({ fights: 1, wins: 1, exp: 8, money: 3, jobExp: 2 }),
  eventsCoverAll: true,
  character: { ...character, exp: 8, money: 3, actNum: 49 },
}

const levelUpResponse: AdvanceResponse = {
  events: [
    { type: "reward", atSeconds: 0, exp: 1200, money: 3, jobExp: 2 },
    {
      type: "levelUp",
      atSeconds: 0,
      newLevel: 2,
      growth: { str: 1, smart: 0, life: 0, vit: 0, agi: 0, au: 0, be: 0 },
    },
  ],
  summary: makeSummary({ fights: 1, wins: 1, exp: 1200, money: 3, jobExp: 2, levelsGained: 1 }),
  eventsCoverAll: true,
  character: { ...character, level: 2, exp: 200, nextExp: 1092, money: 3 },
}

const createFakeApi = (advanceResponse: AdvanceResponse = response): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn(),
  createCharacter: vi.fn(),
  advance: vi.fn().mockResolvedValue(advanceResponse),
  getContent: vi.fn().mockResolvedValue(content),
})

const advanceTime = (milliseconds: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })

beforeEach(() => {
  vi.clearAllMocks()
  stage.fail = false
  vi.useFakeTimers()
  vi.setSystemTime(new Date("2026-10-21T00:00:00Z"))
})

afterEach(() => {
  vi.useRealTimers()
})

const topBarExp = () => within(screen.getByRole("banner")).getByRole("progressbar", { name: "EXP" })

test("AdventureScreen_ShowsLogLineAndUpdatedTotals", async () => {
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} />)
  const topBar = screen.getByRole("banner")
  expect(within(topBar).getByText("Lv 1")).toBeInTheDocument()
  expect(topBarExp()).toHaveAttribute("aria-valuenow", "0")
  expect(within(topBar).getByText("金錢 0")).toBeInTheDocument()

  // 第一段讓名稱表載入、開始輪詢；第二段讓 advance 回應並由 animation frame 播出
  await advanceTime(0)
  await advanceTime(100)

  expect(screen.getByText("測試關卡・一")).toBeInTheDocument()
  const lines = screen.getAllByRole("listitem")
  expect(lines).toHaveLength(2)
  expect(lines[0]).toHaveTextContent("遭遇 測試寒狼！")
  expect(lines[1]).toHaveTextContent("獲得經驗 8、金錢 3、熟練度 2。")
  expect(topBarExp()).toHaveAttribute("aria-valuenow", "8")
  expect(within(topBar).getByText("8 / 1,000")).toBeInTheDocument()
  expect(within(topBar).getByText("金錢 3")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "冒險" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("button", { name: "角色" })).toBeEnabled()
  for (const label of ["技能", "背包"]) {
    expect(screen.getByRole("button", { name: label })).toBeDisabled()
    expect(screen.getByRole("button", { name: label })).toHaveAttribute("title", "尚未開放")
  }
})

test("AdventureScreen_TopBarShowsStamina", async () => {
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} />)
  const topBar = screen.getByRole("banner")
  expect(within(topBar).getByText("行動力 50/50")).toBeInTheDocument()

  await advanceTime(0)
  await advanceTime(100)

  expect(within(topBar).getByText("行動力 49/50")).toBeInTheDocument()
})

test("AdventureScreen_ShowsLevelUpHintWhenLevelUpPlays", async () => {
  render(<AdventureScreen api={createFakeApi(levelUpResponse)} initialCharacter={character} />)
  const topBar = screen.getByRole("banner")
  expect(within(topBar).queryByText("升級！")).not.toBeInTheDocument()

  await advanceTime(0)
  await advanceTime(100)

  expect(within(topBar).getByText("Lv 2")).toBeInTheDocument()
  expect(within(topBar).getByText("升級！")).toBeInTheDocument()
  expect(screen.getAllByRole("listitem")[1]).toHaveTextContent("升級！Lv 2（力量+1）")

  await advanceTime(LevelUpHintMilliseconds)

  expect(within(topBar).queryByText("升級！")).not.toBeInTheDocument()
})

test("AdventureScreen_TopBarExpBarIsProportional", () => {
  render(
    <AdventureScreen
      api={createFakeApi()}
      initialCharacter={{ ...character, level: 7, exp: 250, nextExp: 1000 }}
    />,
  )

  const bar = topBarExp()
  expect(within(screen.getByRole("banner")).getByText("Lv 7")).toBeInTheDocument()
  expect(bar).toHaveAttribute("aria-valuenow", "250")
  expect(bar).toHaveAttribute("aria-valuemax", "1000")
  expect(bar.firstElementChild).toHaveStyle({ width: "25%" })
})

test("AdventureScreen_SwitchesToCharacterSheet_WithoutRemountingAdventure", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} />)
  await advanceTime(0)
  await advanceTime(100)
  const stageElement = screen.getByTestId("stage")
  expect(api.advance).toHaveBeenCalledTimes(1)

  fireEvent.click(screen.getByRole("button", { name: "角色" }))

  expect(screen.getByRole("button", { name: "角色" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("region", { name: "能力" })).toBeVisible()
  expect(screen.getByRole("heading", { name: "青衣" })).toBeVisible()
  expect(screen.getByRole("progressbar", { name: "HP" })).toBeVisible()
  expect(stageElement).not.toBeVisible()
  expect(screen.queryAllByRole("listitem")).toHaveLength(0)

  fireEvent.click(screen.getByRole("button", { name: "冒險" }))

  expect(screen.getByRole("button", { name: "冒險" })).toHaveAttribute("aria-current", "page")
  expect(screen.queryByRole("region", { name: "能力" })).not.toBeInTheDocument()
  expect(screen.getByTestId("stage")).toBe(stageElement)
  expect(stageElement).toBeVisible()
  const lines = screen.getAllByRole("listitem")
  expect(lines).toHaveLength(2)
  expect(lines[0]).toHaveTextContent("遭遇 測試寒狼！")
  expect(lines[1]).toHaveTextContent("獲得經驗 8、金錢 3、熟練度 2。")
  // loop 若重新掛載會立刻再送一次 advance
  expect(api.advance).toHaveBeenCalledTimes(1)
})

test("AdventureScreen_ReplaysEncounterWhenStageBecomesReady", async () => {
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} />)
  await advanceTime(0)
  await advanceTime(100)
  expect(screen.getAllByRole("listitem")[0]).toHaveTextContent("遭遇 測試寒狼！")
  expect(stage.handle.showEncounter).not.toHaveBeenCalled()

  act(() => stage.attach())

  const state = {
    monsterId: "test-wolf",
    monsterHp: 60,
    monsterHpMax: 60,
    playerHp: 100,
    playerHpMax: 120,
  }
  expect(stage.handle.showEncounter).toHaveBeenCalledTimes(1)
  expect(stage.handle.showEncounter).toHaveBeenCalledWith("test-wolf", "測試寒狼", state)
  expect(stage.handle.updateHealth).toHaveBeenCalledWith(state)
})

test("AdventureScreen_KeepsLogWorking_WhenStageChunkFails", async () => {
  stage.fail = true
  const consoleError = vi.spyOn(console, "error").mockImplementation(() => {})

  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} />)
  await advanceTime(0)
  await advanceTime(100)

  expect(within(screen.getByRole("banner")).getByText("青衣")).toBeInTheDocument()
  expect(screen.getAllByRole("listitem")[0]).toHaveTextContent("遭遇 測試寒狼！")
  consoleError.mockRestore()
})

test("AdventureScreen_ShowsPollErrorOnCharacterTab", async () => {
  const api = createFakeApi()
  vi.mocked(api.advance).mockRejectedValue(new TypeError("Failed to fetch"))
  render(<AdventureScreen api={api} initialCharacter={character} />)
  await advanceTime(0)
  await advanceTime(100)

  fireEvent.click(screen.getByRole("button", { name: "角色" }))

  expect(screen.getByRole("region", { name: "能力" })).toBeVisible()
  expect(screen.getByText("連線不穩，稍後自動重試")).toBeVisible()
})
