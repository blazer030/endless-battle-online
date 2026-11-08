export class AdminApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(message: string, status: number, body: unknown) {
    super(message)
    this.status = status
    this.body = body
  }
}

// 回應本體的型別由呼叫端指定；內容紀錄要再經過 Converter 檢查
export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  })
  const text = await response.text()
  const body: unknown = text === "" ? undefined : JSON.parse(text)
  if (!response.ok) {
    throw new AdminApiError(
      `${init?.method ?? "GET"} ${url} 失敗：${response.status}`,
      response.status,
      body,
    )
  }
  return body as T
}
