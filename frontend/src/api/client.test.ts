import { beforeEach, expect, test, vi } from "vitest"
import { readAccessToken, saveAccessToken } from "./authStorage"
import { ApiError, createApiClient } from "./client"

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } })

const advanceBody = {
  events: [],
  experienceGained: 0,
  spiritStonesGained: 0,
  character: { name: "青", experience: 0, spiritStones: 0, currentStageId: "stage-1" },
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

test("getMyCharacter_ReturnsNull_On404", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "找不到角色" }, 404))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  expect(await client.getMyCharacter()).toBeNull()
})

test("request_ThrowsApiErrorWithDetail_OnConflict", async () => {
  const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ detail: "角色名稱已被使用" }, 409))
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const error = await client.createCharacter("青").catch((caught: unknown) => caught)

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
