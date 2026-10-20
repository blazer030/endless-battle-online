import type { AdvanceResponse, CharacterView, ContentNames } from "../game/types"
import { RequestTimeoutMilliseconds } from "../motion/timings"
import { clearAccessToken, readAccessToken, saveAccessToken } from "./authStorage"

const fallbackDetail = "發生錯誤，請稍後再試"

export class ApiError extends Error {
  readonly status: number
  readonly detail: string

  constructor(status: number, detail: string) {
    super(detail)
    this.status = status
    this.detail = detail
    this.name = "ApiError"
  }
}

export type ApiClient = {
  register: (username: string, password: string) => Promise<void>
  login: (username: string, password: string) => Promise<void>
  getMyCharacter: () => Promise<CharacterView | null>
  createCharacter: (name: string) => Promise<CharacterView>
  advance: () => Promise<AdvanceResponse>
  getContent: () => Promise<ContentNames>
}

const readDetail = async (response: Response): Promise<string> => {
  try {
    const problem: unknown = await response.json()
    if (typeof problem === "object" && problem !== null && "detail" in problem) {
      const { detail } = problem
      if (typeof detail === "string" && detail !== "") return detail
    }
  } catch {
    // 內容不是 JSON 時使用預設訊息
  }
  return fallbackDetail
}

export const createApiClient = (options: {
  fetch: typeof fetch
  onUnauthorized: () => void
  requestTimeoutMilliseconds?: number
}): ApiClient => {
  const send = async (path: string, method: string, body?: unknown): Promise<Response> => {
    const headers = new Headers()
    const token = readAccessToken()
    if (token !== null) headers.set("Authorization", `Bearer ${token}`)
    if (body !== undefined) headers.set("Content-Type", "application/json")

    const response = await options.fetch(path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      // 逾時會以 TimeoutError 拋出，不轉成 ApiError，由呼叫端當成一般連線錯誤
      signal: AbortSignal.timeout(options.requestTimeoutMilliseconds ?? RequestTimeoutMilliseconds),
    })
    if (response.ok) return response

    const detail = await readDetail(response)
    if (response.status === 401) {
      clearAccessToken()
      options.onUnauthorized()
    }
    throw new ApiError(response.status, detail)
  }

  const requestJson = async <T>(path: string, method: string, body?: unknown): Promise<T> =>
    (await send(path, method, body)).json() as Promise<T>

  return {
    register: async (username, password) => {
      await send("/api/accounts/register", "POST", { username, password })
    },
    login: async (username, password) => {
      const { accessToken } = await requestJson<{ accessToken: string }>(
        "/api/accounts/login",
        "POST",
        { username, password },
      )
      saveAccessToken(accessToken)
    },
    getMyCharacter: async () => {
      try {
        return await requestJson<CharacterView>("/api/characters/me", "GET")
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null
        throw error
      }
    },
    createCharacter: (name) => requestJson<CharacterView>("/api/characters", "POST", { name }),
    advance: () => requestJson<AdvanceResponse>("/api/adventure/advance", "POST"),
    getContent: () => requestJson<ContentNames>("/api/content", "GET"),
  }
}
