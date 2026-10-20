import { act, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import type { AdvanceResponse, CharacterView, ContentNames } from "../game/types"
import { AdventureScreen } from "./AdventureScreen"

// jsdom 沒有 WebGL，舞台換成空元件
vi.mock("../render/PixiStage", () => ({ PixiStage: () => <div data-testid="stage" /> }))

const character: CharacterView = {
  name: "青衣",
  experience: 0,
  spiritStones: 0,
  currentStageId: "test-stage-1",
}

const content: ContentNames = {
  monsters: [{ id: "test-wolf", name: "測試寒狼" }],
  stages: [{ id: "test-stage-1", name: "測試關卡・一" }],
}

const response: AdvanceResponse = {
  events: [
    {
      type: "encounter",
      atSeconds: 0,
      monsterId: "test-wolf",
      monsterMaxHealth: 60,
      playerMaxHealth: 120,
    },
    { type: "reward", atSeconds: 0, experience: 8, spiritStones: 3 },
  ],
  experienceGained: 8,
  spiritStonesGained: 3,
  character: { ...character, experience: 8, spiritStones: 3 },
}

const createFakeApi = (): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn(),
  createCharacter: vi.fn(),
  advance: vi.fn().mockResolvedValue(response),
  getContent: vi.fn().mockResolvedValue(content),
})

const advanceTime = (milliseconds: number) =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(milliseconds)
  })

beforeEach(() => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date("2026-10-21T00:00:00Z"))
})

afterEach(() => {
  vi.useRealTimers()
})

test("AdventureScreen_ShowsLogLineAndUpdatedTotals", async () => {
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} />)
  expect(screen.getByText("修為 0")).toBeInTheDocument()

  // 第一段讓名稱表載入、開始輪詢；第二段讓 advance 回應並由 animation frame 播出
  await advanceTime(0)
  await advanceTime(100)

  expect(screen.getByText("測試關卡・一")).toBeInTheDocument()
  const lines = screen.getAllByRole("listitem")
  expect(lines).toHaveLength(2)
  expect(lines[0]).toHaveTextContent("測試寒狼現身。")
  expect(lines[1]).toHaveTextContent("獲得修為 8、靈石 3。")
  expect(screen.getByText("修為 8")).toBeInTheDocument()
  expect(screen.getByText("靈石 3")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "本命" })).toBeDisabled()
  expect(screen.getByRole("button", { name: "本命" })).toHaveAttribute("title", "尚未開放")
})
