import { beforeEach, expect, test, vi } from "vitest"
import { readAccessToken, saveAccessToken } from "./authStorage"
import type { CreateCharacterCommand } from "../game/types"
import { makeCharacter } from "../test/fixtures"
import { ApiError, createApiClient } from "./client"

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } })

const advanceBody = {
  events: [],
  expGained: 0,
  moneyGained: 0,
  character: makeCharacter(),
}

const command: CreateCharacterCommand = {
  name: "青衣",
  sex: 2,
  element: 3,
  birth: 4,
  jobId: "sorcerer",
  allocation: { str: 1, smart: 5, life: 3, agi: 1 },
  specialAttackName: "流星火雨",
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
