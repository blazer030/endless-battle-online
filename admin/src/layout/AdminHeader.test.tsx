import { render, screen, within } from "@testing-library/react"
import { MemoryRouter } from "react-router"
import { afterEach, describe, expect, it, vi } from "vitest"
import { App } from "../App"
import { mockFetch } from "../test/fetchMock"

afterEach(() => {
  vi.unstubAllGlobals()
})

describe("AdminHeader", () => {
  it("Header shows page name and draft status", async () => {
    mockFetch({
      "GET /admin/content/monsters": [],
      "GET /admin/draft": { exists: true, valid: true, error: null },
    })

    render(
      <MemoryRouter initialEntries={["/monsters"]}>
        <App />
      </MemoryRouter>,
    )

    const header = await screen.findByRole("banner")
    expect(within(header).getByRole("heading", { name: "怪物" })).toBeInTheDocument()
    expect(await within(header).findByText("草稿通過檢查")).toBeInTheDocument()
    expect(within(header).getByText("發布").closest("button")).toBeEnabled()
  })
})
