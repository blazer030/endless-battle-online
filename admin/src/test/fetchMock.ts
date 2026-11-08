import { vi } from "vitest"

export type FetchCall = { method: string; url: string; body: unknown }

// 指定狀態碼的回應；直接給值時是 200
export type MockResponse = { status: number; body?: unknown }

export const respond = (status: number, body?: unknown): MockResponse => ({ status, body })

const isMockResponse = (value: unknown): value is MockResponse =>
  typeof value === "object" &&
  value !== null &&
  "status" in value &&
  typeof value.status === "number"

// 依「方法 路徑」回應的假 fetch；沒有設定的請求回 404，並記下每一次呼叫
export function mockFetch(routes: Record<string, unknown>) {
  const calls: FetchCall[] = []
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: string, init?: RequestInit) => {
      const method = init?.method ?? "GET"
      const body: unknown = typeof init?.body === "string" ? JSON.parse(init.body) : undefined
      calls.push({ method, url: input, body })
      const key = `${method} ${input}`
      if (!(key in routes)) {
        return new Response("", { status: 404 })
      }
      const routed = routes[key]
      // 同一個網址要依請求本體回不同內容時，給一個函式
      const route: unknown = typeof routed === "function" ? routed(body) : routed
      const { status, body: responseBody } = isMockResponse(route)
        ? route
        : { status: 200, body: route }
      return new Response(responseBody === undefined ? "" : JSON.stringify(responseBody), {
        status,
      })
    }),
  )
  return calls
}
