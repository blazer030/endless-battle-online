import type {
  BaseRecord,
  CreateParams,
  DataProvider,
  GetListParams,
  GetOneParams,
  UpdateParams,
} from "@refinedev/core"

// admin API 的內容紀錄：resource 名稱就是 /admin/content/{kind} 的 kind；寫入只會進草稿
const contentUrl = "/admin/content"

export class AdminApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(message: string, status: number, body: unknown) {
    super(message)
    this.status = status
    this.body = body
  }
}

// 回應本體的型別由呼叫端指定；admin API 的紀錄是無型別的 JSON 物件
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

const recordUrl = (resource: string, id: string | number) =>
  `${contentUrl}/${resource}/${encodeURIComponent(String(id))}`

export const dataProvider: DataProvider = {
  getApiUrl: () => contentUrl,
  getList: async <TData extends BaseRecord = BaseRecord>({ resource }: GetListParams) => {
    const data = await request<TData[]>(`${contentUrl}/${resource}`)
    return { data, total: data.length }
  },
  getOne: async <TData extends BaseRecord = BaseRecord>({ resource, id }: GetOneParams) => ({
    data: await request<TData>(recordUrl(resource, id)),
  }),
  create: async <TData extends BaseRecord = BaseRecord, TVariables = object>({
    resource,
    variables,
  }: CreateParams<TVariables>) => {
    await request(`${contentUrl}/${resource}`, { method: "POST", body: JSON.stringify(variables) })
    return { data: await request<TData>(recordUrl(resource, idOf(variables))) }
  },
  update: async <TData extends BaseRecord = BaseRecord, TVariables = object>({
    resource,
    id,
    variables,
  }: UpdateParams<TVariables>) => {
    await request(recordUrl(resource, id), { method: "PUT", body: JSON.stringify(variables) })
    return { data: await request<TData>(recordUrl(resource, id)) }
  },
  deleteOne: () => Promise.reject(new Error("內容紀錄不能刪除")),
}

function idOf(variables: unknown): string | number {
  if (typeof variables === "object" && variables !== null && "id" in variables) {
    const { id } = variables
    if (typeof id === "string" || typeof id === "number") {
      return id
    }
  }
  throw new Error("新紀錄缺少 id")
}
