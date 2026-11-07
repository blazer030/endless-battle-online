import type { components } from "./schema"
import { AdminApiError, request } from "./request"

export type BalanceReport = components["schemas"]["BalanceReport"]
export type BalanceSkillLevel = components["schemas"]["BalanceSkillLevel"]
export type ReportSource = components["schemas"]["ReportSource"]

// 草稿不存在或沒有通過檢查時，回傳原因而不是報告
export type ReportOutcome = { report: BalanceReport } | { missing: true } | { error: string }

export async function fetchReport(
  source: ReportSource,
  skillLevel: BalanceSkillLevel,
): Promise<ReportOutcome> {
  try {
    return {
      report: await request<BalanceReport>("/admin/reports", {
        method: "POST",
        body: JSON.stringify({ source, skillLevel }),
      }),
    }
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 404) {
      return { missing: true }
    }
    if (error instanceof AdminApiError && error.status === 409) {
      return { error: refusalMessage(error.body) }
    }
    throw error
  }
}

function refusalMessage(body: unknown): string {
  return typeof body === "object" &&
    body !== null &&
    "error" in body &&
    typeof body.error === "string"
    ? body.error
    : "草稿沒有通過檢查"
}
