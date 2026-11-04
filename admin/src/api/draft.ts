import type { components } from "./schema"
import { AdminApiError, request } from "./dataProvider"

export type DraftStatus = components["schemas"]["DraftStatus"]
export type PublishRefusal = components["schemas"]["PublishRefusal"]

export const draftQueryKey = ["admin", "draft"]

export const getDraft = () => request<DraftStatus>("/admin/draft")

export const discardDraft = () => request<void>("/admin/draft", { method: "DELETE" })

// 發布被拒時回傳拒絕原因；成功時回傳 null
export async function publishDraft(): Promise<PublishRefusal | null> {
  try {
    await request("/admin/draft/publish", { method: "POST" })
    return null
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 409 && isRefusal(error.body)) {
      return error.body
    }
    throw error
  }
}

function isRefusal(body: unknown): body is PublishRefusal {
  return typeof body === "object" && body !== null && "reason" in body && "changedFiles" in body
}
