import { act, fireEvent, render, screen, within } from "@testing-library/react"
import type { Ref } from "react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { ApiClient } from "../api/client"
import type { JobChoiceVo, SkillOverviewVo } from "../game/skillOverview"
import type { AdvanceVo } from "../game/battleEvents"
import type { CharacterVo } from "../game/character"
import {
  LevelUpHintMilliseconds,
  PanelRefreshMilliseconds,
  PollIntervalMilliseconds,
} from "../motion/timings"
import type { StageHandle } from "../render/StageRenderer"
import {
  makeCharacter,
  makeCreationOptions,
  makeHonourShop,
  makeInventory,
  makeItem,
  makeJobChoice,
  makeMercenaryOverview,
  makeMission,
  makeMissionBoard,
  makePet,
  makePets,
  makePublicJobs,
  makeSummary,
  makeWorld,
} from "../test/fixtures"
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

const character: CharacterVo = makeCharacter({ name: "青衣" })

const response: AdvanceVo = {
  events: [
    {
      type: "encounter",
      atSeconds: 0,
      monsterId: "test-wolf",
      monsterName: "測試寒狼",
      monsterHealthPoints: 60,
      monsterMaxHealthPoints: 60,
      playerHealthPoints: 100,
      playerMaxHealthPoints: 120,
      playerManaPoints: 10,
      playerMaxManaPoints: 10,
      petName: null,
      petMonsterId: null,
    },
    { type: "reward", atSeconds: 0, experience: 8, money: 3, jobExperience: 2, drops: [] },
  ],
  summary: makeSummary({ fights: 1, wins: 1, experience: 8, money: 3, jobExperience: 2 }),
  eventsCoverAll: true,
  character: { ...character, experience: 8, money: 3 },
  mercenary: null,
  areaPassExpiresAt: null,
}

const levelUpResponse: AdvanceVo = {
  events: [
    { type: "reward", atSeconds: 0, experience: 1200, money: 3, jobExperience: 2, drops: [] },
    {
      type: "levelUp",
      atSeconds: 0,
      newLevel: 2,
      growth: {
        strength: 1,
        intelligence: 0,
        life: 0,
        vitality: 0,
        agility: 0,
        charisma: 0,
        faith: 0,
      },
    },
  ],
  summary: makeSummary({
    fights: 1,
    wins: 1,
    experience: 1200,
    money: 3,
    jobExperience: 2,
    levelsGained: 1,
  }),
  eventsCoverAll: true,
  character: { ...character, level: 2, experience: 200, nextLevelExperience: 1092, money: 3 },
  mercenary: null,
  areaPassExpiresAt: null,
}

const thiefOption: JobChoiceVo = makeJobChoice({ id: "thief", name: "盜賊" })

const skillOverview: SkillOverviewVo = {
  jobId: "test-warrior",
  skillPoints: 0,
  skills: [],
  slots: [],
}

const createFakeApi = (advanceResponse: AdvanceVo = response): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn(),
  createCharacter: vi.fn(),
  advance: vi.fn().mockResolvedValue(advanceResponse),
  getPublicJobs: vi.fn().mockResolvedValue(makePublicJobs()),
  getCreationOptions: vi.fn().mockResolvedValue(makeCreationOptions()),
  getJobs: vi.fn().mockResolvedValue([thiefOption]),
  changeJob: vi.fn().mockResolvedValue({ ...character, jobId: "thief", jobName: "盜賊" }),
  getSkills: vi.fn().mockResolvedValue(skillOverview),
  learnSkill: vi.fn(),
  setSkillSlots: vi.fn(),
  getWorld: vi.fn().mockResolvedValue(makeWorld()),
  moveTo: vi.fn().mockResolvedValue({
    ...character,
    placeId: 2,
    placeName: "試鍊洞窟",
    subPlaceName: "燈影隧道",
  }),
  getInventory: vi.fn().mockResolvedValue(makeInventory([makeItem({ name: "測試木劍" })])),
  equip: vi.fn(),
  unequip: vi.fn(),
  useItem: vi.fn(),
  getShop: vi.fn().mockResolvedValue([]),
  buy: vi.fn(),
  sell: vi.fn(),
  getDepot: vi.fn(),
  store: vi.fn(),
  take: vi.fn(),
  deposit: vi.fn(),
  withdraw: vi.fn(),
  refine: vi.fn(),
  socket: vi.fn(),
  unsocket: vi.fn(),
  drill: vi.fn(),
  mergeStones: vi.fn(),
  mergeCrossStones: vi.fn(),
  getRecipes: vi.fn().mockResolvedValue([]),
  synthesize: vi.fn(),
  getMissions: vi.fn().mockResolvedValue(makeMissionBoard({ available: [makeMission()] })),
  acceptMission: vi.fn(),
  abandonMission: vi.fn(),
  completeMission: vi.fn(),
  getPets: vi.fn().mockResolvedValue(makePets([makePet({ name: "小黏" })])),
  carryPet: vi.fn(),
  storePet: vi.fn(),
  releasePet: vi.fn(),
  renamePet: vi.fn(),
  feedPet: vi.fn(),
  getMercenaries: vi.fn().mockResolvedValue(makeMercenaryOverview()),
  hireMercenary: vi.fn(),
  deployMercenary: vi.fn(),
  restMercenary: vi.fn(),
  renameMercenary: vi.fn(),
  dismissMercenary: vi.fn(),
  getHonourShop: vi.fn().mockResolvedValue(makeHonourShop()),
  exchangeHonour: vi.fn(),
  updateSettings: vi.fn(),
  getBuffs: vi.fn().mockResolvedValue([]),
  getInn: vi.fn().mockResolvedValue({ rooms: [], lodging: null }),
  bookInn: vi.fn(),
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
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} onSignOut={vi.fn()} />)
  const topBar = screen.getByRole("banner")
  expect(within(topBar).getByText("Lv 1")).toBeInTheDocument()
  expect(topBarExp()).toHaveAttribute("aria-valuenow", "0")
  expect(within(topBar).getByText("金錢 0")).toBeInTheDocument()

  // 第一段讓名稱表載入、開始輪詢；第二段讓 advance 回應並由 animation frame 播出
  await advanceTime(0)
  await advanceTime(100)

  expect(screen.getByRole("button", { name: "中央平原・香茅綠地 ▾" })).toBeInTheDocument()
  const lines = screen.getAllByRole("listitem")
  expect(lines).toHaveLength(2)
  expect(lines[0]).toHaveTextContent("遭遇 測試寒狼！")
  expect(lines[1]).toHaveTextContent("獲得經驗 8、金錢 3、熟練度 2。")
  expect(topBarExp()).toHaveAttribute("aria-valuenow", "8")
  expect(within(topBar).getByText("8 / 1,000")).toBeInTheDocument()
  expect(within(topBar).getByText("金錢 3")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "冒險" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("button", { name: "角色" })).toBeEnabled()
  expect(screen.getByRole("button", { name: "技能" })).toBeEnabled()
  expect(screen.getByRole("button", { name: "背包" })).toBeEnabled()
})

test("AdventureScreen_ShowsLevelUpHintWhenLevelUpPlays", async () => {
  render(
    <AdventureScreen
      api={createFakeApi(levelUpResponse)}
      initialCharacter={character}
      onSignOut={vi.fn()}
    />,
  )
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
      initialCharacter={{ ...character, level: 7, experience: 250, nextLevelExperience: 1000 }}
      onSignOut={vi.fn()}
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
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
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
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} onSignOut={vi.fn()} />)
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
    petName: null,
    petMonsterId: null,
    petActive: false,
  }
  expect(stage.handle.showEncounter).toHaveBeenCalledTimes(1)
  expect(stage.handle.showEncounter).toHaveBeenCalledWith("test-wolf", "測試寒狼", state)
  expect(stage.handle.updateHealth).toHaveBeenCalledWith(state)
})

test("AdventureScreen_RestRefillsStageHealth", async () => {
  const restResponse: AdvanceVo = {
    ...response,
    events: [
      response.events[0],
      {
        type: "attack",
        atSeconds: 0.4,
        attacker: "Monster",
        kind: "Physical",
        hit: true,
        critical: false,
        damage: 100,
        elementMultiplier: 1,
        targetHealthPointsAfter: 0,
        stunned: false,
      },
      { type: "defeat", atSeconds: 0.4, loser: "Player" },
      { type: "rest", atSeconds: 2, cost: 0, experiencePenalty: false, waitSeconds: 15 },
    ],
  }
  render(
    <AdventureScreen
      api={createFakeApi(restResponse)}
      initialCharacter={character}
      onSignOut={vi.fn()}
    />,
  )
  await advanceTime(0)
  await advanceTime(500)
  act(() => stage.attach())
  expect(stage.handle.updateHealth).toHaveBeenLastCalledWith(
    expect.objectContaining({ playerHp: 0, playerHpMax: 120 }),
  )

  await advanceTime(2000)

  expect(stage.handle.updateHealth).toHaveBeenLastCalledWith(
    expect.objectContaining({ playerHp: 120, playerHpMax: 120 }),
  )
  expect(stage.handle.play).toHaveBeenLastCalledWith([{ type: "fade", seconds: 15 }])
})

test("AdventureScreen_KeepsLogWorking_WhenStageChunkFails", async () => {
  stage.fail = true
  const consoleError = vi.spyOn(console, "error").mockImplementation(() => {})

  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  await advanceTime(100)

  expect(within(screen.getByRole("banner")).getByText("青衣")).toBeInTheDocument()
  expect(screen.getAllByRole("listitem")[0]).toHaveTextContent("遭遇 測試寒狼！")
  consoleError.mockRestore()
})

test("AdventureScreen_ShowsPollErrorOnCharacterTab", async () => {
  const api = createFakeApi()
  vi.mocked(api.advance).mockRejectedValue(new TypeError("Failed to fetch"))
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  await advanceTime(100)

  fireEvent.click(screen.getByRole("button", { name: "角色" }))

  expect(screen.getByRole("region", { name: "能力" })).toBeVisible()
  expect(screen.getByText("連線不穩，稍後自動重試")).toBeVisible()
})

test("AdventureScreen_SkillsTabLoadsSkillsScreenOnEachVisit", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  expect(api.getJobs).not.toHaveBeenCalled()

  fireEvent.click(screen.getByRole("button", { name: "技能" }))
  await advanceTime(0)

  expect(screen.getByRole("button", { name: "技能" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("heading", { name: "盜賊（基礎）" })).toBeVisible()
  expect(screen.getByTestId("stage")).not.toBeVisible()

  fireEvent.click(screen.getByRole("button", { name: "冒險" }))
  expect(screen.queryByRole("heading", { name: "盜賊（基礎）" })).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole("button", { name: "技能" }))
  await advanceTime(0)

  expect(api.getJobs).toHaveBeenCalledTimes(2)
})

test("AdventureScreen_BagTabOpensBagScreen", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  expect(api.getInventory).not.toHaveBeenCalled()

  fireEvent.click(screen.getByRole("button", { name: "背包" }))
  await advanceTime(0)

  expect(screen.getByRole("button", { name: "背包" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("tablist", { name: "背包分頁" })).toBeVisible()
  expect(screen.getByText("測試木劍 +0")).toBeVisible()
  expect(screen.getByTestId("stage")).not.toBeVisible()
})

test("AdventureScreen_CompanionsTabIsSixthAndLoadsPets", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  expect(api.getPets).not.toHaveBeenCalled()

  fireEvent.click(screen.getByRole("button", { name: "同伴" }))
  await advanceTime(0)

  expect(api.getPets).toHaveBeenCalledTimes(1)
  expect(screen.getByRole("button", { name: "同伴" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("tablist", { name: "同伴分頁" })).toBeVisible()
  expect(screen.getByRole("article", { name: "小黏－草原史萊姆 2歲" })).toBeVisible()
  expect(screen.getByTestId("stage")).not.toBeVisible()
})

test("AdventureScreen_SettingsButtonOpensTheSettingsDialog", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)

  const button = screen.getByRole("button", { name: "設定" })
  expect(button).toHaveAttribute("aria-expanded", "false")
  fireEvent.click(button)

  const dialog = screen.getByRole("dialog", { name: "冒險設定" })
  expect(within(dialog).getByRole("combobox", { name: "攻擊方式" })).toBeInTheDocument()
  fireEvent.click(within(dialog).getByRole("button", { name: "關閉" }))
  expect(screen.queryByRole("dialog", { name: "冒險設定" })).not.toBeInTheDocument()
})

test("AdventureScreen_LogsTheMercenaryReportBeforeTheBatch", async () => {
  const api = createFakeApi({
    ...response,
    character: { ...response.character, experience: 48, money: 10 },
    mercenary: {
      name: "阿福",
      fights: 1,
      wins: 1,
      experience: 40,
      money: 7,
      levelsGained: 0,
      remaining: 99,
      dismissed: false,
      recent: [
        {
          at: new Date("2026-10-20T23:58:20Z"),
          monsterId: "test-wolf",
          monsterName: "測試寒狼",
          won: true,
          experience: 40,
          money: 7,
        },
      ],
    },
  })
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  await advanceTime(100)

  const lines = screen.getAllByRole("listitem")
  expect(lines[0]).toHaveTextContent("【傭兵】阿福 戰勝 測試寒狼，獲得金錢 7，經驗值 40")
  expect(lines[1]).toHaveTextContent("遭遇 測試寒狼！")
  expect(within(screen.getByRole("banner")).getByText("金錢 10")).toBeInTheDocument()
})

test("AdventureScreen_MissionsTabIsFifthAndLoadsMissions", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  expect(api.getMissions).not.toHaveBeenCalled()

  const navigation = within(screen.getByRole("navigation")).getAllByRole("button")
  expect(navigation.map((button) => button.textContent)).toEqual([
    "冒險",
    "角色",
    "技能",
    "背包",
    "任務",
    "同伴",
  ])
  fireEvent.click(screen.getByRole("button", { name: "任務" }))
  await advanceTime(0)

  expect(api.getMissions).toHaveBeenCalledTimes(1)
  expect(screen.getByRole("button", { name: "任務" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("article", { name: "綠地上的黏液" })).toBeVisible()
  expect(screen.getByTestId("stage")).not.toBeVisible()
})

test("AdventureScreen_JobChangeUpdatesTopBarAndLogs", async () => {
  render(<AdventureScreen api={createFakeApi()} initialCharacter={character} onSignOut={vi.fn()} />)
  const topBar = screen.getByRole("banner")
  expect(within(topBar).getByText("測試戰士")).toBeInTheDocument()
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "技能" }))
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: "轉職" }))
  await advanceTime(0)

  expect(within(topBar).getByText("盜賊")).toBeInTheDocument()
  fireEvent.click(screen.getByRole("button", { name: "冒險" }))
  const lines = screen.getAllByRole("listitem").map((line) => line.textContent)
  expect(lines).toContain("你轉職為盜賊。")
})

test("AdventureScreen_MovingUpdatesTheHeadingAndLogs", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "中央平原・香茅綠地 ▾" }))
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: /^試鍊洞窟/ }))
  fireEvent.click(screen.getByRole("button", { name: "燈影隧道" }))
  await advanceTime(0)

  expect(api.moveTo).toHaveBeenCalledWith(2, 1)
  expect(screen.queryByRole("dialog", { name: "選擇地區" })).not.toBeInTheDocument()
  expect(screen.getByRole("button", { name: "試鍊洞窟・燈影隧道 ▾" })).toBeInTheDocument()
  const lines = screen.getAllByRole("listitem").map((line) => line.textContent)
  expect(lines).toContain("你前往試鍊洞窟・燈影隧道。")
})

test("AdventureScreen_MovingToAPlaceWithoutSubPlacesLogsOnlyThePlace", async () => {
  const api = createFakeApi()
  vi.mocked(api.moveTo).mockResolvedValue({
    ...character,
    placeId: 20,
    placeName: "黑曜神廟",
    subPlace: null,
    subPlaceName: null,
  })
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "中央平原・香茅綠地 ▾" }))
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: /^黑曜神廟/ }))
  fireEvent.click(screen.getByRole("button", { name: "前往" }))
  await advanceTime(0)

  expect(screen.getByRole("button", { name: "黑曜神廟 ▾" })).toBeInTheDocument()
  const lines = screen.getAllByRole("listitem").map((line) => line.textContent)
  expect(lines).toContain("你前往黑曜神廟。")
})

test("AdventureScreen_ShowsTheRemainingAreaPassTime", async () => {
  render(
    <AdventureScreen
      api={createFakeApi({ ...response, areaPassExpiresAt: new Date("2026-10-21T00:30:00Z") })}
      initialCharacter={character}
      onSignOut={vi.fn()}
    />,
  )

  await advanceTime(0)
  await advanceTime(100)

  expect(screen.getByText("禁區通行剩 30 分")).toBeInTheDocument()
})

test("AdventureScreen_TellsWhenTheAreaPassHasEnded", async () => {
  render(
    <AdventureScreen
      api={createFakeApi({ ...response, areaPassExpiresAt: new Date("2026-10-20T23:00:00Z") })}
      initialCharacter={character}
      onSignOut={vi.fn()}
    />,
  )

  await advanceTime(0)
  await advanceTime(100)

  expect(screen.getByText("禁區通行時間已結束，請到地圖用鑰匙再次進入")).toBeInTheDocument()
  expect(screen.queryByText(/禁區通行剩/)).not.toBeInTheDocument()
})

test("AdventureScreen_OpeningTheCharacterTabReloadsBuffs", async () => {
  const api = createFakeApi()
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "角色" }))
  await advanceTime(0)
  expect(api.getBuffs).toHaveBeenCalledTimes(1)

  fireEvent.click(screen.getByRole("button", { name: "冒險" }))
  fireEvent.click(screen.getByRole("button", { name: "角色" }))
  await advanceTime(0)

  expect(api.getBuffs).toHaveBeenCalledTimes(2)
})

test("AdventureScreen_MovingDiscardsTheOldAreasQueuedBattles", async () => {
  const api = createFakeApi({
    ...response,
    events: [
      response.events[0],
      { type: "reward", atSeconds: 5, experience: 8, money: 3, jobExperience: 2, drops: [] },
    ],
  })
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  await advanceTime(100)
  expect(screen.getAllByRole("listitem")).toHaveLength(1)

  fireEvent.click(screen.getByRole("button", { name: "中央平原・香茅綠地 ▾" }))
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: /^試鍊洞窟/ }))
  fireEvent.click(screen.getByRole("button", { name: "燈影隧道" }))
  await advanceTime(0)
  await advanceTime(6_000)

  const lines = screen.getAllByRole("listitem").map((line) => line.textContent)
  expect(lines).toEqual(["遭遇 測試寒狼！", "你前往試鍊洞窟・燈影隧道。"])
})

// 移動前送出的輪詢在移動之後才回來：回應裡是舊地區，不顯示它的通行倒數
test("AdventureScreen_StaleResponseFromTheOldPlaceShowsNoPassCountdown", async () => {
  const api = createFakeApi()
  let resolveAdvance: (value: AdvanceVo) => void = () => {}
  vi.mocked(api.advance).mockReturnValue(
    new Promise<AdvanceVo>((resolve) => {
      resolveAdvance = resolve
    }),
  )
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "中央平原・香茅綠地 ▾" }))
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: /^試鍊洞窟/ }))
  fireEvent.click(screen.getByRole("button", { name: "燈影隧道" }))
  await advanceTime(0)
  await act(async () => {
    resolveAdvance({
      ...response,
      events: [],
      summary: makeSummary(),
      character,
      areaPassExpiresAt: new Date("2026-10-21T00:30:00Z"),
    })
  })
  await advanceTime(100)

  expect(screen.getByRole("button", { name: "試鍊洞窟・燈影隧道 ▾" })).toBeInTheDocument()
  expect(screen.queryByText(/禁區通行/)).not.toBeInTheDocument()
})

test("AdventureScreen_OpenBagReloadsWhenADropPlays", async () => {
  const api = createFakeApi({
    ...response,
    events: [
      response.events[0],
      {
        type: "reward",
        atSeconds: 1,
        experience: 8,
        money: 3,
        jobExperience: 2,
        drops: [{ itemId: "wolf-fang", name: "狼牙", quantity: 1, discarded: false }],
      },
    ],
  })
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: "背包" }))
  await advanceTime(0)
  expect(api.getInventory).toHaveBeenCalledTimes(1)

  await advanceTime(PanelRefreshMilliseconds + 1_000)

  expect(api.getInventory).toHaveBeenCalledTimes(2)
})

// 背景回來時伺服器只回最後一段事件，其餘場次的掉落由摘要交代
test("AdventureScreen_OpenBagReloadsAfterASummary", async () => {
  const api = createFakeApi({
    ...response,
    events: [],
    summary: makeSummary({ fights: 30, wins: 30, experience: 240, money: 90 }),
    eventsCoverAll: false,
  })
  render(<AdventureScreen api={api} initialCharacter={character} onSignOut={vi.fn()} />)
  await advanceTime(0)
  fireEvent.click(screen.getByRole("button", { name: "背包" }))
  await advanceTime(0)
  expect(api.getInventory).toHaveBeenCalledTimes(1)

  await advanceTime(PanelRefreshMilliseconds + 1_000)

  expect(api.getInventory).toHaveBeenCalledTimes(2)
})

// 從城鎮點旅館：停在背包分頁的旅館子頁
test("AdventureScreen_OpensAtTheGivenTabAndBagPage", async () => {
  render(
    <AdventureScreen
      api={createFakeApi()}
      initialCharacter={character}
      initialTab="bag"
      initialBagPage="inn"
      onSignOut={vi.fn()}
      onReturnToTown={vi.fn()}
    />,
  )
  await advanceTime(0)

  expect(screen.getByRole("button", { name: "背包" })).toHaveAttribute("aria-current", "page")
  expect(screen.getByRole("tab", { name: "旅館" })).toHaveAttribute("aria-selected", "true")
})

// 回城鎮：還沒播的戰鬥丟掉，交回伺服器最新的角色
test("AdventureScreen_ReturnToTownHandsBackTheLatestCharacter", async () => {
  const onReturnToTown = vi.fn()
  render(
    <AdventureScreen
      api={createFakeApi()}
      initialCharacter={character}
      onSignOut={vi.fn()}
      onReturnToTown={onReturnToTown}
    />,
  )
  // 推進回應已收到、還沒播完（第二段只過 0 毫秒）
  await advanceTime(0)
  await advanceTime(0)

  fireEvent.click(screen.getByRole("button", { name: "回城鎮" }))

  expect(onReturnToTown).toHaveBeenCalledTimes(1)
  expect(onReturnToTown.mock.calls[0][0]).toMatchObject({ experience: 8, money: 3 })
})

// 卸載（回到城鎮）後不再推進
test("AdventureScreen_ReturnToTownStopsPolling", async () => {
  const api = createFakeApi()
  const { unmount } = render(
    <AdventureScreen
      api={api}
      initialCharacter={character}
      onSignOut={vi.fn()}
      onReturnToTown={vi.fn()}
    />,
  )
  await advanceTime(0)
  const calls = vi.mocked(api.advance).mock.calls.length
  expect(calls).toBeGreaterThan(0)

  unmount()
  await advanceTime(PollIntervalMilliseconds * 3)

  expect(api.advance).toHaveBeenCalledTimes(calls)
})
