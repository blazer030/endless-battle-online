import { beforeEach, expect, test, vi } from "vitest"
import { readAccessToken, saveAccessToken } from "./authStorage"
import type { CreateCharacterCommand } from "../game/character"
import { makeCharacter, makeInventory, makeItem, makeShopItem, makeSummary } from "../test/fixtures"
import { ApiError, createApiClient } from "./client"

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } })

const advanceBody = {
  events: [],
  summary: makeSummary(),
  eventsCoverAll: true,
  character: makeCharacter(),
  mercenary: null,
  areaPassExpiresAt: null,
}

// 只驗證路徑與方法的測試共用：同時可當成角色，也帶著夾帶的角色
const anyBody = { ...makeCharacter(), character: makeCharacter() }

const depotBody = { entries: [makeItem({ inventoryId: 11 })], capacity: 30 }

// 背包、商店、倉庫的回應依路徑給對應的本體
const inventoryBodyFor = (path: string): unknown => {
  if (path.startsWith("/api/shop?")) return [makeShopItem()]
  if (path === "/api/depot") return depotBody
  if (path.startsWith("/api/depot/")) return { inventory: makeInventory(), depot: depotBody }
  if (path === "/api/inventory/use")
    return {
      inventory: makeInventory(),
      obtained: [],
      petName: null,
      skillPoints: 0,
      levelsGained: 0,
      buff: null,
    }
  if (path.startsWith("/api/inventory") || path.startsWith("/api/shop/")) return makeInventory()
  return anyBody
}

// 鐵舖的回應依路徑給對應的本體
const forgeBodyFor = (path: string): unknown => {
  if (path === "/api/forge/recipes") return []
  if (path === "/api/forge/synthesize")
    return { successes: 1, attempts: 2, cost: 100, inventory: makeInventory() }
  if (path === "/api/forge/unsocket")
    return { success: true, recovered: false, inventory: makeInventory() }
  return { success: true, inventory: makeInventory() }
}

const command: CreateCharacterCommand = {
  name: "青衣",
  sex: 2,
  element: 3,
  birth: 4,
  jobId: "sorcerer",
  allocation: { strength: 1, intelligence: 5, life: 3, agility: 1 },
}

beforeEach(() => localStorage.clear())

test("request_AddsBearerToken_WhenLoggedIn", async () => {
  saveAccessToken("abc")
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(advanceBody))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.advance()

  const init = fetchMock.mock.calls[0][1] as RequestInit
  expect(new Headers(init.headers).get("Authorization")).toBe("Bearer abc")
})

test("login_SavesToken", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ accessToken: "xyz" }))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.login("user", "password")

  expect(readAccessToken()).toBe("xyz")
})

test("createCharacter_PostsTheFullCommand", async () => {
  const created = makeCharacter({ name: "青衣" })
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(created, 201))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  expect(await client.createCharacter(command)).toEqual(created)

  const [path, init] = fetchMock.mock.calls[0] as [string, RequestInit]
  expect(path).toBe("/api/characters")
  expect(init.method).toBe("POST")
  expect(JSON.parse(init.body as string)).toEqual(command)
})

test("getMyCharacter_ReturnsNull_On404", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "找不到角色" }, 404))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  expect(await client.getMyCharacter()).toBeNull()
})

test("request_ThrowsApiErrorWithDetail_OnConflict", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "角色名稱已被使用" }, 409))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const error = await client.createCharacter(command).catch((caught: unknown) => caught)

  expect(error).toBeInstanceOf(ApiError)
  expect((error as ApiError).status).toBe(409)
  expect((error as ApiError).detail).toBe("角色名稱已被使用")
})

test("request_ClearsTokenAndNotifies_On401", async () => {
  saveAccessToken("abc")
  const onUnauthorized = vi.fn()
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "未授權" }, 401))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized })

  await expect(client.advance()).rejects.toBeInstanceOf(ApiError)

  expect(readAccessToken()).toBeNull()
  expect(onUnauthorized).toHaveBeenCalledTimes(1)
})

const neverRespondingFetch = () =>
  vi.fn((_input: RequestInfo | URL, init?: RequestInit) => {
    const signal = init?.signal
    return new Promise<Response>((_resolve, reject) => {
      signal?.addEventListener("abort", () => reject(signal.reason))
    })
  })

// AbortSignal.timeout 是原生計時器，fake timers 推不動，所以用真計時器與極短逾時
test("request_TimesOut_WhenServerNeverResponds", async () => {
  saveAccessToken("abc")
  const fetchMock = neverRespondingFetch()
  const onUnauthorized = vi.fn()
  const client = createApiClient({
    fetch: fetchMock,
    onUnauthorized,
    requestTimeoutMilliseconds: 20,
  })

  const error = await client.advance().catch((caught: unknown) => caught)

  expect(error).toBeInstanceOf(Error)
  expect(error).not.toBeInstanceOf(ApiError)
  expect((fetchMock.mock.calls[0][1] as RequestInit).signal).toBeInstanceOf(AbortSignal)
  expect(readAccessToken()).toBe("abc")
  expect(onUnauthorized).not.toHaveBeenCalled()
})

test("skillAndJobCalls_UseTheirEndpoints", async () => {
  const emptyOverview = { jobId: "warrior", skillPoints: 0, skills: [], slots: [] }
  const fetchMock = vi
    .fn()
    .mockImplementation((path: string) =>
      Promise.resolve(
        jsonResponse(
          path === "/api/jobs" ? [] : path.startsWith("/api/skills") ? emptyOverview : anyBody,
        ),
      ),
    )
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.getJobs()
  await client.changeJob("knight")
  await client.getSkills()
  await client.learnSkill("burst")
  await client.setSkillSlots([{ slot: 1, skillId: "burst" }])

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["GET", "/api/jobs", undefined],
    ["POST", "/api/jobs/change", { jobId: "knight" }],
    ["GET", "/api/skills", undefined],
    ["POST", "/api/skills/learn", { skillId: "burst" }],
    ["PUT", "/api/skills/slots", { slots: [{ slot: 1, skillId: "burst" }] }],
  ])
})

test("request_ExposesProblemReasons_OnValidationError", async () => {
  const problem = { detail: "技能欄位不正確", reasons: ["duplicateSkill", "notLearned"] }
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(problem, 400))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const error = await client.setSkillSlots([]).catch((caught: unknown) => caught)

  expect(error).toBeInstanceOf(ApiError)
  expect((error as ApiError).detail).toBe("技能欄位不正確")
  expect((error as ApiError).reasons).toEqual(["duplicateSkill", "notLearned"])
})

test("request_HasNoReasons_WhenProblemOmitsThem", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "衝突" }, 409))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const error = await client.learnSkill("burst").catch((caught: unknown) => caught)

  expect((error as ApiError).reasons).toEqual([])
})

test("worldCalls_UseTheirEndpoints", async () => {
  const fetchMock = vi
    .fn()
    .mockImplementation((path: string) =>
      Promise.resolve(jsonResponse(path === "/api/world" ? [] : anyBody)),
    )
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.getWorld()
  await client.moveTo(2, 1)
  await client.moveTo(20, null)

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["GET", "/api/world", undefined],
    ["PUT", "/api/world/location", { placeId: 2, subPlace: 1 }],
    ["PUT", "/api/world/location", { placeId: 20, subPlace: null }],
  ])
})

test("updateSettings_PutsTheCommand", async () => {
  const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(jsonResponse(anyBody)))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })
  const command = {
    attack: null,
    potionHealthPointsPercent: 30,
    potionManaPointsPercent: -1,
    restHealthPointsPercent: 20,
    restManaPointsPercent: -1,
    escapeHealthPointsPercent: 10,
  }

  await client.updateSettings(command)

  const [path, init] = (fetchMock.mock.calls as [string, RequestInit][])[0]
  expect([init.method, path, JSON.parse(init.body as string)]).toEqual([
    "PUT",
    "/api/characters/me/settings",
    command,
  ])
})

test("itemAndEconomyCalls_UseTheirEndpoints", async () => {
  const fetchMock = vi
    .fn()
    .mockImplementation((path: string) => Promise.resolve(jsonResponse(inventoryBodyFor(path))))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.getInventory()
  await client.equip(7)
  await client.equip(8, 20)
  await client.unequip("Supply")
  await client.useItem(9)
  await client.getShop("Weapon", 3)
  await client.buy("wooden-sword", 1)
  await client.sell(7, 2)
  await client.getDepot()
  await client.store(7, 5)
  await client.take(11, 3)
  await client.deposit(1000)
  await client.withdraw(500)

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["GET", "/api/inventory", undefined],
    ["POST", "/api/inventory/equip", { inventoryId: 7 }],
    ["POST", "/api/inventory/equip", { inventoryId: 8, quantity: 20 }],
    ["POST", "/api/inventory/unequip", { slot: "Supply" }],
    ["POST", "/api/inventory/use", { inventoryId: 9 }],
    ["GET", "/api/shop?category=Weapon&level=3", undefined],
    ["POST", "/api/shop/buy", { itemId: "wooden-sword", quantity: 1 }],
    ["POST", "/api/shop/sell", { inventoryId: 7, quantity: 2 }],
    ["GET", "/api/depot", undefined],
    ["POST", "/api/depot/store", { inventoryId: 7, quantity: 5 }],
    ["POST", "/api/depot/take", { depotId: 11, quantity: 3 }],
    ["POST", "/api/bank/deposit", { amount: 1000 }],
    ["POST", "/api/bank/withdraw", { amount: 500 }],
  ])
})

test("forgeCalls_UseTheirEndpoints", async () => {
  const fetchMock = vi
    .fn()
    .mockImplementation((path: string) => Promise.resolve(jsonResponse(forgeBodyFor(path))))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.refine(7, 30, true)
  await client.socket(7, 2, 31)
  await client.unsocket(7, 1)
  await client.drill(7)
  await client.mergeStones(30, 3)
  await client.mergeCrossStones(30, 32, 2)
  await client.getRecipes()
  await client.synthesize("iron-ingot", 5)

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["POST", "/api/forge/refine", { inventoryId: 7, stoneInventoryId: 30, useSafeStone: true }],
    ["POST", "/api/forge/socket", { inventoryId: 7, hole: 2, stoneInventoryId: 31 }],
    ["POST", "/api/forge/unsocket", { inventoryId: 7, hole: 1 }],
    ["POST", "/api/forge/drill", { inventoryId: 7 }],
    ["POST", "/api/forge/merge-stones", { stoneInventoryId: 30, quantityPairs: 3 }],
    [
      "POST",
      "/api/forge/merge-cross",
      { firstInventoryId: 30, secondInventoryId: 32, quantityPairs: 2 },
    ],
    ["GET", "/api/forge/recipes", undefined],
    ["POST", "/api/forge/synthesize", { recipeId: "iron-ingot", attempts: 5 }],
  ])
})

test("missionCalls_UseTheirEndpoints", async () => {
  const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(jsonResponse(anyBody)))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.getMissions()
  await client.acceptMission("test-hunt")
  await client.abandonMission("test-hunt")
  await client.completeMission("test-hunt")

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["GET", "/api/missions", undefined],
    ["POST", "/api/missions/accept", { missionId: "test-hunt" }],
    ["POST", "/api/missions/abandon", { missionId: "test-hunt" }],
    ["POST", "/api/missions/complete", { missionId: "test-hunt" }],
  ])
})

test("companionCalls_UseTheirEndpoints", async () => {
  const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(jsonResponse(anyBody)))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await client.getPets()
  await client.carryPet(3)
  await client.storePet(3)
  await client.releasePet(3)
  await client.renamePet(3, "小黏")
  await client.feedPet(3, "fruit-aroma-medium")
  await client.getMercenaries()
  await client.hireMercenary("mercenary-f")
  await client.deployMercenary(2)
  await client.restMercenary()
  await client.renameMercenary("阿福")
  await client.dismissMercenary()
  await client.getHonourShop()
  await client.exchangeHonour("honour-attack-stone")

  const calls = (fetchMock.mock.calls as [string, RequestInit][]).map(([path, init]) => [
    init.method,
    path,
    init.body === undefined ? undefined : JSON.parse(init.body as string),
  ])
  expect(calls).toEqual([
    ["GET", "/api/pets", undefined],
    ["POST", "/api/pets/carry", { petId: 3 }],
    ["POST", "/api/pets/store", { petId: 3 }],
    ["POST", "/api/pets/release", { petId: 3 }],
    ["POST", "/api/pets/rename", { petId: 3, name: "小黏" }],
    ["POST", "/api/pets/feed", { petId: 3, foodId: "fruit-aroma-medium" }],
    ["GET", "/api/mercenaries", undefined],
    ["POST", "/api/mercenaries/hire", { mercenaryId: "mercenary-f" }],
    ["POST", "/api/mercenaries/deploy", { placeId: 2 }],
    ["POST", "/api/mercenaries/rest", undefined],
    ["POST", "/api/mercenaries/rename", { name: "阿福" }],
    ["POST", "/api/mercenaries/dismiss", undefined],
    ["GET", "/api/honour", undefined],
    ["POST", "/api/honour/exchange", { exchangeId: "honour-attack-stone" }],
  ])
})

test("useItem_ReturnsTheUseResult", async () => {
  const body = {
    inventory: makeInventory(),
    obtained: ["攻擊魔石", "疾風魔石"],
    petName: null,
    skillPoints: 0,
    levelsGained: 0,
    buff: null,
  }
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(body))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const result = await client.useItem(9)

  expect(result.obtained).toEqual(["攻擊魔石", "疾風魔石"])
  expect(result.inventory).toEqual(makeInventory())
})

test("getBuffs_GetsTheActiveBuffs", async () => {
  const buffs = [{ kind: "Experience", multiplier: 1.5, endsAt: "2026-10-23T04:00:00Z" }]
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(buffs))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await expect(client.getBuffs()).resolves.toEqual(buffs)
  expect(fetchMock).toHaveBeenCalledWith(
    "/api/characters/me/buffs",
    expect.objectContaining({ method: "GET" }),
  )
})

test("getInn_GetsRoomsAndLodging", async () => {
  const inn = { rooms: [], lodging: null }
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(inn))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await expect(client.getInn()).resolves.toEqual(inn)
  expect(fetchMock).toHaveBeenCalledWith("/api/inn", expect.objectContaining({ method: "GET" }))
})

test("bookInn_PostsTheRoomId", async () => {
  const booked = {
    character: makeCharacter(),
    lodging: { roomId: "bulwark-room", endsAt: "2026-10-30T08:00:00Z" },
    cost: 300,
  }
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse(booked))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  await expect(client.bookInn("bulwark-room")).resolves.toEqual(booked)
  expect(fetchMock).toHaveBeenCalledWith(
    "/api/inn/book",
    expect.objectContaining({ method: "POST", body: JSON.stringify({ roomId: "bulwark-room" }) }),
  )
})
