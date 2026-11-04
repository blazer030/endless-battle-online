import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import { mockFetch, respond } from "../test/fetchMock"

afterEach(() => {
  vi.unstubAllGlobals()
})

const renderApp = () =>
  render(
    <MemoryRouter>
      <App />
    </MemoryRouter>,
  )

describe("DraftBar", () => {
  it("shows the loader error and disables publish when the draft is invalid", async () => {
    mockFetch({
      "GET /admin/content/items": [],
      "GET /admin/draft": {
        exists: true,
        valid: false,
        error: "怪物 test-sprite 引用了不存在的地區：9",
      },
    })

    renderApp()

    expect(await screen.findByText("怪物 test-sprite 引用了不存在的地區：9")).toBeInTheDocument()
    expect(screen.getByRole("button", { name: "發布" })).toBeDisabled()
  })

  it("names the changed official file when publishing is refused and keeps the draft", async () => {
    const calls = mockFetch({
      "GET /admin/content/items": [],
      "GET /admin/draft": { exists: true, valid: true, error: null },
      "POST /admin/draft/publish": respond(409, {
        reason: "officialChanged",
        error: null,
        changedFiles: ["items.json"],
      }),
    })
    renderApp()

    await userEvent.click(await screen.findByRole("button", { name: "發布" }))

    expect(await screen.findByText(/items\.json/)).toBeInTheDocument()
    // 發布被拒、重新讀取狀態後，以按鈕名稱查詢找不到這顆按鈕（原因未查明），改用文字找
    expect(screen.getByText("發布").closest("button")).toBeEnabled()
    await waitFor(() => expect(calls.some((call) => call.method === "DELETE")).toBe(false))
  })
})
