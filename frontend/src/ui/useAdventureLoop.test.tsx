import { act, renderHook } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import type { ApiClient } from "../api/client"
import { BattlePlayback } from "../game/battlePlayback"
import type { AdvanceResponse, CharacterView } from "../game/types"
import { PollIntervalMilliseconds, RequestTimeoutMilliseconds } from "../motion/timings"
import { makeCharacter, makeSummary } from "../test/fixtures"
import { useAdventureLoop } from "./useAdventureLoop"

const character: CharacterView = makeCharacter()

const emptyResponse: AdvanceResponse = {
  events: [],
  summary: makeSummary(),
  eventsCoverAll: true,
  character,
  mercenary: null,
  areaPassExpiresAt: null,
}

const createFakeApi = (advance: ApiClient["advance"]): ApiClient => ({
  register: vi.fn(),
  login: vi.fn(),
  getMyCharacter: vi.fn(),
  createCharacter: vi.fn(),
  advance,
  getContent: vi.fn(),
  getPublicJobs: vi.fn(),
  getCreationOptions: vi.fn(),
  getJobs: vi.fn(),
  changeJob: vi.fn(),
  getSkills: vi.fn(),
  learnSkill: vi.fn(),
  setSkillSlots: vi.fn(),
  getWorld: vi.fn(),
  moveTo: vi.fn(),
  getInventory: vi.fn(),
  equip: vi.fn(),
  unequip: vi.fn(),
  useItem: vi.fn(),
  getShop: vi.fn(),
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
  getMissions: vi.fn(),
  acceptMission: vi.fn(),
  abandonMission: vi.fn(),
  completeMission: vi.fn(),
  getPets: vi.fn(),
  carryPet: vi.fn(),
  storePet: vi.fn(),
  releasePet: vi.fn(),
  renamePet: vi.fn(),
  feedPet: vi.fn(),
  getMercenaries: vi.fn(),
  hireMercenary: vi.fn(),
  deployMercenary: vi.fn(),
  restMercenary: vi.fn(),
  renameMercenary: vi.fn(),
  dismissMercenary: vi.fn(),
  getHonourShop: vi.fn(),
  exchangeHonour: vi.fn(),
  updateSettings: vi.fn(),
  getBuffs: vi.fn(),
  getInn: vi.fn(),
  bookInn: vi.fn(),
})

const renderLoop = (api: ApiClient) =>
  renderHook(() =>
    useAdventureLoop({ api, playback: new BattlePlayback(character), onItems: vi.fn() }),
  )

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
  Reflect.deleteProperty(document, "visibilityState")
})

const setVisibility = (state: DocumentVisibilityState) =>
  act(async () => {
    Object.defineProperty(document, "visibilityState", { configurable: true, get: () => state })
    document.dispatchEvent(new Event("visibilitychange"))
  })

test("adventureLoop_AdvancesOnMountAndEveryInterval", async () => {
  const advance = vi.fn().mockResolvedValue(emptyResponse)
  renderLoop(createFakeApi(advance))
  await advanceTime(0)
  expect(advance).toHaveBeenCalledTimes(1)

  await advanceTime(10_000)
  expect(advance).toHaveBeenCalledTimes(2)

  await advanceTime(20_000)
  expect(advance).toHaveBeenCalledTimes(4)
})

test("adventureLoop_IgnoresConflictAndKeepsPolling", async () => {
  const advance = vi
    .fn()
    .mockRejectedValueOnce(new ApiError(409, "冒險進行中"))
    .mockResolvedValue(emptyResponse)
  const { result } = renderLoop(createFakeApi(advance))
  await advanceTime(0)
  expect(result.current.error).toBeNull()

  await advanceTime(10_000)

  expect(advance).toHaveBeenCalledTimes(2)
  expect(result.current.error).toBeNull()
})

test("adventureLoop_ShowsRetryMessage_OnNetworkError_AndClearsOnSuccess", async () => {
  const advance = vi
    .fn()
    .mockRejectedValueOnce(new TypeError("Failed to fetch"))
    .mockResolvedValue(emptyResponse)
  const { result } = renderLoop(createFakeApi(advance))
  await advanceTime(0)
  expect(result.current.error).toBe("連線不穩，稍後自動重試")

  await advanceTime(10_000)

  expect(result.current.error).toBeNull()
})

test("adventureLoop_StopsOnUnmount", async () => {
  const advance = vi.fn().mockResolvedValue(emptyResponse)
  const { unmount } = renderLoop(createFakeApi(advance))
  await advanceTime(0)
  const callsBeforeUnmount = advance.mock.calls.length

  unmount()
  await advanceTime(30_000)
  document.dispatchEvent(new Event("visibilitychange"))

  expect(advance).toHaveBeenCalledTimes(callsBeforeUnmount)
})

test("adventureLoop_AdvancesWhenTabBecomesVisible", async () => {
  const advance = vi.fn().mockResolvedValue(emptyResponse)
  renderLoop(createFakeApi(advance))
  await advanceTime(0)

  await act(async () => {
    document.dispatchEvent(new Event("visibilitychange"))
  })

  expect(advance).toHaveBeenCalledTimes(2)
})

test("adventureLoop_DoesNotPollWhileHidden", async () => {
  const advance = vi.fn().mockResolvedValue(emptyResponse)
  renderLoop(createFakeApi(advance))
  await advanceTime(0)
  expect(advance).toHaveBeenCalledTimes(1)

  await setVisibility("hidden")
  await advanceTime(30_000)
  expect(advance).toHaveBeenCalledTimes(1)

  await setVisibility("visible")
  expect(advance).toHaveBeenCalledTimes(2)

  await advanceTime(10_000)
  expect(advance).toHaveBeenCalledTimes(3)
})

test("adventureLoop_HandsDueItemsToOnItems", async () => {
  const response: AdvanceResponse = {
    ...emptyResponse,
    events: [
      {
        type: "encounter",
        atSeconds: 0,
        monsterId: "test-wolf",
        monsterHp: 60,
        monsterHpMax: 60,
        playerHp: 120,
        playerHpMax: 120,
        playerSp: 10,
        playerSpMax: 10,
        petName: null,
        petMonsterId: null,
      },
    ],
  }
  const onItems = vi.fn()
  const api = createFakeApi(vi.fn().mockResolvedValue(response))
  renderHook(() => useAdventureLoop({ api, playback: new BattlePlayback(character), onItems }))

  await advanceTime(100)

  expect(onItems).toHaveBeenCalledTimes(1)
  expect(onItems).toHaveBeenCalledWith([{ kind: "event", event: response.events[0] }])
})

test("adventureLoop_RecoversAfterHungRequest", async () => {
  const advance = vi
    .fn()
    .mockImplementationOnce(
      () =>
        new Promise((_resolve, reject) => {
          setTimeout(
            () => reject(new DOMException("signal timed out", "TimeoutError")),
            RequestTimeoutMilliseconds,
          )
        }),
    )
    .mockResolvedValue(emptyResponse)
  const { result } = renderLoop(createFakeApi(advance))
  await advanceTime(0)
  expect(result.current.error).toBeNull()

  await advanceTime(RequestTimeoutMilliseconds)
  expect(result.current.error).toBe("連線不穩，稍後自動重試")

  await advanceTime(PollIntervalMilliseconds - RequestTimeoutMilliseconds)
  expect(advance).toHaveBeenCalledTimes(2)
  expect(result.current.error).toBeNull()
})
