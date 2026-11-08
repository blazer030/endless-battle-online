import { render, screen } from "@testing-library/react"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import type { BalanceReport } from "../api/report"
import { mockFetch } from "../test/fetchMock"

afterEach(() => {
  vi.unstubAllGlobals()
})

const reportWithWarriorLevel = (level: number): BalanceReport => ({
  assumptions: {
    allocation: { str: 4, smart: 4, life: 1, agi: 1 },
    seed: 1,
    simulatedHours: 8,
    maxReportedLevel: 100,
    skillLevel: "Max",
  },
  shopLevels: [1, 2],
  wearLevels: [
    {
      jobId: "warrior",
      jobName: "戰士",
      stat: "str",
      cells: [
        { shopLevel: 1, required: 10, level: 1 },
        { shopLevel: 2, required: 30, level },
      ],
    },
  ],
  adventures: [],
})

describe("ReportPage", () => {
  it("shows the draft report and marks only the cell that differs from the official one", async () => {
    mockFetch({
      "GET /admin/draft": { exists: true, valid: true, error: null },
      "POST /admin/reports": (body: { source: string }) =>
        reportWithWarriorLevel(body.source === "Draft" ? 10 : 8),
    })

    render(
      <MemoryRouter initialEntries={["/reports"]}>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByText("8 → 10")).toBeInTheDocument()
    expect(document.querySelectorAll("[data-changed]")).toHaveLength(1)
  })

  it("falls back to the official report when there is no draft", async () => {
    mockFetch({
      "GET /admin/draft": { exists: false, valid: true, error: null },
      "POST /admin/reports": (body: { source: string }) =>
        body.source === "Draft" ? { status: 404 } : reportWithWarriorLevel(8),
    })

    render(
      <MemoryRouter initialEntries={["/reports"]}>
        <App />
      </MemoryRouter>,
    )

    expect(await screen.findByText("沒有草稿，顯示正式內容的報告")).toBeInTheDocument()
    expect(await screen.findByText("8")).toBeInTheDocument()
    expect(document.querySelectorAll("[data-changed]")).toHaveLength(0)
  })
})
